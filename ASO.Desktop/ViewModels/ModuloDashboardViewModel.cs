using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Controls;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Una cifra del resumen de un módulo.
///
/// <paramref name="Estado"/> es lo que decide el color. Sin él las cuatro tarjetas se pintaban
/// iguales y había que leerlas una a una para enterarse de que algo iba mal: cuatro mantenimientos
/// vencidos y cuatro unidades en flota se veían exactamente igual.
/// </summary>
public sealed record Indicador(
    string Etiqueta,
    string Valor,
    string Nota,
    EstadoIndicador Estado = EstadoIndicador.Normal);

/// <summary>
/// Resumen de un módulo: sus indicadores y el acceso a cada submódulo.
///
/// Los indicadores se calculan <b>fuera del hilo de interfaz</b>. Antes se calculaban en el
/// constructor, de forma síncrona, en cada navegación y sin caché: entrar a Nómina disparaba
/// cuatro consultas más el servicio de horarios, y entrar a Flota construía cinco fuentes de datos
/// y recorría todas las reglas de mantenimiento. La ventana se quedaba congelada mientras tanto,
/// sin decir por qué, y volver al módulo lo repetía entero.
/// </summary>
public sealed class ModuloDashboardViewModel : ViewModelBase, IRecargable
{
    public event EventHandler<Submodulo>? SubmoduloSolicitado;

    public Modulo Modulo { get; }
    public ObservableCollection<Indicador> Indicadores { get; } = [];
    public IReadOnlyList<Submodulo> Submodulos { get; }

    public ICommand AbrirSubmoduloCommand { get; }

    private bool _cargando = true;

    /// <summary>Mientras dura, las tarjetas muestran un esqueleto en vez de un valor en blanco.</summary>
    public bool Cargando
    {
        get => _cargando;
        private set => SetProperty(ref _cargando, value);
    }

    private string _errorCarga = string.Empty;

    /// <summary>
    /// Vacío salvo que la consulta falle. Un resumen que no carga tiene que decirlo: en blanco
    /// se confunde con un módulo sin actividad, que es justo lo contrario.
    /// </summary>
    public string ErrorCarga
    {
        get => _errorCarga;
        private set
        {
            if (SetProperty(ref _errorCarga, value))
                OnPropertyChanged(nameof(HayError));
        }
    }

    public bool HayError => ErrorCarga.Length > 0;

    public ModuloDashboardViewModel(Modulo modulo) : this(modulo, SesionActual.Instancia) { }

    public ModuloDashboardViewModel(Modulo modulo, ISesionActual sesion)
    {
        Modulo = modulo;
        Submodulos = sesion.SubmodulosVisibles(modulo);

        AbrirSubmoduloCommand = new RelayCommand<Submodulo>(s => SubmoduloSolicitado?.Invoke(this, s));

        // Cuatro esqueletos mientras llegan los datos: la tarjeta ocupa ya su sitio, asi que la
        // rejilla no salta cuando el valor aparece.
        for (var i = 0; i < 4; i++)
            Indicadores.Add(new Indicador(string.Empty, string.Empty, string.Empty));

        _ = CargarIndicadores();

        _suscripcion = new SuscripcionACambios(Recargar);
    }

    private readonly SuscripcionACambios _suscripcion;

    /// <summary>
    /// Vuelve a calcular los indicadores. Reutiliza la carga en segundo plano de siempre, así que
    /// las tarjetas muestran su esqueleto mientras llegan los valores nuevos.
    /// </summary>
    public void Recargar()
    {
        Cargando = true;
        ErrorCarga = string.Empty;
        _ = CargarIndicadores();
    }

    public void Desconectar() => _suscripcion.Dispose();

    /// <summary>
    /// Trae los indicadores en segundo plano y los publica de vuelta en el hilo de interfaz.
    ///
    /// Cada método de cálculo abre y cierra su propio <c>DbContext</c> (ver
    /// <c>BD/SqlCrudDataSource</c>), así que sacarlos del hilo de interfaz es seguro.
    /// </summary>
    private async Task CargarIndicadores()
    {
        try
        {
            var calculados = await Task.Run(() => CalcularIndicadores(Modulo));

            Indicadores.Clear();
            foreach (var indicador in calculados ?? [])
                Indicadores.Add(indicador);
        }
        catch (Exception ex)
        {
            Indicadores.Clear();
            ErrorCarga = $"No se pudieron calcular los indicadores: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Indicadores de los módulos que tienen fuente conectada; <c>null</c> en los que no
    /// (Inicio, Peticiones, Administración y Configuración no tienen resumen).
    ///
    /// Las etiquetas se escriben aquí y solo aquí. Antes estaban además declaradas en
    /// <see cref="ModuloCatalogo"/>, con un comentario que decía que "deben coincidir" y nada que
    /// lo garantizara.
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

    /// <summary>Cuenta que crece mal: cero está bien, y a partir de ahí pide atención.</summary>
    private static EstadoIndicador SegunCuenta(int cuantos, int desdeCritico) => cuantos switch
    {
        0 => EstadoIndicador.Normal,
        _ when cuantos >= desdeCritico => EstadoIndicador.Critico,
        _ => EstadoIndicador.Atencion,
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
            new Indicador("Tiempo muerto", "—", "sin definir")
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
            new Indicador("Bajo mínimo", bajos.ToString(), "por reponer", SegunCuenta(bajos, 5)),
            // Agotado es peor que bajo mínimo: uno solo ya para un taller.
            new Indicador("Agotados", agotados.ToString(), "sin existencia", SegunCuenta(agotados, 1)),
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
            new Indicador("Liquidaciones pendientes", pendientes.ToString(), "por cerrar o pagar", SegunCuenta(pendientes, 5)),
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
        var saldo = porCobrar - porPagar;

        return
        [
            new Indicador("Por cobrar", $"{porCobrar:N2}", "facturado al ingenio"),
            new Indicador("Por pagar", $"{porPagar:N2}", "deuda con proveedores"),
            // Cualquier cifra vencida es dinero fuera de plazo, en un sentido o en el otro.
            new Indicador("Vencido", $"{vencido:N2}", "cobros y pagos fuera de plazo",
                vencido > 0 ? EstadoIndicador.Critico : EstadoIndicador.Normal),
            new Indicador("Saldo neto", $"{saldo:N2}", "por cobrar menos por pagar",
                saldo < 0 ? EstadoIndicador.Atencion : EstadoIndicador.Normal)
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
            new Indicador("En taller", enTaller.ToString(), "unidades detenidas", SegunCuenta(enTaller, 3)),
            // En plena zafra, por debajo de tres cuartos de flota operativa la cosecha se resiente.
            new Indicador("Disponibilidad", $"{disponibilidad} %", "flota operativa",
                activos.Count == 0 ? EstadoIndicador.Normal
                : disponibilidad < 60 ? EstadoIndicador.Critico
                : disponibilidad < 75 ? EstadoIndicador.Atencion
                : EstadoIndicador.Normal),
            new Indicador("Mantenimientos vencidos", vencidos.ToString(), "revisiones por hacer",
                SegunCuenta(vencidos, 3))
        ];
    }
}
