using System;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Facturas de compra pendientes de pago. Sub-listado de Finanzas · Cuentas por Pagar; el
/// encabezado y el conmutador los pone <see cref="CuentasPorPagarViewModel"/>.
/// </summary>
public sealed class FacturasProveedorCrudViewModel : CrudViewModelBase<FacturaProveedor, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IProveedorDataSource _proveedores;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly CuentasPorPagarService _servicio;

    private string _filtroEstado = FiltroTodas;

    public FacturasProveedorCrudViewModel(IFacturaProveedorDataSource facturas,
                                          IProveedorDataSource proveedores,
                                          IServicioDialogo dialogos,
                                          ISesionActual sesion)
        : base(facturas, dialogos, sesion)
    {
        _proveedores = proveedores;
        _dialogos = dialogos;
        _sesionActual = sesion;
        _servicio = new CuentasPorPagarService(facturas);

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        RegistrarPagoCommand = new RelayCommand(RegistrarPago,
            () => SelectedItem is { } f && _servicio.PuedeRegistrarPago(f) && _sesionActual.Puede("Finanzas.Pagar"));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } f && _servicio.PuedeAnular(f) && _sesionActual.Puede("Finanzas.Anular"));
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand RegistrarPagoCommand { get; }
    public ICommand AnularCommand { get; }

    /// <summary>Estado de la deuda, visible sin ir al dashboard del módulo.</summary>
    public string ResumenDeuda =>
        $"Por pagar {_servicio.TotalPorPagar():N2} · vencido {_servicio.TotalVencido():N2}";

    protected override string ModuloPermiso => "Finanzas";

    protected override bool CoincideBusqueda(FacturaProveedor item, string texto) =>
        item.ProveedorNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.NumeroDocumento.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(FacturaProveedor item) => _filtroEstado switch
    {
        "Pendientes" => item.Estado == EstadoFacturaProveedor.Pendiente,
        "Vencidas" => item.EstaVencida,
        "Pagadas" => item.Estado == EstadoFacturaProveedor.Pagada,
        "Anuladas" => item.Estado == EstadoFacturaProveedor.Anulada,
        _ => true
    };

    protected override bool PuedeEditar(FacturaProveedor item) => _servicio.PuedeEditar(item);

    protected override bool PuedeEliminar(FacturaProveedor item) => _servicio.PuedeEliminar(item);

    protected override FacturaProveedor CrearNuevo() => new()
    {
        FechaEmision = DateTime.Today,
        FechaVencimiento = DateTime.Today.AddDays(30),
        Estado = EstadoFacturaProveedor.Pendiente,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0,
        FechaCreacion = DateTime.Now
    };

    protected override CrudEditorViewModelBase<FacturaProveedor> CrearEditor(FacturaProveedor item) =>
        new FacturaProveedorEditorViewModel(item, _proveedores, _servicio);

    private void RegistrarPago()
    {
        if (SelectedItem is not { } factura)
            return;

        if (!_dialogos.Confirmar("Registrar pago",
                $"¿Registrar el pago de {factura.MontoTexto} a {factura.ProveedorNombre}?"))
            return;

        Aplicar(factura, () => _servicio.RegistrarPago(factura));
    }

    private void Anular()
    {
        if (SelectedItem is not { } factura)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular factura {factura.NumeroDocumento}",
            $"{factura.ProveedorNombre} — {factura.Descripcion} — {factura.MontoTexto}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(factura, () => _servicio.Anular(factura, editor.Motivo));
    }

    private void Aplicar(FacturaProveedor original, Func<FacturaProveedor> transicion)
    {
        try
        {
            var actualizada = transicion();

            var indice = Items.IndexOf(original);
            if (indice >= 0)
                Items[indice] = actualizada;

            SelectedItem = actualizada;
            ItemsView.Refresh();
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
        finally
        {
            OnPropertyChanged(nameof(ResumenDeuda));
        }
    }
}

/// <summary>Maestro de proveedores. Sub-listado de Finanzas · Cuentas por Pagar.</summary>
public sealed class ProveedoresCrudViewModel : CrudViewModelBase<Proveedor, int>
{
    private readonly IProveedorDataSource _proveedores;

    public ProveedoresCrudViewModel(IProveedorDataSource proveedores,
                                    IServicioDialogo dialogos,
                                    ISesionActual sesion)
        : base(proveedores, dialogos, sesion)
    {
        _proveedores = proveedores;
    }

    protected override string ModuloPermiso => "Finanzas";

    protected override bool CoincideBusqueda(Proveedor item, string texto) =>
        item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Rif.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Telefono.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Proveedor CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<Proveedor> CrearEditor(Proveedor item) =>
        new ProveedorEditorViewModel(item, _proveedores);
}

/// <summary>
/// Finanzas · Cuentas por Pagar: las facturas de compra y el maestro de proveedores en una
/// pantalla conmutable, con el mismo patrón que Nómina · Empleados.
/// </summary>
public sealed class CuentasPorPagarViewModel : ViewModelBase
{
    public const string VistaFacturas = "Facturas";
    public const string VistaProveedores = "Proveedores";

    public event EventHandler? VolverSolicitado;

    public CuentasPorPagarViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private CuentasPorPagarViewModel(Modulo modulo,
                                     Submodulo submodulo,
                                     IServicioDialogo dialogos,
                                     ISesionActual sesion)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        var proveedores = DataSourceFactory.CrearProveedores();

        Facturas = new FacturasProveedorCrudViewModel(
            DataSourceFactory.CrearFacturasProveedor(), proveedores, dialogos, sesion);

        Proveedores = new ProveedoresCrudViewModel(proveedores, dialogos, sesion);

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }
    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public FacturasProveedorCrudViewModel Facturas { get; }
    public ProveedoresCrudViewModel Proveedores { get; }

    private string _vistaActual = VistaFacturas;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            if (SetProperty(ref _vistaActual, value))
            {
                OnPropertyChanged(nameof(MostrarFacturas));
                OnPropertyChanged(nameof(MostrarProveedores));
            }
        }
    }

    public bool MostrarFacturas => VistaActual == VistaFacturas;
    public bool MostrarProveedores => VistaActual == VistaProveedores;

    public ICommand VolverCommand { get; }
    public ICommand CambiarVistaCommand { get; }
}
