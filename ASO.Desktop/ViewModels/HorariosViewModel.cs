using System;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Nómina · Gestión de Horarios: apertura y cierre de jornadas.
///
/// El frente de trabajo se elige una vez arriba, no en cada diálogo: en campo se ficha a varias
/// personas seguidas contra la misma remesa, y repetir la elección en cada alta sería teclear lo
/// mismo diez veces. Ese frente hace dos cosas: acota la tabla a sus jornadas y es el que se
/// estampa en las entradas nuevas de personal de campo.
///
/// Reusa el listado de <see cref="CrudViewModelBase{T, TId}"/> por el filtrado y la búsqueda,
/// pero desactiva editar y eliminar: una asistencia registrada no se reescribe (ver
/// <see cref="HorarioService"/>). El alta pasa igualmente por el servicio, que es quien
/// impide abrir dos jornadas a la misma persona.
/// </summary>
public sealed class HorariosViewModel : PantallaCrudViewModel<JornadaTrabajo, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IJornadaDataSource _jornadas;
    private readonly IEventoOperacionDataSource _eventos;
    private readonly IRemesaDataSource _remesas;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly HorarioService _servicio;

    private string _filtroEstado = FiltroTodas;

    public HorariosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearJornadas(),
               DataSourceFactory.CrearEventosOperacion(), DataSourceFactory.CrearRemesas(),
               new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private HorariosViewModel(Modulo modulo,
                              Submodulo submodulo,
                              IJornadaDataSource jornadas,
                              IEventoOperacionDataSource eventos,
                              IRemesaDataSource remesas,
                              IServicioDialogo dialogos,
                              ISesionActual sesion)
        : base(modulo, submodulo, jornadas, dialogos, sesion)
    {
        _jornadas = jornadas;
        _eventos = eventos;
        _remesas = remesas;
        _dialogos = dialogos;
        _sesionActual = sesion;
        _servicio = new HorarioService(jornadas, eventos, remesas);

        CargarFrentes();

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        RegistrarEntradaCommand = new RelayCommand(RegistrarEntrada,
            () => _sesionActual.Puede(Permisos.Horarios.Crear));

        RegistrarSalidaCommand = new RelayCommand(RegistrarSalida,
            () => SelectedItem is { } j && _servicio.PuedeRegistrarSalida(j)
                  && _sesionActual.Puede(Permisos.Horarios.RegistrarSalida));
    }

    // --- Encabezado de la pantalla ---

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand RegistrarEntradaCommand { get; }
    public ICommand RegistrarSalidaCommand { get; }

    /// <summary>Frentes elegibles; el primero, "Todos los frentes", quita el filtro.</summary>
    public IReadOnlyList<OpcionRemesa> FrentesEnCurso { get; private set; } = [];

    private OpcionRemesa? _frenteSeleccionado;
    public OpcionRemesa? FrenteSeleccionado
    {
        get => _frenteSeleccionado;
        set
        {
            if (!SetProperty(ref _frenteSeleccionado, value)) return;

            ItemsView.Refresh();
            OnPropertyChanged(nameof(HayFrente));
            OnPropertyChanged(nameof(AyudaFrente));
        }
    }

    /// <summary>Remesa contra la que se ficha, o null si se están viendo todos los frentes.</summary>
    public Remesa? Frente => FrenteSeleccionado?.Remesa;

    public bool HayFrente => Frente is not null;

    public string AyudaFrente => Frente is null
        ? "Elija un frente para fichar al personal de campo"
        : "Las entradas de campo se registran en este frente";

    /// <summary>
    /// Relee los frentes vivos conservando el elegido. Se busca por Id y no por referencia: la
    /// recarga trae objetos nuevos y la opción anterior ya no está en la lista.
    /// </summary>
    private void CargarFrentes()
    {
        var elegidoId = Frente?.Id;

        FrentesEnCurso =
        [
            new OpcionRemesa(null, "Todos los frentes"),
            .. _remesas.GetAll()
                .Where(r => r.Estado is EstadoRemesa.Borrador or EstadoRemesa.Confirmada)
                .OrderByDescending(r => r.InicioCarga)
                .Select(r => new OpcionRemesa(r))
        ];

        OnPropertyChanged(nameof(FrentesEnCurso));

        _frenteSeleccionado = elegidoId is { } id
            ? FrentesEnCurso.FirstOrDefault(o => o.Remesa?.Id == id) ?? FrentesEnCurso[0]
            : FrentesEnCurso[0];

        OnPropertyChanged(nameof(FrenteSeleccionado));
        OnPropertyChanged(nameof(Frente));
        OnPropertyChanged(nameof(HayFrente));
        OnPropertyChanged(nameof(AyudaFrente));
    }

    /// <summary>Los frentes también se renuevan: una remesa nueva debe poder elegirse sin salir.</summary>
    public override void Recargar()
    {
        CargarFrentes();
        base.Recargar();
    }

    /// <summary>Horas cerradas de las dos últimas semanas: el mismo corte que usa el dashboard.</summary>
    public string ResumenHoras
    {
        get
        {
            var horas = _servicio.HorasTotalesEnPeriodo(DateTime.Today.AddDays(-14), DateTime.Today);
            var abiertas = _jornadas.GetAll().Count(j => j.EstaAbierta);
            return $"{horas:N2} h en los últimos 14 días · {abiertas} jornada(s) abierta(s)";
        }
    }

    /// <summary>
    /// Movimientos de personal en los frentes: los publica <see cref="HorarioService"/> al abrir
    /// y al cerrar cada jornada de campo, y se leen desde la línea de tiempo de la remesa.
    /// </summary>
    public string ResumenCambiosTurno
    {
        get
        {
            var desde = DateTime.Today.AddDays(-7);
            var movimientos = _eventos.GetAll()
                .Count(e => e.Tipo == TipoEventoOperacion.CambioTurno && e.FechaHora >= desde);

            return $"{movimientos} movimiento(s) de personal en frentes (7 días)";
        }
    }

    // --- Puntos de extensión del CRUD ---

    protected override string ModuloPermiso => "Horarios";

    protected override bool CoincideBusqueda(JornadaTrabajo item, string texto) =>
        item.PersonaNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.CargoORol.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.NucleoCodigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.RemesaTexto.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(JornadaTrabajo item)
    {
        if (Frente is { } frente && item.RemesaId != frente.Id)
            return false;

        return _filtroEstado switch
        {
            "Abiertas" => item.EstaAbierta,
            "Cerradas" => !item.EstaAbierta,
            "Campo" => item.TipoPersonal == TipoPersonal.Campo,
            "Administrativos" => item.TipoPersonal == TipoPersonal.Administrativo,
            _ => true
        };
    }

    /// <summary>Una asistencia registrada no se reescribe: la vista no ofrece editar ni eliminar.</summary>
    protected override bool PuedeEditar(JornadaTrabajo item) => false;

    protected override bool PuedeEliminar(JornadaTrabajo item) => false;

    // El alta no pasa por el CRUD heredado (ver RegistrarEntrada): la jornada nace en el servicio.
    protected override JornadaTrabajo CrearNuevo() =>
        throw new NotSupportedException("Las jornadas se abren con RegistrarEntradaCommand, que pasa por HorarioService.");

    protected override CrudEditorViewModelBase<JornadaTrabajo> CrearEditor(JornadaTrabajo item) =>
        new JornadaEditorViewModel(item, _servicio,
            DataSourceFactory.CrearEmpleados(), DataSourceFactory.CrearPersonalCampo(), Frente);

    // --- Acciones ---

    private void RegistrarEntrada()
    {
        var nueva = new JornadaTrabajo
        {
            Fecha = DateTime.Today,
            CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0
        };

        var editor = CrearEditor(nueva);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var registrada = _servicio.Registrar(editor.ObtenerResultado());
            SeleccionarTrasRecargar(registrada.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo registrar la entrada", ex.Message);
        }
    }

    private void RegistrarSalida()
    {
        if (SelectedItem is not { } jornada)
            return;

        var editor = new SalidaJornadaEditorViewModel(jornada);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var cerrada = _servicio.RegistrarSalida(jornada, editor.Salida);
            SeleccionarTrasRecargar(cerrada.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo registrar la salida", ex.Message);
        }
    }
}
