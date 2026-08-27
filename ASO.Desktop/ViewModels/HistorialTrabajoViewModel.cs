using System.Collections.Generic;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Historial de trabajo de una persona: las jornadas que ha fichado y las horas que suman.
///
/// Ventana de SOLO LECTURA, con el mismo molde que <see cref="DetalleEventoViewModel"/>: reusa
/// <see cref="CrudEditorViewModelBase"/> —y con él la ventana, la escala y el tema— pero no guarda
/// nada, no valida, oculta Cancelar y su botón de acción solo cierra.
///
/// Sirve a los DOS padrones de Nómina · Empleados. Lo único que cambia entre ellos es el
/// <see cref="TipoPersonal"/> y de dónde sale el oficio (el cargo del empleado, el rol del personal
/// de campo): la jornada ya distingue ambos padrones, así que no hacen falta dos ventanas.
///
/// Quién decide qué es el historial es <see cref="HorarioService.HistorialDe"/>, no este
/// ViewModel: contar las horas es una regla de dominio —una jornada abierta no suma— y aquí solo
/// se pinta lo que devuelve.
/// </summary>
public sealed class HistorialTrabajoViewModel : CrudEditorViewModelBase
{
    private readonly string _nombre;
    private readonly string _cargoORol;
    private readonly TipoPersonal _tipo;
    private readonly HistorialTrabajo _historial;

    public HistorialTrabajoViewModel(TipoPersonal tipo,
                                     int personaId,
                                     string nombre,
                                     string cargoORol,
                                     HorarioService servicio)
    {
        _tipo = tipo;
        _nombre = nombre;
        _cargoORol = cargoORol;
        _historial = servicio.HistorialDe(tipo, personaId);
    }

    public override string Titulo => $"Historial de trabajo · {_nombre}";

    public override string TextoAccion => "Cerrar";

    public override bool MuestraCancelar => false;

    /// <summary>Lleva una lista dentro, que es lo que pide el ancho amplio.</summary>
    public override double AnchoEditor => Ancho.Amplio;

    public IReadOnlyList<JornadaTrabajo> Jornadas => _historial.Jornadas;

    /// <summary>Quién es: su oficio y de qué padrón sale, que es lo que el título no dice.</summary>
    public string Subtitulo
    {
        get
        {
            var padron = _tipo == TipoPersonal.Administrativo ? "Administrativo" : "Personal de campo";
            return string.IsNullOrWhiteSpace(_cargoORol) ? padron : $"{_cargoORol} · {padron}";
        }
    }

    public string ResumenHoras =>
        $"{_historial.HorasTotales:N2} h en {_historial.JornadasCerradas} jornada(s) cerrada(s)";

    /// <summary>La tabla y el estado vacío se turnan: aquí no hay hueco para superponerlos.</summary>
    public bool HayJornadas => _historial.Jornadas.Count > 0;

    public bool TieneJornadaAbierta => _historial.Abierta is not null;

    /// <summary>
    /// Las horas de arriba no la incluyen —todavía no se sabe cuánto durará—, así que hay que
    /// decir que está ahí o el resumen parecería quedarse corto.
    /// </summary>
    public string AvisoJornadaAbierta => _historial.Abierta is { } abierta
        ? $"Jornada abierta desde el {abierta.EntradaTexto}; sus horas no entran en el resumen."
        : string.Empty;

    protected override bool Validar(out string? error)
    {
        // No hay nada que validar: el historial no escribe.
        error = null;
        return true;
    }
}
