using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Apertura de una jornada. La persona sale de uno de los dos padrones según el conmutador, y
/// al guardar se copian su nombre, cargo y núcleo dentro de la jornada: el registro de
/// asistencia debe leerse igual dentro de dos zafras aunque la persona ya no esté.
/// </summary>
public sealed class JornadaEditorViewModel : CrudEditorViewModelBase<JornadaTrabajo>
{
    private readonly JornadaTrabajo _original;
    private readonly HorarioService _servicio;
    private readonly IReadOnlyList<Empleado> _administrativos;
    private readonly IReadOnlyList<PersonalCampo> _campo;

    public JornadaEditorViewModel(JornadaTrabajo original,
                                  HorarioService servicio,
                                  IEmpleadoDataSource empleados,
                                  IPersonalCampoDataSource personalCampo)
        : base(original)
    {
        _original = original;
        _servicio = servicio;

        _administrativos = empleados.GetAll().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();
        _campo = personalCampo.GetAll().Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();

        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        Turno = original.Turno;
        TipoPersonal = original.TipoPersonal;
        HoraEntrada = original.HoraEntrada == default
            ? DateTime.Now.ToString("HH:mm")
            : original.HoraEntrada.ToString("HH:mm");
        Observacion = original.Observacion;

        CambiarPadronCommand = new RelayCommand<string>(padron =>
            TipoPersonal = padron == "Campo" ? TipoPersonal.Campo : TipoPersonal.Administrativo);
    }

    /// <summary>Conmuta entre los dos padrones; al hacerlo se recarga el combo de personas.</summary>
    public ICommand CambiarPadronCommand { get; }

    public override string Titulo => "Registrar entrada";
    public override double AnchoEditor => 460;

    public IReadOnlyList<TurnoJornada> Turnos { get; } = Enum.GetValues<TurnoJornada>();

    /// <summary>Lista que ve el combo: cambia con el conmutador de padrón.</summary>
    public IEnumerable<object> Personas =>
        TipoPersonal == TipoPersonal.Administrativo ? _administrativos : _campo;

    private TipoPersonal _tipoPersonal;
    public TipoPersonal TipoPersonal
    {
        get => _tipoPersonal;
        set
        {
            if (SetProperty(ref _tipoPersonal, value))
            {
                PersonaSeleccionada = null;
                OnPropertyChanged(nameof(Personas));
                OnPropertyChanged(nameof(EsAdministrativo));
                OnPropertyChanged(nameof(EsCampo));
            }
        }
    }

    public bool EsAdministrativo => TipoPersonal == TipoPersonal.Administrativo;
    public bool EsCampo => TipoPersonal == TipoPersonal.Campo;

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
