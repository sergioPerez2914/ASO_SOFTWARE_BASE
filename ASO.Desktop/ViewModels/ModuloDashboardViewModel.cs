using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

public sealed record Indicador(string Etiqueta, string Valor, string Nota);

/// <summary>
/// Resumen de un módulo: sus indicadores y el acceso a cada submódulo.
/// Los indicadores muestran valores vacíos hasta que el módulo correspondiente
/// tenga una fuente de datos conectada.
/// </summary>
public sealed class ModuloDashboardViewModel : ViewModelBase
{
    public event EventHandler<Submodulo>? SubmoduloSolicitado;

    public Modulo Modulo { get; }
    public IReadOnlyList<Indicador> Indicadores { get; }
    public IReadOnlyList<Submodulo> Submodulos => Modulo.Submodulos;

    public ICommand AbrirSubmoduloCommand { get; }

    public ModuloDashboardViewModel(Modulo modulo)
    {
        Modulo = modulo;
        Indicadores = CalcularIndicadores(modulo)
            ?? [.. modulo.Indicadores.Select(e => new Indicador(e, "—", "sin datos"))];

        AbrirSubmoduloCommand = new RelayCommand<Submodulo>(s => SubmoduloSolicitado?.Invoke(this, s));
    }

    /// <summary>
    /// Indicadores con datos reales para los módulos que ya tienen fuente conectada;
    /// <c>null</c> deja el marcador "—" genérico. Las etiquetas deben coincidir con las
    /// declaradas en <see cref="ModuloCatalogo"/>.
    /// </summary>
    private static IReadOnlyList<Indicador>? CalcularIndicadores(Modulo modulo) => modulo.Clave switch
    {
        "Operaciones" => CalcularOperaciones(),
        "Flota" => CalcularFlota(),
        "Inventario" => CalcularInventario(),
        "Nomina" => CalcularNomina(),
        "Finanzas" => CalcularFinanzas(),
        _ => null
    };

    private static IReadOnlyList<Indicador> CalcularOperaciones()
    {
        var remesas = DataSourceFactory.CrearRemesas().GetAll().ToList();
        var hoy = DateTime.Today;

        var toneladasHoy = remesas
            .Where(r => r.Estado == EstadoRemesa.Recibida && r.LlegadaCentral?.Date == hoy)
            .Sum(r => r.PesoNetoT ?? 0m);

        var abiertas = remesas.Count(r => r.Estado is EstadoRemesa.Borrador or EstadoRemesa.Confirmada);

        // Un "frente" es una finca con actividad hoy: donde se está cortando ahora mismo.
        var frentes = remesas
            .Where(r => r.Estado != EstadoRemesa.Anulada && r.InicioCarga.Date == hoy)
            .Select(r => r.FincaId)
            .Distinct()
            .Count();

        return
        [
            new Indicador("Toneladas del día", $"{toneladasHoy:N2}", "caña recibida en el central"),
            new Indicador("Operaciones abiertas", abiertas.ToString(), "borradores y confirmadas"),
            new Indicador("Frentes activos", frentes.ToString(), "fincas con carga hoy"),
            // PROVISIONAL: pendiente de la definición de tiempo muerto del socio (qué cuenta como parada).
            new Indicador("Tiempo muerto", "—", "sin datos")
        ];
    }

    private static IReadOnlyList<Indicador> CalcularInventario()
    {
        var articulos = DataSourceFactory.CrearInventario().GetAll().ToList();

        var bajos = articulos.Count(a => a.Estado == StockStatus.Bajo);
        var agotados = articulos.Count(a => a.Estado == StockStatus.Agotado);
        var valor = articulos.Sum(a => a.ValorTotal);

        return
        [
            new Indicador("Artículos", articulos.Count.ToString(), "en el catálogo de almacén"),
            new Indicador("Bajo mínimo", bajos.ToString(), "por reponer"),
            new Indicador("Agotados", agotados.ToString(), "sin existencia"),
            new Indicador("Valor de inventario", $"{valor:N2}", "existencia valorada")
        ];
    }

