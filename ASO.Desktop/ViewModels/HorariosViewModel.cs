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
/// Reusa el listado de <see cref="CrudViewModelBase{T, TId}"/> por el filtrado y la búsqueda,
/// pero desactiva editar y eliminar: una asistencia registrada no se reescribe (ver
/// <see cref="HorarioService"/>). El alta pasa igualmente por el servicio, que es quien
/// impide abrir dos jornadas a la misma persona.
/// </summary>
public sealed class HorariosViewModel : CrudViewModelBase<JornadaTrabajo, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IJornadaDataSource _jornadas;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly HorarioService _servicio;

    private string _filtroEstado = FiltroTodas;

    public event EventHandler? VolverSolicitado;

    public HorariosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearJornadas(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private HorariosViewModel(Modulo modulo,
                              Submodulo submodulo,
                              IJornadaDataSource jornadas,
                              IServicioDialogo dialogos,
                              ISesionActual sesion)
        : base(jornadas, dialogos, sesion)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        _jornadas = jornadas;
        _dialogos = dialogos;
        _sesionActual = sesion;
        _servicio = new HorarioService(jornadas);

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        RegistrarEntradaCommand = new RelayCommand(RegistrarEntrada,
            () => _sesionActual.Puede("Horarios.Crear"));

        RegistrarSalidaCommand = new RelayCommand(RegistrarSalida,
            () => SelectedItem is { } j && _servicio.PuedeRegistrarSalida(j) && _sesionActual.Puede("Horarios.RegistrarSalida"));
    }

    // --- Encabezado de la pantalla ---
    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }
    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }
    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand RegistrarEntradaCommand { get; }
    public ICommand RegistrarSalidaCommand { get; }

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
    /// Cambios de turno registrados en Operaciones. Es informativo: la conciliación entre el
    /// evento del frente y la jornada del trabajador queda pendiente de definición del socio.
    /// </summary>
    public string ResumenCambiosTurno
    {
        get
        {
            var desde = DateTime.Today.AddDays(-7);
            var cambios = DataSourceFactory.CrearEventosOperacion().GetAll()
                .Count(e => e.Tipo == TipoEventoOperacion.CambioTurno && e.FechaHora >= desde);

            return $"{cambios} cambio(s) de turno registrados en operaciones (7 días)";
        }
    }

    // --- Puntos de extensión del CRUD ---

    protected override string ModuloPermiso => "Horarios";

    protected override bool CoincideBusqueda(JornadaTrabajo item, string texto) =>
        item.PersonaNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.CargoORol.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.NucleoCodigo.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(JornadaTrabajo item) => _filtroEstado switch
    {
        "Abiertas" => item.EstaAbierta,
        "Cerradas" => !item.EstaAbierta,
        "Campo" => item.TipoPersonal == TipoPersonal.Campo,
        "Administrativos" => item.TipoPersonal == TipoPersonal.Administrativo,
        _ => true
    };

    /// <summary>Una asistencia registrada no se reescribe: la vista no ofrece editar ni eliminar.</summary>
    protected override bool PuedeEditar(JornadaTrabajo item) => false;

    protected override bool PuedeEliminar(JornadaTrabajo item) => false;

    // El alta no pasa por el CRUD heredado (ver RegistrarEntrada): la jornada nace en el servicio.
    protected override JornadaTrabajo CrearNuevo() =>
        throw new NotSupportedException("Las jornadas se abren con RegistrarEntradaCommand, que pasa por HorarioService.");

    protected override CrudEditorViewModelBase<JornadaTrabajo> CrearEditor(JornadaTrabajo item) =>
        new JornadaEditorViewModel(item, _servicio,
            DataSourceFactory.CrearEmpleados(), DataSourceFactory.CrearPersonalCampo());

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
            Items.Add(registrada);
            SelectedItem = registrada;
            ItemsView.Refresh();
            OnPropertyChanged(nameof(ResumenHoras));
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

            var indice = Items.IndexOf(jornada);
            if (indice >= 0)
                Items[indice] = cerrada;

            SelectedItem = cerrada;
            ItemsView.Refresh();
            OnPropertyChanged(nameof(ResumenHoras));
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo registrar la salida", ex.Message);
        }
    }
}
