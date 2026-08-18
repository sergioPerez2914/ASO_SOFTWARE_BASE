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
/// tenga una fuente de datos conectada (hoy solo Flota los calcula).
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
    /// <c>null</c> deja el marcador "—" genérico.
    /// </summary>
    private static IReadOnlyList<Indicador>? CalcularIndicadores(Modulo modulo)
    {
        if (modulo.Clave != "Flota")
            return null;

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
