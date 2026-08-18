using System;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Finanzas · Cuentas por Cobrar: la facturación al ingenio por la caña entregada.
///
/// Como en Liquidaciones, la factura no se captura a mano: se genera desde las remesas
/// recibidas y el tarifario. Por eso el alta y la edición heredados del CRUD quedan
/// deshabilitados y en su lugar están Generar, Emitir, Registrar cobro y Anular.
/// </summary>
public sealed class CuentasPorCobrarViewModel : CrudViewModelBase<FacturaCliente, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly FacturaClienteService _servicio;
    private readonly TarifaService _tarifas;

    private string _filtroEstado = FiltroTodas;

    public event EventHandler? VolverSolicitado;

    public CuentasPorCobrarViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearFacturasCliente(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private CuentasPorCobrarViewModel(Modulo modulo,
                                      Submodulo submodulo,
                                      IFacturaClienteDataSource facturas,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion)
        : base(facturas, dialogos, sesion)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        _dialogos = dialogos;
        _sesionActual = sesion;
        _tarifas = new TarifaService(DataSourceFactory.CrearTarifas());
        _servicio = new FacturaClienteService(facturas, DataSourceFactory.CrearRemesas(), _tarifas);

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        GenerarCommand = new RelayCommand(Generar,
            () => _sesionActual.Puede("Finanzas.Facturar"));

        EmitirCommand = new RelayCommand(Emitir,
            () => SelectedItem is { } f && _servicio.PuedeEmitir(f) && _sesionActual.Puede("Finanzas.Facturar"));

        RegistrarCobroCommand = new RelayCommand(RegistrarCobro,
            () => SelectedItem is { } f && _servicio.PuedeRegistrarCobro(f) && _sesionActual.Puede("Finanzas.RegistrarCobro"));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } f && _servicio.PuedeAnular(f) && _sesionActual.Puede("Finanzas.Anular"));

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(SelectedItem))
                return;

            OnPropertyChanged(nameof(HayDetalle));
            OnPropertyChanged(nameof(TotalesTexto));
        };
    }

    // --- Encabezado de la pantalla ---
    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }
    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }
    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand GenerarCommand { get; }
    public ICommand EmitirCommand { get; }
    public ICommand RegistrarCobroCommand { get; }
    public ICommand AnularCommand { get; }

    public bool HayDetalle => SelectedItem is not null;

    public string TotalesTexto => SelectedItem is { } f
        ? $"{f.RemesasTexto} · {f.Toneladas:N2} t · total {f.TotalTexto}"
        : string.Empty;

    /// <summary>Estado de la cartera, visible sin ir al dashboard del módulo.</summary>
    public string ResumenCartera
    {
        get
        {
            var facturables = _servicio.RemesasFacturables().Count;
            return $"Por cobrar {_servicio.TotalPorCobrar():N2} · vencido {_servicio.TotalVencido():N2} · " +
                   $"{facturables} remesa(s) sin facturar";
        }
    }

    // --- Puntos de extensión del CRUD ---

    protected override string ModuloPermiso => "Finanzas";

    protected override bool CoincideBusqueda(FacturaCliente item, string texto) =>
        item.NumeroTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.ClienteNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Lineas.Any(l => l.FincaNombre.Contains(texto, StringComparison.OrdinalIgnoreCase));

    protected override bool PasaFiltroExtra(FacturaCliente item) => _filtroEstado switch
    {
        "Borrador" => item.Estado == EstadoFacturaCliente.Borrador,
        "Emitida" => item.Estado == EstadoFacturaCliente.Emitida,
        "Vencida" => item.EstaVencida,
        "Cobrada" => item.Estado == EstadoFacturaCliente.Cobrada,
        "Anulada" => item.Estado == EstadoFacturaCliente.Anulada,
        _ => true
    };

    /// <summary>Una factura no se edita: un borrador mal generado se elimina y se rehace.</summary>
    protected override bool PuedeEditar(FacturaCliente item) => false;

    protected override bool PuedeEliminar(FacturaCliente item) => _servicio.PuedeEliminar(item);

    protected override FacturaCliente CrearNuevo() =>
        throw new NotSupportedException("Las facturas se generan con GenerarCommand, que pasa por FacturaClienteService.");

    protected override CrudEditorViewModelBase<FacturaCliente> CrearEditor(FacturaCliente item) =>
        throw new NotSupportedException("La factura no se edita en un formulario: se genera desde las remesas.");

    // --- Acciones ---

    private void Generar()
    {
        var facturables = _servicio.RemesasFacturables();

        if (facturables.Count == 0)
        {
            _dialogos.Informar("Sin remesas por facturar",
                "Todas las remesas recibidas ya están facturadas. " +
                "Registre la recepción de nuevas remesas en Operaciones para poder facturar.");
            return;
        }

        var editor = new GenerarFacturaEditorViewModel(facturables, TarifaTotalPorTonelada());
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var generada = _servicio.GenerarBorrador(editor.Seleccionadas, _sesionActual.UsuarioActual?.Id ?? 0);
            Items.Add(generada);
            SelectedItem = generada;
            ItemsView.Refresh();
            OnPropertyChanged(nameof(ResumenCartera));
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo generar la factura", ex.Message);
        }
    }

    private void Emitir()
    {
        if (SelectedItem is not { } factura)
            return;

        if (!_dialogos.Confirmar("Emitir factura",
                $"¿Emitir la factura {factura.NumeroTexto} por {factura.TotalTexto}? " +
                "Sus remesas quedarán marcadas como facturadas."))
            return;

        Aplicar(factura, () => _servicio.Emitir(factura));
    }

    private void RegistrarCobro()
    {
        if (SelectedItem is not { } factura)
            return;

        if (!_dialogos.Confirmar("Registrar cobro",
                $"¿Registrar el cobro de {factura.TotalTexto} de la factura {factura.NumeroTexto}?"))
            return;

        Aplicar(factura, () => _servicio.RegistrarCobro(factura));
    }

    private void Anular()
    {
        if (SelectedItem is not { } factura)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular factura {factura.NumeroTexto}",
            $"{factura.ClienteNombre} — {factura.RemesasTexto} — total {factura.TotalTexto}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(factura, () => _servicio.Anular(factura, editor.Motivo));
    }

    /// <summary>Lo que rinde una tonelada sumando los tres servicios; solo para el estimado del editor.</summary>
    private decimal TarifaTotalPorTonelada()
    {
        var hoy = DateTime.Today;
        return new[] { ServicioZafra.Corte, ServicioZafra.AlzaEmpuje, ServicioZafra.Transporte }
            .Select(s => _tarifas.ObtenerVigente(s, AmbitoTarifa.Cobro, hoy, UnidadTarifa.Tonelada))
            .Sum(t => t?.MontoPorUnidad ?? 0m);
    }

    /// <summary>
    /// Ejecuta una transición y refleja el resultado. Si el servicio la rechaza se informa en vez
    /// de tragarse el error: el botón habilitado es cortesía, la regla la impone el servicio.
    /// </summary>
    private void Aplicar(FacturaCliente original, Func<FacturaCliente> transicion)
    {
        try
        {
            var actualizada = transicion();

            var indice = Items.IndexOf(original);
            if (indice >= 0)
                Items[indice] = actualizada;

            SelectedItem = actualizada;
            ItemsView.Refresh();
            OnPropertyChanged(nameof(TotalesTexto));
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
        finally
        {
            OnPropertyChanged(nameof(ResumenCartera));
        }
    }
}
