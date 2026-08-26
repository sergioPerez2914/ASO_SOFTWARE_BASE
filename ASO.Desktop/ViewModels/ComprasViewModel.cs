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
    private readonly IProveedorDataSource _proveedores;
    private readonly ICotizacionProveedorDataSource _cotizaciones;
    private readonly IMarcaLubricanteDataSource _marcasLubricante;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly ComprasService _servicio;

    private string _filtroEstado = FiltroTodas;

    public RequisicionesCrudViewModel(IRequisicionDataSource requisiciones,
                                      IInventoryDataSource articulos,
                                      IProveedorDataSource proveedores,
                                      ICotizacionProveedorDataSource cotizaciones,
                                      IMarcaLubricanteDataSource marcasLubricante,
                                      ComprasService servicio,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion)
        : base(requisiciones, dialogos, sesion)
    {
        _articulos = articulos;
        _proveedores = proveedores;
        _cotizaciones = cotizaciones;
        _marcasLubricante = marcasLubricante;
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
        new RequisicionEditorViewModel(item, _articulos);

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

        var editor = new CompararProveedoresEditorViewModel(
            requisicion, _proveedores, _cotizaciones, _marcasLubricante, _servicio, _dialogos, _sesionActual);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var orden = _servicio.CrearDesdeRequisicion(
                requisicion, editor.GanadoraSeleccionada!, [.. editor.LineasOrden], _sesionActual.UsuarioActual?.Id ?? 0);

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

        RegistrarRecepcionCommand = new RelayCommand(RegistrarRecepcion,
            () => SelectedItem is { } oc && _servicio.PuedeRegistrarRecepcion(oc) && _sesionActual.Puede(Permisos.RecepcionMercancia.Crear));
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand AprobarCommand { get; }
    public ICommand AnularCommand { get; }
    public ICommand RegistrarRecepcionCommand { get; }

    /// <summary>Se dispara al registrar una recepción, para que el contenedor cambie de pestaña.</summary>
    public event EventHandler<int>? RecepcionCreada;

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

    protected override bool PuedeEliminar(OrdenCompra item) => _servicio.PuedeEliminarOrdenCompra(item);

    protected override OrdenCompra CrearNuevo() =>
        throw new NotSupportedException(
            "La orden de compra se arma desde una requisición enviada, con \"Armar orden de compra\".");

    protected override CrudEditorViewModelBase<OrdenCompra> CrearEditor(OrdenCompra item) =>
        throw new NotSupportedException(
            "El detalle de la orden de compra se completa al armarla, en \"Comparar proveedores\" — aquí solo se autoriza.");

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

    private void RegistrarRecepcion()
    {
        if (SelectedItem is not { } orden)
            return;

        if (!_dialogos.Confirmar("Registrar recepción",
                $"¿Registrar la recepción de mercancía de la orden Nº {orden.Id} a {orden.ProveedorNombre}?"))
            return;

        try
        {
            var recepcion = _servicio.CrearRecepcionDesdeOrdenCompra(orden, _sesionActual.UsuarioActual?.Id ?? 0);
            SeleccionarTrasRecargar(orden.Id);
            RecepcionCreada?.Invoke(this, recepcion.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo registrar la recepción", ex.Message);
        }
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
/// Recepciones de mercancía de las órdenes de compra aprobadas: la que de verdad mueve
/// inventario real. Sub-listado de Inventario · Compras; el encabezado y el conmutador los pone
/// <see cref="ComprasViewModel"/>. Nace desde la fila de una orden de compra
/// (<see cref="OrdenesCompraCrudViewModel.RegistrarRecepcionCommand"/>), no con un botón
/// "Nueva…", igual que las órdenes de compra nacen desde una requisición.
/// </summary>
public sealed class RecepcionesCrudViewModel : CrudViewModelBase<RecepcionMercancia, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IStockCombustibleDataSource _stockCombustible;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly ComprasService _servicio;

    private string _filtroEstado = FiltroTodas;

    public RecepcionesCrudViewModel(IRecepcionMercanciaDataSource recepciones,
                                    IStockCombustibleDataSource stockCombustible,
                                    ComprasService servicio,
                                    IServicioDialogo dialogos,
                                    ISesionActual sesion)
        : base(recepciones, dialogos, sesion)
    {
        _stockCombustible = stockCombustible;
        _servicio = servicio;
        _dialogos = dialogos;
        _sesionActual = sesion;

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        ConfirmarCommand = new RelayCommand(Confirmar,
            () => SelectedItem is { } r && _servicio.PuedeConfirmarRecepcion(r) && _sesionActual.Puede(Permisos.RecepcionMercancia.Confirmar));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } r && _servicio.PuedeAnularRecepcion(r) && _sesionActual.Puede(Permisos.RecepcionMercancia.Anular));
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand ConfirmarCommand { get; }
    public ICommand AnularCommand { get; }

    protected override string ModuloPermiso => "RecepcionMercancia";

    protected override bool CoincideBusqueda(RecepcionMercancia item, string texto) =>
        item.ProveedorNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.RecibidoPor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(RecepcionMercancia item) => _filtroEstado switch
    {
        "Borrador" => item.Estado == EstadoRecepcionMercancia.Borrador,
        "Confirmada" => item.Estado == EstadoRecepcionMercancia.Confirmada,
        "Anulada" => item.Estado == EstadoRecepcionMercancia.Anulada,
        _ => true
    };

    protected override bool PuedeEditar(RecepcionMercancia item) => _servicio.PuedeEditarRecepcion(item);

    protected override bool PuedeEliminar(RecepcionMercancia item) => _servicio.PuedeEliminarRecepcion(item);

    protected override RecepcionMercancia CrearNuevo() =>
        throw new NotSupportedException(
            "La recepción se registra desde una orden de compra aprobada, con \"Registrar recepción\".");

    protected override CrudEditorViewModelBase<RecepcionMercancia> CrearEditor(RecepcionMercancia item) =>
        new RecepcionMercanciaEditorViewModel(item, _stockCombustible);

    private void Confirmar()
    {
        if (SelectedItem is not { } recepcion)
            return;

        Aplicar(() => _servicio.ConfirmarRecepcion(recepcion));
    }

    private void Anular()
    {
        if (SelectedItem is not { } recepcion)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular recepción Nº {recepcion.Id}",
            $"{recepcion.ProveedorNombre} — orden de compra Nº {recepcion.OrdenCompraId}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.AnularRecepcion(recepcion, editor.Motivo));
    }

    private void Aplicar(Func<RecepcionMercancia> transicion)
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
    public const string VistaRecepciones = "Recepciones";

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
        var proveedores = DataSourceFactory.CrearProveedores();
        var requisiciones = DataSourceFactory.CrearRequisiciones();
        var cotizaciones = DataSourceFactory.CrearCotizacionesProveedor();
        var ordenesCompra = DataSourceFactory.CrearOrdenesCompra();
        var recepciones = DataSourceFactory.CrearRecepcionesMercancia();
        var stockCombustible = DataSourceFactory.CrearStockCombustible();
        var lubricantes = DataSourceFactory.CrearLubricantes();
        var marcasLubricante = DataSourceFactory.CrearMarcasLubricante();

        var servicio = new ComprasService(
            requisiciones, cotizaciones, ordenesCompra, recepciones, articulos, stockCombustible, lubricantes);

        Requisiciones = new RequisicionesCrudViewModel(
            requisiciones, articulos, proveedores, cotizaciones, marcasLubricante, servicio, dialogos, sesion);
        Requisiciones.OrdenCompraCreada += (_, _) => VistaActual = VistaOrdenesCompra;

        OrdenesCompra = new OrdenesCompraCrudViewModel(ordenesCompra, servicio, dialogos, sesion);
        OrdenesCompra.RecepcionCreada += (_, _) => VistaActual = VistaRecepciones;

        Recepciones = new RecepcionesCrudViewModel(recepciones, stockCombustible, servicio, dialogos, sesion);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public RequisicionesCrudViewModel Requisiciones { get; }
    public OrdenesCompraCrudViewModel OrdenesCompra { get; }
    public RecepcionesCrudViewModel Recepciones { get; }

    /// <summary>
    /// Los tres listados, aunque solo se vea uno: armar una orden de compra o registrar una
    /// recepción desde otra pestaña tiene que dejarla disponible al conmutar, sin salir y volver
    /// a entrar.
    /// </summary>
    public override void Recargar()
    {
        Requisiciones.Recargar();
        OrdenesCompra.Recargar();
        Recepciones.Recargar();
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
    public bool MostrarRecepciones => VistaActual == VistaRecepciones;

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

    public bool EsRecepciones
    {
        get => MostrarRecepciones;
        set { if (value) VistaActual = VistaRecepciones; }
    }

    public ICommand CambiarVistaCommand { get; }
}
