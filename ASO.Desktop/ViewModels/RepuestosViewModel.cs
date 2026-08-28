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
/// Inventario · Repuestos: dos caras de lo mismo en una pantalla conmutable.
///
/// - Existencias: el catálogo de artículos (maestro, CRUD directo).
/// - Salidas: el documento de movimiento que descuenta esas existencias y le pone precio al
///   trabajo de taller.
///
/// Las reglas viven en <see cref="InventarioService"/>; aquí solo se pide la acción y se
/// refleja el resultado. La única excepción con criterio propio es el forzado de stock: cuando
/// el servicio rechaza por falta de existencia y el usuario tiene el permiso de excepción, se
/// le pregunta explícitamente antes de reintentar. No se fuerza en silencio.
/// </summary>
public sealed class RepuestosViewModel : PantallaCrudViewModel<InventoryItem, string>
{
    public const string VistaExistencias = "Existencias";
    public const string VistaSalidas = "Salidas";

    private const string FiltroTodos = "Todos";

    private readonly IInventoryDataSource _articulos;
    private readonly ISalidaInventarioDataSource _fuenteSalidas;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly InventarioService _servicio;

    private string _filtroEstadoStock = FiltroTodos;
    private string _filtroEstadoSalida = FiltroTodos;

    public RepuestosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearInventario(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private RepuestosViewModel(Modulo modulo,
                               Submodulo submodulo,
                               IInventoryDataSource articulos,
                               IServicioDialogo dialogos,
                               ISesionActual sesion)
        : base(modulo, submodulo, articulos, dialogos, sesion)
    {
        _articulos = articulos;
        _dialogos = dialogos;
        _sesionActual = sesion;
        _fuenteSalidas = DataSourceFactory.CrearSalidasInventario();

        _servicio = new InventarioService(_fuenteSalidas, articulos, DataSourceFactory.CrearMantenimientos(), sesion);

        Salidas = new ObservableCollection<SalidaInventario>(
            _fuenteSalidas.GetAll().OrderByDescending(s => s.Fecha));
        SalidasView = CollectionViewSource.GetDefaultView(Salidas);
        SalidasView.Filter = FiltrarSalida;

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);

        CambiarFiltroStockCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstadoStock = filtro;
            ItemsView.Refresh();
        });

        CambiarFiltroSalidaCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstadoSalida = filtro;
            SalidasView.Refresh();
        });

        RegistrarSalidaCommand = new RelayCommand(RegistrarSalida,
            () => _sesionActual.Puede("Inventario.RegistrarSalida"));

        EditarSalidaCommand = new RelayCommand(EditarSalida,
            () => SalidaSeleccionada is { } s && _servicio.PuedeEditar(s) && _sesionActual.Puede("Inventario.RegistrarSalida"));

        ConfirmarSalidaCommand = new RelayCommand(ConfirmarSalida,
            () => SalidaSeleccionada is { } s && _servicio.PuedeConfirmar(s) && _sesionActual.Puede("Inventario.ConfirmarSalida"));

        AnularSalidaCommand = new RelayCommand(AnularSalida,
            () => SalidaSeleccionada is { } s && _servicio.PuedeAnular(s) && _sesionActual.Puede("Inventario.AnularSalida"));

        EliminarSalidaCommand = new RelayCommand(EliminarSalida,
            () => SalidaSeleccionada is { } s && _servicio.PuedeEliminar(s) && _sesionActual.Puede("Inventario.EliminarSalida"));
    }

    // --- Encabezado de la pantalla ---

    public ICommand CambiarVistaCommand { get; }
    public ICommand CambiarFiltroStockCommand { get; }
    public ICommand CambiarFiltroSalidaCommand { get; }
    public ICommand RegistrarSalidaCommand { get; }
    public ICommand EditarSalidaCommand { get; }
    public ICommand ConfirmarSalidaCommand { get; }
    public ICommand AnularSalidaCommand { get; }
    public ICommand EliminarSalidaCommand { get; }

    public ObservableCollection<SalidaInventario> Salidas { get; }
    public ICollectionView SalidasView { get; }

    private SalidaInventario? _salidaSeleccionada;
    public SalidaInventario? SalidaSeleccionada
    {
        get => _salidaSeleccionada;
        set
        {
            if (SetProperty(ref _salidaSeleccionada, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _vistaActual = VistaExistencias;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            // Notifica todas: enumerar aqui un OnPropertyChanged por cada Mostrar… es
            // la lista que se queda corta el dia que se agrega un padron mas.
            if (SetProperty(ref _vistaActual, value))
                OnTodasLasPropiedadesCambiaron();
        }
    }

    public bool MostrarExistencias => VistaActual == VistaExistencias;
    public bool MostrarSalidas => VistaActual == VistaSalidas;

    /// <summary>
    /// Las dos pestañas, enlazadas en DOS VÍAS al <c>IsChecked</c> de su botón, como en
    /// <see cref="AdministracionViewModel"/>. Antes la selección viajaba solo de la vista al
    /// ViewModel por <c>Command</c>, con <c>IsChecked="True"</c> a fuego en la primera: si algo
    /// cambiaba <see cref="VistaActual"/> desde el código, los botones no se enteraban.
    ///
    /// El setter solo actúa al marcar: al desmarcar ya hay otro botón del grupo encendiéndose.
    /// </summary>
    public bool EsExistencias
    {
        get => MostrarExistencias;
        set { if (value) VistaActual = VistaExistencias; }
    }

    public bool EsSalidas
    {
        get => MostrarSalidas;
        set { if (value) VistaActual = VistaSalidas; }
    }

    /// <summary>Resumen del almacén, visible en la barra sin tener que ir al dashboard.</summary>
    public string ResumenExistencias
    {
        get
        {
            var articulos = _articulos.GetAll().ToList();
            var bajos = articulos.Count(a => a.Estado != StockStatus.Ok);
            var valor = articulos.Sum(a => a.ValorTotal);
            return $"{articulos.Count} artículos · {bajos} por reponer · valor {valor:N2}";
        }
    }

    // --- Puntos de extensión del CRUD de existencias ---

    protected override string ModuloPermiso => "Inventario";

    protected override bool CoincideBusqueda(InventoryItem item, string texto) =>
        item.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Categoria.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Ubicacion.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(InventoryItem item) => _filtroEstadoStock switch
    {
        "Disponible" => item.Estado == StockStatus.Ok,
        "Bajo" => item.Estado == StockStatus.Bajo,
        "Agotado" => item.Estado == StockStatus.Agotado,
        _ => true
    };

    protected override InventoryItem CrearNuevo() => new() { Unidad = "und" };

    protected override CrudEditorViewModelBase<InventoryItem> CrearEditor(InventoryItem item) =>
        new InventoryItemEditorViewModel(item, _articulos, esNuevo: string.IsNullOrWhiteSpace(item.Codigo));

    // --- Salidas de almacén ---

    private bool FiltrarSalida(object obj)
    {
        if (obj is not SalidaInventario salida)
            return false;

        var texto = TextoBusquedaSalidas.Trim();
        var coincide = string.IsNullOrWhiteSpace(texto)
            || salida.ArticuloNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || salida.ArticuloCodigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || salida.DestinoTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || salida.Motivo.Contains(texto, StringComparison.OrdinalIgnoreCase);

        var estado = _filtroEstadoSalida switch
        {
            "Borrador" => salida.Estado == EstadoSalida.Borrador,
            "Confirmada" => salida.Estado == EstadoSalida.Confirmada,
            "Anulada" => salida.Estado == EstadoSalida.Anulada,
            _ => true
        };

        return coincide && estado;
    }

    private string _textoBusquedaSalidas = string.Empty;
    public string TextoBusquedaSalidas
    {
        get => _textoBusquedaSalidas;
        set { if (SetProperty(ref _textoBusquedaSalidas, value)) SalidasView.Refresh(); }
    }

    private void RegistrarSalida()
    {
        var nueva = new SalidaInventario
        {
            Fecha = DateTime.Today,
            Estado = EstadoSalida.Borrador,
            CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0,
            FechaCreacion = DateTime.Now
        };

        var editor = CrearEditorSalida(nueva);
        if (!_dialogos.MostrarEditor(editor))
            return;

        var agregada = _fuenteSalidas.Add(editor.ObtenerResultado());
        _idSalidaASeleccionar = agregada.Id;
        VistaActual = VistaSalidas;
    }

    private void EditarSalida()
    {
        if (SalidaSeleccionada is not { } actual)
            return;

        var editor = CrearEditorSalida(actual);
        if (!_dialogos.MostrarEditor(editor))
            return;

        var actualizada = editor.ObtenerResultado();
        _fuenteSalidas.Update(actualizada);
        _idSalidaASeleccionar = actualizada.Id;
    }

    private SalidaInventarioEditorViewModel CrearEditorSalida(SalidaInventario salida) =>
        new(salida, _articulos, DataSourceFactory.CrearActivosFlota(), DataSourceFactory.CrearMantenimientos());

    private void ConfirmarSalida()
    {
        if (SalidaSeleccionada is not { } salida)
            return;

        try
        {
            _idSalidaASeleccionar = _servicio.Confirmar(salida).Id;
        }
        catch (InvalidOperationException ex)
        {
            // Falta de existencia: si quien opera tiene la excepción de Admin, se le ofrece
            // autorizarla de forma explícita. Cualquier otro rechazo solo se informa.
            var faltaStock = ex.Message.Contains("Existencia insuficiente", StringComparison.OrdinalIgnoreCase);

            if (faltaStock && _sesionActual.Puede("Inventario.OverrideStock")
                && _dialogos.Confirmar("Existencia insuficiente",
                    $"{ex.Message}\n\n¿Autorizar la salida de todas formas? Quedará registrada como excepción."))
            {
                try
                {
                    _idSalidaASeleccionar = _servicio.Confirmar(salida, forzarStock: true).Id;
                }
                catch (InvalidOperationException reintento)
                {
                    _dialogos.Informar("No se pudo confirmar", reintento.Message);
                }

                return;
            }

            _dialogos.Informar("No se pudo confirmar", ex.Message);
        }
    }

    private void AnularSalida()
    {
        if (SalidaSeleccionada is not { } salida)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular salida Nº {salida.Id}",
            $"{salida.ArticuloNombre} — {salida.CantidadTexto} — {salida.DestinoTexto}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            _idSalidaASeleccionar = _servicio.Anular(salida, editor.Motivo).Id;
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo anular", ex.Message);
        }
    }

    private void EliminarSalida()
    {
        if (SalidaSeleccionada is not { } salida)
            return;

        if (!_dialogos.Confirmar("Eliminar",
                "¿Eliminar la salida en borrador? Esta acción no se puede deshacer."))
            return;

        _fuenteSalidas.Delete(salida.Id);
        _idSalidaASeleccionar = null;
        SalidaSeleccionada = null;
    }

    /// <summary>Id de la salida que debe quedar seleccionada tras la próxima recarga.</summary>
    private int? _idSalidaASeleccionar;

    /// <summary>
    /// Relee las DOS tablas de la pantalla, y por eso la recarga automática importa aquí más que
    /// en ninguna otra: confirmar una salida descuenta el stock del artículo, así que la tabla de
    /// existencias cambia por una acción hecha sobre la de salidas.
    ///
    /// Como en el listado base, la fila se reencuentra por Id: la recarga trae objetos nuevos.
    /// </summary>
    public override void Recargar()
    {
        base.Recargar();

        var idBuscado = _idSalidaASeleccionar ?? SalidaSeleccionada?.Id;
        _idSalidaASeleccionar = null;

        Salidas.Clear();
        foreach (var salida in _fuenteSalidas.GetAll().OrderByDescending(s => s.Fecha))
            Salidas.Add(salida);

        SalidasView.Refresh();

        SalidaSeleccionada = idBuscado is { } id
            ? Salidas.FirstOrDefault(s => s.Id == id)
            : null;
    }
}
