using System;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Requisiciones pendientes de convertirse en una orden de compra. Sub-listado de
/// Inventario · Compras; el encabezado y el conmutador los pone <see cref="ComprasViewModel"/>.
/// </summary>
public sealed class RequisicionesCrudViewModel : CrudViewModelBase<Requisicion, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IInventoryDataSource _articulos;
    private readonly IActivoFlotaDataSource _activos;
    private readonly IProveedorDataSource _proveedores;
    private readonly ICotizacionProveedorDataSource _cotizaciones;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly ComprasService _servicio;

    private string _filtroEstado = FiltroTodas;

    public RequisicionesCrudViewModel(IRequisicionDataSource requisiciones,
                                      IInventoryDataSource articulos,
                                      IActivoFlotaDataSource activos,
                                      IProveedorDataSource proveedores,
                                      ICotizacionProveedorDataSource cotizaciones,
                                      ComprasService servicio,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion)
        : base(requisiciones, dialogos, sesion)
    {
        _articulos = articulos;
        _activos = activos;
        _proveedores = proveedores;
        _cotizaciones = cotizaciones;
        _servicio = servicio;
        _dialogos = dialogos;
        _sesionActual = sesion;

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        EnviarCommand = new RelayCommand(Enviar,
            () => SelectedItem is { } r && _servicio.PuedeEnviarRequisicion(r) && _sesionActual.Puede(Permisos.Requisicion.Enviar));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } r && _servicio.PuedeAnularRequisicion(r) && _sesionActual.Puede(Permisos.Requisicion.Anular));

        ArmarOrdenCompraCommand = new RelayCommand(ArmarOrdenCompra,
            () => SelectedItem is { } r && _servicio.PuedeArmarOrdenCompra(r) && _sesionActual.Puede(Permisos.OrdenCompra.Crear));
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand EnviarCommand { get; }
    public ICommand AnularCommand { get; }
    public ICommand ArmarOrdenCompraCommand { get; }

    /// <summary>Se dispara al armar una orden de compra, para que el contenedor cambie de pestaña.</summary>
    public event EventHandler<int>? OrdenCompraCreada;

    protected override string ModuloPermiso => "Requisicion";

    protected override bool CoincideBusqueda(Requisicion item, string texto) =>
        item.Lineas.Any(l => l.DestinoTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                              || l.UnidadDestinoTexto.Contains(texto, StringComparison.OrdinalIgnoreCase));

    protected override bool PasaFiltroExtra(Requisicion item) => _filtroEstado switch
    {
        "Borrador" => item.Estado == EstadoRequisicion.Borrador,
        "Enviada" => item.Estado == EstadoRequisicion.Enviada,
        "Atendida" => item.Estado == EstadoRequisicion.Atendida,
        "Anulada" => item.Estado == EstadoRequisicion.Anulada,
        _ => true
    };

    protected override bool PuedeEditar(Requisicion item) => _servicio.PuedeEditarRequisicion(item);

    protected override bool PuedeEliminar(Requisicion item) => _servicio.PuedeEliminarRequisicion(item);

    protected override Requisicion CrearNuevo() => new()
    {
        Fecha = DateTime.Today,
        Estado = EstadoRequisicion.Borrador,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0,
        FechaCreacion = DateTime.Now
    };

    protected override CrudEditorViewModelBase<Requisicion> CrearEditor(Requisicion item) =>
        new RequisicionEditorViewModel(item, _articulos, _activos);

    private void Enviar()
    {
        if (SelectedItem is not { } requisicion)
            return;

        Aplicar(() => _servicio.EnviarRequisicion(requisicion));
    }

    private void Anular()
    {
        if (SelectedItem is not { } requisicion)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular requisición Nº {requisicion.Id}",
            requisicion.LineasTexto,
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.AnularRequisicion(requisicion, editor.Motivo));
    }

    private void ArmarOrdenCompra()
    {
        if (SelectedItem is not { } requisicion)
            return;

        var editor = new CompararProveedoresEditorViewModel(requisicion, _proveedores, _cotizaciones);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var orden = _servicio.CrearDesdeRequisicion(
                requisicion, editor.GanadoraSeleccionada!, _sesionActual.UsuarioActual?.Id ?? 0);

            SeleccionarTrasRecargar(requisicion.Id);
            OrdenCompraCreada?.Invoke(this, orden.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo armar la orden de compra", ex.Message);
        }
    }

    private void Aplicar(Func<Requisicion> transicion)
    {
        try
        {
            SeleccionarTrasRecargar(transicion().Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
    }
}

/// <summary>
/// Órdenes de compra armadas a partir de una requisición. Sub-listado de Inventario · Compras;
/// el encabezado y el conmutador los pone <see cref="ComprasViewModel"/>.
/// </summary>
public sealed class OrdenesCompraCrudViewModel : CrudViewModelBase<OrdenCompra, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly ComprasService _servicio;

    private string _filtroEstado = FiltroTodas;

    public OrdenesCompraCrudViewModel(IOrdenCompraDataSource ordenesCompra,
                                      ComprasService servicio,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion)
        : base(ordenesCompra, dialogos, sesion)
    {
        _servicio = servicio;
        _dialogos = dialogos;
        _sesionActual = sesion;

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        AprobarCommand = new RelayCommand(Aprobar,
            () => SelectedItem is { } oc && _servicio.PuedeAprobarOrdenCompra(oc) && _sesionActual.Puede(Permisos.OrdenCompra.Aprobar));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } oc && _servicio.PuedeAnularOrdenCompra(oc) && _sesionActual.Puede(Permisos.OrdenCompra.Anular));
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand AprobarCommand { get; }
    public ICommand AnularCommand { get; }

    protected override string ModuloPermiso => "OrdenCompra";

    protected override bool CoincideBusqueda(OrdenCompra item, string texto) =>
        item.ProveedorNombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(OrdenCompra item) => _filtroEstado switch
    {
        "Borrador" => item.Estado == EstadoOrdenCompra.Borrador,
        "Aprobada" => item.Estado == EstadoOrdenCompra.Aprobada,
        "Cerrada" => item.Estado == EstadoOrdenCompra.Cerrada,
        "Anulada" => item.Estado == EstadoOrdenCompra.Anulada,
        _ => true
    };

    protected override bool PuedeEditar(OrdenCompra item) => _servicio.PuedeEditarOrdenCompra(item);

    protected override bool PuedeEliminar(OrdenCompra item) => _servicio.PuedeEliminarOrdenCompra(item);

    protected override OrdenCompra CrearNuevo() =>
        throw new NotSupportedException(
            "La orden de compra se arma desde una requisición enviada, con \"Armar orden de compra\".");

    protected override CrudEditorViewModelBase<OrdenCompra> CrearEditor(OrdenCompra item) =>
        new OrdenCompraEditorViewModel(item);

    private void Aprobar()
    {
        if (SelectedItem is not { } orden)
            return;

        if (!_dialogos.Confirmar("Aprobar orden de compra",
                $"¿Aprobar la orden a {orden.ProveedorNombre} por {orden.MontoTotalTexto}?"))
            return;

        Aplicar(() => _servicio.Aprobar(orden, _sesionActual.UsuarioActual?.Id ?? 0));
    }

    private void Anular()
    {
        if (SelectedItem is not { } orden)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular orden de compra Nº {orden.Id}",
            $"{orden.ProveedorNombre} — {orden.MontoTotalTexto}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.AnularOrdenCompra(orden, editor.Motivo));
    }

    private void Aplicar(Func<OrdenCompra> transicion)
    {
        try
        {
            SeleccionarTrasRecargar(transicion().Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
    }
}

/// <summary>
/// Inventario · Compras: requisiciones y órdenes de compra en una pantalla conmutable, con el
/// mismo patrón que Finanzas · Cuentas por Pagar.
/// </summary>
public sealed class ComprasViewModel : PantallaViewModelBase
{
    public const string VistaRequisiciones = "Requisiciones";
    public const string VistaOrdenesCompra = "OrdenesCompra";

    public ComprasViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private ComprasViewModel(Modulo modulo,
                             Submodulo submodulo,
                             IServicioDialogo dialogos,
                             ISesionActual sesion)
        : base(modulo, submodulo)
    {
        var articulos = DataSourceFactory.CrearInventario();
        var activos = DataSourceFactory.CrearActivosFlota();
        var proveedores = DataSourceFactory.CrearProveedores();
        var requisiciones = DataSourceFactory.CrearRequisiciones();
        var cotizaciones = DataSourceFactory.CrearCotizacionesProveedor();
        var ordenesCompra = DataSourceFactory.CrearOrdenesCompra();

        var servicio = new ComprasService(requisiciones, cotizaciones, ordenesCompra);

        Requisiciones = new RequisicionesCrudViewModel(
            requisiciones, articulos, activos, proveedores, cotizaciones, servicio, dialogos, sesion);
        Requisiciones.OrdenCompraCreada += (_, _) => VistaActual = VistaOrdenesCompra;

        OrdenesCompra = new OrdenesCompraCrudViewModel(ordenesCompra, servicio, dialogos, sesion);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public RequisicionesCrudViewModel Requisiciones { get; }
    public OrdenesCompraCrudViewModel OrdenesCompra { get; }

    /// <summary>
    /// Los dos listados, aunque solo se vea uno: armar una orden de compra desde la vista de
    /// requisiciones tiene que dejarla disponible al conmutar, sin salir y volver a entrar.
    /// </summary>
    public override void Recargar()
    {
        Requisiciones.Recargar();
        OrdenesCompra.Recargar();
    }

    private string _vistaActual = VistaRequisiciones;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            if (SetProperty(ref _vistaActual, value))
                OnTodasLasPropiedadesCambiaron();
        }
    }

    public bool MostrarRequisiciones => VistaActual == VistaRequisiciones;
    public bool MostrarOrdenesCompra => VistaActual == VistaOrdenesCompra;

    public bool EsRequisiciones
    {
        get => MostrarRequisiciones;
        set { if (value) VistaActual = VistaRequisiciones; }
    }

    public bool EsOrdenesCompra
    {
        get => MostrarOrdenesCompra;
        set { if (value) VistaActual = VistaOrdenesCompra; }
    }

    public ICommand CambiarVistaCommand { get; }
}
