using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Una remesa cerrada que todavía no está en ninguna factura: plata por cobrar esperando.
///
/// Trae al lado lo que el central reconoció pagar en su boleto y lo que sale del tarifario, que
/// es la comparación que se vio al cerrarla. Aquí sirve para otra cosa: ver de un vistazo si el
/// lote que se va a facturar arrastra alguna diferencia sin reclamar.
/// </summary>
public sealed class RemesaPorFacturar
{
    private readonly decimal? _cobroEstimado;

    public RemesaPorFacturar(Remesa remesa, decimal? cobroEstimado)
    {
        Remesa = remesa;
        _cobroEstimado = cobroEstimado;
    }

    public Remesa Remesa { get; }

    public int Id => Remesa.Id;
    public string FincaNombre => Remesa.FincaNombre;
    public string FechaTexto => Remesa.LlegadaCentral?.ToString("dd/MM/yyyy") ?? "—";
    public string BoletoTexto => Remesa.Boleto?.NumeroTexto ?? "—";
    public string ToneladasTexto => (Remesa.PesoNetoT ?? 0m).ToString("N2", CultureInfo.CurrentCulture);

    public string CobroEstimadoTexto => Texto(_cobroEstimado);
    public string DeclaradoTexto => Texto(Remesa.Boleto?.DescuentosDeServicio);

    private decimal? Diferencia =>
        _cobroEstimado is { } estimado && Remesa.Boleto is { } boleto
            ? boleto.DescuentosDeServicio - estimado
            : null;

    public string DiferenciaTexto => Diferencia is { } d
        ? d.ToString("+#,##0.00;-#,##0.00;0,00", CultureInfo.CurrentCulture)
        : "—";

    /// <summary>Sin comparación posible se da por cuadrada: no hay nada que reclamar todavía.</summary>
    public bool Cuadra => Diferencia is not { } d || Math.Abs(d) < 0.01m;

    private static string Texto(decimal? valor) =>
        valor is { } v ? v.ToString("N2", CultureInfo.CurrentCulture) : "—";
}