    private static IReadOnlyList<Indicador> CalcularNomina()
    {
        // Los empleados activos salen de los dos padrones: administración/taller y campo.
        var administrativos = DataSourceFactory.CrearEmpleados().GetAll().Count(e => e.Activo);
        var campo = DataSourceFactory.CrearPersonalCampo().GetAll().Count(p => p.Activo);

        var liquidaciones = DataSourceFactory.CrearLiquidaciones().GetAll().ToList();
        var pendientes = liquidaciones.Count(l => l.Estado is EstadoLiquidacion.Borrador or EstadoLiquidacion.Cerrada);

        var horarios = new HorarioService(DataSourceFactory.CrearJornadas());
        var horas = horarios.HorasTotalesEnPeriodo(DateTime.Today.AddDays(-14), DateTime.Today);

        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var montoMes = liquidaciones
            .Where(l => l.Estado is EstadoLiquidacion.Cerrada or EstadoLiquidacion.Pagada)
            .Where(l => l.PeriodoHasta >= inicioMes)
            .Sum(l => l.Neto);

        return
        [
            new Indicador("Empleados activos", (administrativos + campo).ToString(), $"{administrativos} de nómina · {campo} de campo"),
            new Indicador("Liquidaciones pendientes", pendientes.ToString(), "por cerrar o pagar"),
            new Indicador("Horas del período", $"{horas:N1}", "jornadas cerradas, 14 días"),
            new Indicador("Monto del período", $"{montoMes:N2}", "liquidado en el mes")
        ];
    }

    private static IReadOnlyList<Indicador> CalcularFinanzas()
    {
        var cobrar = new FacturaClienteService(
            DataSourceFactory.CrearFacturasCliente(),
            DataSourceFactory.CrearRemesas(),
            new TarifaService(DataSourceFactory.CrearTarifas()));

        var pagar = new CuentasPorPagarService(DataSourceFactory.CrearFacturasProveedor());

        var porCobrar = cobrar.TotalPorCobrar();
        var porPagar = pagar.TotalPorPagar();
        var vencido = cobrar.TotalVencido() + pagar.TotalVencido();

        return
        [
            new Indicador("Por cobrar", $"{porCobrar:N2}", "facturado al ingenio"),
            new Indicador("Por pagar", $"{porPagar:N2}", "deuda con proveedores"),
            new Indicador("Vencido", $"{vencido:N2}", "cobros y pagos fuera de plazo"),
            new Indicador("Saldo neto", $"{porCobrar - porPagar:N2}", "por cobrar menos por pagar")
        ];
    }

    private static IReadOnlyList<Indicador> CalcularFlota()
    {
        var activos = DataSourceFactory.CrearActivosFlota().GetAll().ToList();
        var servicio = new MantenimientoService(
            DataSourceFactory.CrearMantenimientos(),
            DataSourceFactory.CrearActivosFlota(),
            DataSourceFactory.CrearReglasMantenimiento(),
            DataSourceFactory.CrearEventosOperacion(),
            DataSourceFactory.CrearRemesas());

        var operativos = activos.Count(a => a.Estado == EstadoActivo.Operativo);
        var enTaller = activos.Count(a => a.Estado == EstadoActivo.EnTaller);
        var disponibilidad = activos.Count == 0 ? 0 : operativos * 100 / activos.Count;
        var vencidos = servicio.CalcularRecomendaciones()
            .Count(r => r.Estado == EstadoRecomendacion.Vencido);

        return
        [
            new Indicador("Unidades activas", operativos.ToString(), $"de {activos.Count} en flota"),
            new Indicador("En taller", enTaller.ToString(), "unidades detenidas"),
            new Indicador("Disponibilidad", $"{disponibilidad} %", "flota operativa"),
            new Indicador("Mantenimientos vencidos", vencidos.ToString(), "revisiones por hacer")
        ];
    }
}
