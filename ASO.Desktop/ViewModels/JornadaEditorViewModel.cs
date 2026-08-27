using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Apertura de una jornada. Al guardar se copian el nombre, el cargo y el núcleo de la persona
/// dentro de la jornada: el registro de asistencia debe leerse igual dentro de dos zafras aunque
/// la persona ya no esté.
///
/// Ni el PADRÓN ni el FRENTE se eligen aquí: los dos llegan decididos desde la pantalla, que es
/// donde se escogen una vez para toda una tanda de altas. El editor solo los muestra, para que se
/// vea de qué padrón sale la persona y a qué remesa va a parar la jornada. El padrón dejó de
/// preguntarse cuando la pantalla lo separó en pestañas: tenerlo también aquí permitía guardar
/// una jornada del otro padrón, que desaparecía de la tabla nada más crearse.
///
/// Las reglas duras las aplica <see cref="HorarioService.Registrar"/>; aquí solo se guía el
/// formulario.
/// </summary>
public sealed class JornadaEditorViewModel : CrudEditorViewModelBase<JornadaTrabajo>
{
    private readonly JornadaTrabajo _original;
    private readonly HorarioService _servicio;
    private readonly Remesa? _frente;
    private readonly IReadOnlyList<Empleado> _administrativos;
    private readonly IReadOnlyList<PersonalCampo> _campo;

    public JornadaEditorViewModel(JornadaTrabajo original,
                                  HorarioService servicio,
                                  IEmpleadoDataSource empleados,
                                  IPersonalCampoDataSource personalCampo,
                                  Remesa? frente)
    {
        _original = original;
        _servicio = servicio;
        _frente = frente;

        _administrativos = empleados.GetAll().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();
        _campo = personalCampo.GetAll().Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();

        TipoPersonal = original.TipoPersonal;
        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        Turno = original.Turno;
        HoraEntrada = original.HoraEntrada == default
            ? DateTime.Now.ToString("HH:mm")
            : original.HoraEntrada.ToString("HH:mm");
        Observacion = original.Observacion;
    }

    public override string Titulo => "Registrar entrada";
    public override double AnchoEditor => Ancho.Estandar;

    public IReadOnlyList<TurnoJornada> Turnos { get; } = Enum.GetValues<TurnoJornada>();

    /// <summary>Frente elegido en la pantalla; solo se enseña, no se edita.</summary>
    public string FrenteTexto => _frente is { } r
        ? $"Nº {r.Id} · {r.FincaNombre} · {r.UbicacionTexto}"
        : "Sin frente elegido";

    /// <summary>Lista que ve el combo: cambia con el conmutador de padrón.</summary>
    public IEnumerable<object> Personas =>
        TipoPersonal == TipoPersonal.Administrativo ? _administrativos : _campo;

    /// <summary>Padrón de la pestaña desde la que se abrió; fijo mientras dure el diálogo.</summary>
    public TipoPersonal TipoPersonal { get; }

    public bool EsAdministrativo => TipoPersonal == TipoPersonal.Administrativo;
    public bool EsCampo => TipoPersonal == TipoPersonal.Campo;

    /// <summary>Se enseña, no se edita, igual que <see cref="FrenteTexto"/>.</summary>
    public string PadronTexto => EsCampo ? "Personal de campo" : "Administrativo";

    private object? _personaSeleccionada;
    public object? PersonaSeleccionada
    {
        get => _personaSeleccionada;
        set => SetProperty(ref _personaSeleccionada, value);
    }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private TurnoJornada _turno;
    public TurnoJornada Turno
    {
        get => _turno;
        set => SetProperty(ref _turno, value);
    }

    private string _horaEntrada = string.Empty;
    public string HoraEntrada
    {
        get => _horaEntrada;
        set => SetProperty(ref _horaEntrada, value);
    }

    private string _observacion = string.Empty;
    public string Observacion
    {
        get => _observacion;
        set => SetProperty(ref _observacion, value);
    }

    protected override bool Validar(out string? error)
    {
        if (PersonaSeleccionada is null)
        {
            error = "Seleccione la persona que inicia la jornada.";
            return false;
        }

        if (!TryLeerHora(out _))
        {
            error = "La hora de entrada debe tener el formato HH:mm (por ejemplo, 06:30).";
            return false;
        }

        if (EsCampo && _frente is null)
        {
            error = "El personal de campo se ficha contra un frente. " +
                    "Elija la remesa en el selector \"Frente\" de la pantalla y vuelva a intentarlo.";
            return false;
        }

        var (personaId, nombre, _, _) = DatosPersona();
        if (_servicio.TieneJornadaAbierta(TipoPersonal, personaId, out var abierta))
        {
            error = $"{nombre} tiene una jornada abierta desde el {abierta!.EntradaTexto}. " +
                    "Regístrele la salida antes de abrir otra.";
            return false;
        }

        error = null;
        return true;
    }

    public override JornadaTrabajo ObtenerResultado()
    {
        var (personaId, nombre, cargo, nucleo) = DatosPersona();
        TryLeerHora(out var hora);

        var jornada = _original.Clonar();
        jornada.Fecha = Fecha;
        jornada.TipoPersonal = TipoPersonal;
        jornada.PersonaId = personaId;
        jornada.PersonaNombre = nombre;
        jornada.CargoORol = cargo;
        jornada.NucleoCodigo = nucleo;
        jornada.Turno = Turno;
        jornada.HoraEntrada = Fecha.Date + hora;
        jornada.RemesaId = EsCampo ? _frente?.Id : null;
        jornada.Observacion = Observacion.Trim();

        return jornada;
    }

    private (int Id, string Nombre, string Cargo, string Nucleo) DatosPersona() => PersonaSeleccionada switch
    {
        Empleado e => (e.Id, e.Nombre, e.Cargo, string.Empty),
        PersonalCampo p => (p.Id, p.Nombre, p.RolTexto, p.NucleoCodigo),
        _ => (0, string.Empty, string.Empty, string.Empty)
    };

    private bool TryLeerHora(out TimeSpan hora) =>
        TimeSpan.TryParseExact(HoraEntrada?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out hora);
}