/// <summary>
/// Finanzas · Cuentas por Cobrar: la facturación al ingenio por la caña entregada.
///
/// Como en Liquidaciones, la factura no se captura a mano: se genera desde las remesas
/// recibidas y el tarifario. Por eso el alta y la edición heredados del CRUD quedan
/// deshabilitados y en su lugar están Generar, Emitir, Registrar cobro y Anular.
///
/// La pantalla enseña DOS padrones con una pestaña: las facturas y las remesas cerradas que
/// todavía esperan factura. El segundo existe porque esas remesas son dinero por cobrar y hasta
/// ahora solo se veían dentro del diálogo de generar y como un número suelto en el resumen.
/// </summary>
public sealed class CuentasPorCobrarViewModel : PantallaCrudViewModel<FacturaCliente, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly FacturaClienteService _servicio;
    private readonly TarifaService _tarifas;
    private readonly BancoService _banco;

    private string _filtroEstado = FiltroTodas;

    public CuentasPorCobrarViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearFacturasCliente(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private CuentasPorCobrarViewModel(Modulo modulo,
                                      Submodulo submodulo,
                                      IFacturaClienteDataSource facturas,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion)
        : base(modulo, submodulo, facturas, dialogos, sesion)
    {
        _dialogos = dialogos;
        _sesionActual = sesion;
        _tarifas = new TarifaService(DataSourceFactory.CrearTarifas(), sesion);
        _banco = new BancoService(DataSourceFactory.CrearMovimientosBanco(),
                                  DataSourceFactory.CrearCuentasBancarias(), sesion);
        _servicio = new FacturaClienteService(facturas, DataSourceFactory.CrearRemesas(), _tarifas,
                                              DataSourceFactory.CrearEventosOperacion(), _banco, sesion);

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

        // El padrón de pendientes se puebla aquí y no solo en Recargar: la lista heredada la llena
        // el constructor de la base, así que sin esta llamada la pestaña saldría vacía al entrar y
        // no se llenaría hasta el primer cambio de datos.
        RefrescarPendientes();
    }

    // --- Encabezado de la pantalla ---

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand GenerarCommand { get; }
    public ICommand EmitirCommand { get; }
    public ICommand RegistrarCobroCommand { get; }
    public ICommand AnularCommand { get; }

    public bool HayDetalle => SelectedItem is not null && MostrarFacturas;

    // --- Los dos padrones ---

    public const string VistaFacturas = "Facturas";
    public const string VistaPendientes = "Pendientes";

    /// <summary>Las remesas cerradas que esperan factura, con su comparación contra el tarifario.</summary>
    public ObservableCollection<RemesaPorFacturar> RemesasPorFacturar { get; } = [];

    private string _vistaActual = VistaFacturas;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            // Notifica todas, igual que Cuentas por Pagar: enumerar aquí un OnPropertyChanged por
            // cada Mostrar… es la lista que se queda corta el día que se agregue otro padrón.
            if (SetProperty(ref _vistaActual, value))
                OnTodasLasPropiedadesCambiaron();
        }
    }

    public bool MostrarFacturas => VistaActual == VistaFacturas;
    public bool MostrarPendientes => VistaActual == VistaPendientes;

    /// <summary>El setter solo actúa al marcar: al desmarcar ya hay otro botón del grupo encendiéndose.</summary>
    public bool EsFacturas
    {
        get => MostrarFacturas;
        set { if (value) VistaActual = VistaFacturas; }
    }

    public bool EsPendientes
    {
        get => MostrarPendientes;
        set { if (value) VistaActual = VistaPendientes; }
    }

    public string PendientesTexto => $"Remesas por facturar ({RemesasPorFacturar.Count})";

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

    /// <summary>
    /// El padrón de pendientes se repuebla con cada recarga, no solo al entrar: cerrar una remesa
    /// en Operaciones dispara la recarga de esta pantalla, y sin esto la lista se quedaría vieja.
    /// </summary>
    public override void Recargar()
    {
        base.Recargar();
        RefrescarPendientes();
    }

    private void RefrescarPendientes()
    {
        RemesasPorFacturar.Clear();

        foreach (var remesa in _servicio.RemesasFacturables())
            RemesasPorFacturar.Add(new RemesaPorFacturar(remesa, CobroEstimado(remesa)));

        OnPropertyChanged(nameof(PendientesTexto));
    }

    /// <summary>
    /// Lo que rendiría esta remesa según el tarifario. Devuelve null si no hay tarifa vigente:
    /// sin tarifario cargado la columna se queda en blanco, que es más honesto que un cero, y la
    /// remesa se sigue viendo — el tarifario incompleto es problema de Finanzas, no motivo para
    /// esconder la caña ya entregada.
    /// </summary>
    private decimal? CobroEstimado(Remesa remesa)
    {
        var toneladas = remesa.PesoNetoT ?? 0m;
        if (toneladas <= 0)
            return null;

        var fecha = remesa.LlegadaCentral ?? remesa.FechaConfirmacion ?? DateTime.Today;

        try
        {
            return _tarifas.CalcularCobroPorServicio(remesa, toneladas, fecha).Sum(c => c.Monto);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    protected override string ModuloPermiso => "FacturasCliente";

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
            SeleccionarTrasRecargar(generada.Id);
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

        Aplicar(() => _servicio.Emitir(factura));
    }

    /// <summary>
    /// Pregunta a qué cuenta entró el dinero y lo anota en el libro de banco, además de dar la
    /// factura por cobrada.
    ///
    /// Antes esto era un Confirmar de sí/no. El paso extra no es burocracia: sin cuenta, fecha
    /// valor y referencia, el cobro quedaba como un cambio de estado y no había forma de saber
    /// después de qué bolsillo entró — que es justo lo que Finanzas · Banco viene a resolver.
    /// </summary>
    private void RegistrarCobro()
    {
        if (SelectedItem is not { } factura)
            return;

        var editor = new AsientoBancoEditorViewModel(
            $"Registrar cobro de {factura.NumeroTexto}",
            $"{factura.ClienteNombre} — {factura.RemesasTexto}",
            factura.Total,
            esEntrada: true,
            _banco.CuentasActivas(),
            "Registrar cobro");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.RegistrarCobro(factura, editor.Resultado,
                                               _sesionActual.UsuarioActual?.Id ?? 0));
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

        Aplicar(() => _servicio.Anular(factura, editor.Motivo));
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
    /// Ejecuta una transición. Si el servicio la rechaza se informa en vez de tragarse el error:
    /// el botón habilitado es cortesía, la regla la impone el servicio.
    ///
    /// La lista la repuebla la recarga que dispara la propia escritura del servicio; aquí solo
    /// se apunta qué factura dejar seleccionada.
    /// </summary>
    private void Aplicar(Func<FacturaCliente> transicion)
    {
        try
        {
            SeleccionarTrasRecargar(transicion().Id);
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
