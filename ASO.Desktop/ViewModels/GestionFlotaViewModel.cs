using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Flota · Gestión de Flota: tarjetas de todos los activos y, al seleccionar una, la ficha
/// completa con indicadores, revisiones, últimos mantenimientos e historial de uso.
///
/// Las reglas viven en <see cref="FlotaService"/> y <see cref="MantenimientoService"/>; los
/// comandos solo piden la acción y reflejan el resultado.
/// </summary>
public sealed class GestionFlotaViewModel : PantallaViewModelBase
{
    private const string FiltroTodos = "Todos";

    private readonly FlotaService _flota;
    private readonly MantenimientoService _mantenimiento;
    private readonly IServicioDialogo _dialogos;
    private readonly SolicitudesDeCambio _solicitudes;
    private readonly ISesionActual _sesion;
    private readonly IRemesaDataSource _remesas;
    private readonly IActivoFlotaDataSource _activos;

    /// <summary>Se dispara al pedir volver al dashboard del módulo; la ventana principal navega.</summary>

    public GestionFlotaViewModel(Modulo modulo, Submodulo submodulo, ISesionActual? sesion = null)
        : base(modulo, submodulo)
    {
        var activos = DataSourceFactory.CrearActivosFlota();
        _activos = activos;
        var mantenimientos = DataSourceFactory.CrearMantenimientos();
        _remesas = DataSourceFactory.CrearRemesas();

        _flota = new FlotaService(activos, _remesas, mantenimientos, DataSourceFactory.CrearValesCombustible());
        _mantenimiento = new MantenimientoService(mantenimientos, activos,
            DataSourceFactory.CrearReglasMantenimiento(), DataSourceFactory.CrearEventosOperacion(), _remesas);
        _dialogos = new ServicioDialogo();
        _solicitudes = new SolicitudesDeCambio(_sesion, _dialogos);
        _sesion = sesion ?? SesionActual.Instancia;

        Activos = new ObservableCollection<ActivoFlota>(activos.GetAll());
        ActivosView = CollectionViewSource.GetDefaultView(Activos);
        ActivosView.Filter = Filtrar;
        ActivosView.SortDescriptions.Add(new SortDescription(nameof(ActivoFlota.Codigo), ListSortDirection.Ascending));

        AbrirDetalleCommand = new RelayCommand<ActivoFlota>(activo => ActivoSeleccionado = activo);
        CerrarDetalleCommand = new RelayCommand(() => ActivoSeleccionado = null);

        // Alta y edicion de flota son de administrador; el remesero las SOLICITA (el boton
        // sigue activo y abre una peticion en vez de quedarse gris).
        NuevoActivoCommand = new RelayCommand(NuevoActivo,
            () => _solicitudes.PuedeIntentar(Permisos.Flota.Crear));
        EditarActivoCommand = new RelayCommand(EditarActivo,
            () => ActivoSeleccionado is not null && _solicitudes.PuedeIntentar(Permisos.Flota.Editar));

        CambiarEstadoCommand = new RelayCommand<EstadoActivo>(CambiarEstado,
            estado => ActivoSeleccionado is { } a && a.Estado != estado && _sesion.Puede("Flota.CambiarEstado"));

        RegistrarMantenimientoCommand = new RelayCommand(RegistrarMantenimiento,
            () => ActivoSeleccionado is not null && _sesion.Puede("Mantenimiento.Registrar"));

        CambiarFiltroTipoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroTipo = filtro;
            ActivosView.Refresh();
        });
    }

    // --- Encabezado ---

    public ICommand AbrirDetalleCommand { get; }
    public ICommand CerrarDetalleCommand { get; }
    public ICommand NuevoActivoCommand { get; }
    public ICommand EditarActivoCommand { get; }
    public ICommand CambiarEstadoCommand { get; }
    public ICommand RegistrarMantenimientoCommand { get; }
    public ICommand CambiarFiltroTipoCommand { get; }

    public ObservableCollection<ActivoFlota> Activos { get; }
    public ICollectionView ActivosView { get; }

    private ActivoFlota? _activoSeleccionado;
    public ActivoFlota? ActivoSeleccionado
    {
        get => _activoSeleccionado;
        set
        {
            if (!SetProperty(ref _activoSeleccionado, value)) return;

            RecargarDetalle();
            OnPropertyChanged(nameof(HayDetalle));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HayDetalle => ActivoSeleccionado is not null;

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (SetProperty(ref _textoBusqueda, value)) ActivosView.Refresh(); }
    }

    private string _filtroTipo = FiltroTodos;

    // --- Detalle ---
    public ObservableCollection<UsoActivoItem> HistorialUso { get; } = [];
    public ObservableCollection<MantenimientoRegistro> UltimosMantenimientos { get; } = [];
    public ObservableCollection<RecomendacionMantenimiento> Recomendaciones { get; } = [];

    private string _ultimoMantenimientoTexto = "—";
    public string UltimoMantenimientoTexto
    {
        get => _ultimoMantenimientoTexto;
        private set => SetProperty(ref _ultimoMantenimientoTexto, value);
    }

    private string _proximaRevisionTexto = "—";
    public string ProximaRevisionTexto
    {
        get => _proximaRevisionTexto;
        private set => SetProperty(ref _proximaRevisionTexto, value);
    }

    private bool Filtrar(object obj)
    {
        if (obj is not ActivoFlota activo) return false;

        var pasaTipo = _filtroTipo switch
        {
            FiltroTodos => true,
            "Transporte" => activo.EsTransporte,
            _ => activo.TipoTexto == _filtroTipo
        };
        if (!pasaTipo) return false;

        var texto = TextoBusqueda.Trim();
        return texto.Length == 0
               || Contiene(activo.Codigo, texto)
               || Contiene(activo.Placa, texto)
               || Contiene(activo.Marca, texto)
               || Contiene(activo.Modelo, texto);
    }

    private static bool Contiene(string valor, string texto)
        => valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Relee la flota y, si hay una ficha abierta, su detalle: al confirmar un vale o registrar
    /// un mantenimiento cambian el horometro y las recomendaciones del activo que se esta viendo.
    /// </summary>
    public override void Recargar()
    {
        RecargarActivos(_activos);
        RecargarDetalle();
    }

    private void RecargarActivos(IActivoFlotaDataSource fuente)
    {
        var seleccionadoId = _idActivoASeleccionar ?? ActivoSeleccionado?.Id;
        _idActivoASeleccionar = null;

        Activos.Clear();
        foreach (var activo in fuente.GetAll())
            Activos.Add(activo);

        ActivoSeleccionado = seleccionadoId is { } id
            ? Activos.FirstOrDefault(a => a.Id == id)
            : null;
    }

    private void RecargarDetalle()
    {
        HistorialUso.Clear();
        UltimosMantenimientos.Clear();
        Recomendaciones.Clear();
        UltimoMantenimientoTexto = "—";
        ProximaRevisionTexto = "—";

        if (ActivoSeleccionado is not { } activo)
            return;

        foreach (var uso in _flota.ObtenerHistorialUso(activo))
            HistorialUso.Add(uso);

        var mantenimientos = _mantenimiento.ObtenerPorActivo(activo.Id);
        foreach (var registro in mantenimientos.Take(5))
            UltimosMantenimientos.Add(registro);

        foreach (var recomendacion in _mantenimiento.CalcularParaActivo(activo))
            Recomendaciones.Add(recomendacion);

        UltimoMantenimientoTexto = mantenimientos.Count > 0
            ? $"{FormatearHace(mantenimientos[0].Fecha)} — {mantenimientos[0].TipoTexto}"
            : "Sin registros";

        var pendiente = Recomendaciones.FirstOrDefault(r => r.Estado != EstadoRecomendacion.AlDia);
        ProximaRevisionTexto = pendiente is not null
            ? $"{pendiente.Regla.Revision} ({pendiente.EstadoTexto.ToLowerInvariant()})"
            : "Al día";
    }

    private static string FormatearHace(DateTime fecha)
    {
        var dias = (DateTime.Today - fecha.Date).Days;
        return dias switch
        {
            0 => "Hoy",
            1 => "Ayer",
            _ => $"Hace {dias} días"
        };
    }

    private void NuevoActivo()
    {
        if (_solicitudes.RequierePeticion(Permisos.Flota.Crear))
        {
            _solicitudes.Solicitar(Permisos.Flota.Crear, "Agregar activo de flota",
                nameof(ActivoFlota), string.Empty, "Alta de una unidad nueva");
            return;
        }

        var editor = new ActivoEditorViewModel(new ActivoFlota { Estado = EstadoActivo.Operativo });
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            _idActivoASeleccionar = _flota.Agregar(editor.ObtenerResultado()).Id;
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Flota", ex.Message);
        }
    }

    private void EditarActivo()
    {
        if (ActivoSeleccionado is not { } actual)
            return;

        if (_solicitudes.RequierePeticion(Permisos.Flota.Editar))
        {
            _solicitudes.Solicitar(Permisos.Flota.Editar, "Modificar activo de flota",
                nameof(ActivoFlota), actual.Id.ToString(), actual.Etiqueta);
            return;
        }

        var editor = new ActivoEditorViewModel(actual);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            _idActivoASeleccionar = _flota.Actualizar(editor.ObtenerResultado()).Id;
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Flota", ex.Message);
        }
    }

    private void CambiarEstado(EstadoActivo nuevoEstado)
    {
        if (ActivoSeleccionado is not { } actual)
            return;

        var etiquetaEstado = nuevoEstado switch
        {
            EstadoActivo.Operativo => "Operativo",
            EstadoActivo.EnTaller => "En taller",
            _ => "Fuera de servicio"
        };

        if (!_dialogos.Confirmar("Cambiar estado",
                $"¿Pasar {actual.Etiqueta} a \"{etiquetaEstado}\"?"))
            return;

        try
        {
            _idActivoASeleccionar = _flota.CambiarEstado(actual, nuevoEstado).Id;
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Flota", ex.Message);
        }
    }

    private void RegistrarMantenimiento()
    {
        if (ActivoSeleccionado is not { } activo)
            return;

        var editor = new MantenimientoEditorViewModel(new MantenimientoRegistro(),
            [.. Activos], _remesas, preseleccionado: activo);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            // La lectura del activo pudo cambiar, pero ya no hay que ir a buscarla: guardar
            // dispara la recarga de la pantalla, que relee la flota y el detalle abierto.
            _mantenimiento.Registrar(editor.ObtenerResultado());
            _idActivoASeleccionar = activo.Id;
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Mantenimiento", ex.Message);
        }
    }

    /// <summary>Id del activo que debe quedar seleccionado tras la próxima recarga.</summary>
    private int? _idActivoASeleccionar;
}
