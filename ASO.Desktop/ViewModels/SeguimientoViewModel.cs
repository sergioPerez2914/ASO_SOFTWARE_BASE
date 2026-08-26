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
/// Operaciones · Seguimiento: maestro-detalle de la historia de cada remesa. A la izquierda las
/// remesas, a la derecha su línea de tiempo.
///
/// No hereda de <see cref="CrudViewModelBase{T, TId}"/> a propósito: aquí no se dan de alta ni se
/// borran remesas, solo se consultan y se les agregan notas.
/// </summary>
public sealed class SeguimientoViewModel : PantallaViewModelBase
{
    private const string FiltroTodas = "Todas";

    private readonly IRemesaDataSource _remesas;
    private readonly SeguimientoService _servicio;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    /// <summary>Se dispara al pedir volver al dashboard del módulo; la ventana principal navega.</summary>

    public SeguimientoViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo,
               DataSourceFactory.CrearRemesas(),
               new SeguimientoService(DataSourceFactory.CrearEventosOperacion(),
                                      DataSourceFactory.CrearFacturasCliente(),
                                      DataSourceFactory.CrearLiquidaciones(),
                                      DataSourceFactory.CrearPeticiones(),
                                      DataSourceFactory.CrearJornadas(),
                                      DataSourceFactory.CrearMantenimientos()),
               new ServicioDialogo(),
               SesionActual.Instancia)
    {
    }

    private SeguimientoViewModel(Modulo modulo,
                                 Submodulo submodulo,
                                 IRemesaDataSource remesas,
                                 SeguimientoService servicio,
                                 IServicioDialogo dialogos,
                                 ISesionActual sesion)
        : base(modulo, submodulo)
    {
        _remesas = remesas;
        _servicio = servicio;
        _dialogos = dialogos;
        _sesion = sesion;

        Remesas = new ObservableCollection<Remesa>(_remesas.GetAll());
        RemesasView = CollectionViewSource.GetDefaultView(Remesas);
        RemesasView.Filter = Filtrar;
        RemesasView.SortDescriptions.Add(
            new SortDescription(nameof(Remesa.InicioCarga), ListSortDirection.Descending));

        AgregarNotaCommand = new RelayCommand(AgregarNota,
            () => RemesaSeleccionada is not null && _sesion.Puede(Permisos.Seguimiento.AgregarNota));

        // Consultar la ficha no pide permiso: quien puede ver el submódulo puede leer su historia.
        VerDetalleEventoCommand = new RelayCommand<EventoTimelineItem>(VerDetalleEvento);

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            RemesasView.Refresh();
        });
    }

    // --- Encabezado de la pantalla (mismo patrón que las demás) ---

    public ICommand AgregarNotaCommand { get; }
    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand VerDetalleEventoCommand { get; }

    public ObservableCollection<Remesa> Remesas { get; }
    public ICollectionView RemesasView { get; }

    /// <summary>Eventos de la remesa seleccionada, en orden ascendente.</summary>
    public ObservableCollection<EventoTimelineItem> Eventos { get; } = [];

    private Remesa? _remesaSeleccionada;
    public Remesa? RemesaSeleccionada
    {
        get => _remesaSeleccionada;
        set
        {
            if (!SetProperty(ref _remesaSeleccionada, value)) return;

            ReconstruirTimeline();
            OnPropertyChanged(nameof(HaySeleccion));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HaySeleccion => RemesaSeleccionada is not null;

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (SetProperty(ref _textoBusqueda, value)) RemesasView.Refresh(); }
    }

    private string _filtroEstado = FiltroTodas;

    private bool Filtrar(object obj)
    {
        if (obj is not Remesa remesa) return false;

        if (_filtroEstado != FiltroTodas && remesa.EstadoTexto != _filtroEstado)
            return false;

        var texto = TextoBusqueda.Trim();
        return texto.Length == 0
               || Contiene(remesa.Id.ToString(), texto)
               || Contiene(remesa.FincaCodigoCam, texto)
               || Contiene(remesa.FincaNombre, texto)
               || Contiene(remesa.VehiculoPlaca, texto);
    }

    private static bool Contiene(string valor, string texto)
        => valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    /// <summary>Recarga desde la fuente conservando la remesa que se estaba mirando.</summary>
    public override void Recargar()
    {
        var seleccionadaId = RemesaSeleccionada?.Id;

        Remesas.Clear();
        foreach (var remesa in _remesas.GetAll())
            Remesas.Add(remesa);

        RemesaSeleccionada = seleccionadaId is { } id
            ? Remesas.FirstOrDefault(r => r.Id == id)
            : null;

        ReconstruirTimeline();
    }

    private void AgregarNota()
    {
        if (RemesaSeleccionada is not { } remesa)
            return;

        var editor = new NotaEditorViewModel(remesa);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            _servicio.AgregarNota(remesa, editor.Texto, _sesion.UsuarioActual?.NombreCompleto ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Seguimiento", ex.Message);
            return;
        }

        ReconstruirTimeline();
    }

    /// <summary>Abre la ficha del evento con los datos del documento que lo originó.</summary>
    private void VerDetalleEvento(EventoTimelineItem? item)
    {
        if (item is null || RemesaSeleccionada is not { } remesa)
            return;

        var datos = _servicio.ObtenerDetalle(item.Evento, remesa);
        _dialogos.MostrarEditor(new DetalleEventoViewModel(item.Evento, datos));
    }

    private void ReconstruirTimeline()
    {
        Eventos.Clear();

        if (RemesaSeleccionada is not { } remesa)
            return;

        var eventos = _servicio.ObtenerTimeline(remesa);
        for (var i = 0; i < eventos.Count; i++)
            Eventos.Add(new EventoTimelineItem(eventos[i], i == 0, i == eventos.Count - 1));
    }
}

/// <summary>
/// Fila de la línea de tiempo: el evento más las marcas de extremo, que la vista usa para
/// recortar el riel vertical arriba del primer nodo y debajo del último.
/// </summary>
public sealed class EventoTimelineItem
{
    public EventoTimelineItem(EventoOperacion evento, bool esPrimero, bool esUltimo)
    {
        Evento = evento;
        EsPrimero = esPrimero;
        EsUltimo = esUltimo;
    }

    public EventoOperacion Evento { get; }
    public bool EsPrimero { get; }
    public bool EsUltimo { get; }
    public bool TieneAutor => Evento.Autor.Length > 0;
}
