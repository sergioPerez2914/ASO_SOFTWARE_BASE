using System;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Nómina · Liquidaciones: el cálculo de lo que se le debe a cada núcleo y a cada empleado.
///
/// A diferencia de un maestro, aquí no se da de alta a mano: la liquidación se genera desde lo
/// ya registrado (remesas, jornadas, tarifario) y solo admite ajustes por conceptos. Por eso el
/// alta y la edición heredados del CRUD quedan deshabilitados y en su lugar están Generar,
/// Agregar concepto, Cerrar, Pagar y Anular.
/// </summary>
public sealed class LiquidacionesViewModel : PantallaCrudViewModel<Liquidacion, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly LiquidacionService _servicio;

    private string _filtroEstado = FiltroTodas;

    public LiquidacionesViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearLiquidaciones(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private LiquidacionesViewModel(Modulo modulo,
                                   Submodulo submodulo,
                                   ILiquidacionDataSource liquidaciones,
                                   IServicioDialogo dialogos,
                                   ISesionActual sesion)
        : base(modulo, submodulo, liquidaciones, dialogos, sesion)
    {
        _dialogos = dialogos;
        _sesionActual = sesion;

        _servicio = new LiquidacionService(
            liquidaciones,
            DataSourceFactory.CrearRemesas(),
            new TarifaService(DataSourceFactory.CrearTarifas()),
            new HorarioService(DataSourceFactory.CrearJornadas()));

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        GenerarCommand = new RelayCommand(Generar,
            () => _sesionActual.Puede("Nomina.Generar"));

        AgregarConceptoCommand = new RelayCommand(AgregarConcepto,
            () => SelectedItem is { } l && _servicio.PuedeEditarLineas(l) && _sesionActual.Puede("Nomina.EditarLineas"));

        QuitarLineaCommand = new RelayCommand(QuitarLinea,
            () => SelectedItem is { } l && LineaSeleccionada is not null
                  && _servicio.PuedeEditarLineas(l) && _sesionActual.Puede("Nomina.EditarLineas"));

        CerrarCommand = new RelayCommand(Cerrar,
            () => SelectedItem is { } l && _servicio.PuedeCerrar(l) && _sesionActual.Puede("Nomina.Cerrar"));

        PagarCommand = new RelayCommand(Pagar,
            () => SelectedItem is { } l && _servicio.PuedePagar(l) && _sesionActual.Puede("Nomina.Pagar"));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } l && _servicio.PuedeAnular(l) && _sesionActual.Puede("Nomina.Anular"));

        // El panel de detalle cuelga de la selección: al cambiar de liquidación hay que
        // reevaluar lo que muestra, porque son propiedades derivadas de SelectedItem.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(SelectedItem))
                return;

            LineaSeleccionada = null;
            OnPropertyChanged(nameof(HayDetalle));
            OnPropertyChanged(nameof(TotalesTexto));
        };
    }

    // --- Encabezado de la pantalla ---

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand GenerarCommand { get; }
    public ICommand AgregarConceptoCommand { get; }
    public ICommand QuitarLineaCommand { get; }
    public ICommand CerrarCommand { get; }
    public ICommand PagarCommand { get; }
    public ICommand AnularCommand { get; }

    private LiquidacionLinea? _lineaSeleccionada;
    public LiquidacionLinea? LineaSeleccionada
    {
        get => _lineaSeleccionada;
        set
        {
            if (SetProperty(ref _lineaSeleccionada, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Detalle de la liquidación seleccionada; la vista lo muestra bajo la grilla.</summary>
    public bool HayDetalle => SelectedItem is not null;

    public string TotalesTexto => SelectedItem is { } l
        ? $"Devengos {l.TotalDevengos:N2} · Deducciones {l.TotalDeducciones:N2} · Neto {l.Neto:N2}"
        : string.Empty;

    // --- Puntos de extensión del CRUD ---

    protected override string ModuloPermiso => "Nomina";

    protected override bool CoincideBusqueda(Liquidacion item, string texto) =>
        item.SujetoNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.SujetoCodigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.SujetoTipoTexto.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(Liquidacion item) => _filtroEstado switch
    {
        "Borrador" => item.Estado == EstadoLiquidacion.Borrador,
        "Cerrada" => item.Estado == EstadoLiquidacion.Cerrada,
        "Pagada" => item.Estado == EstadoLiquidacion.Pagada,
        "Anulada" => item.Estado == EstadoLiquidacion.Anulada,
        _ => true
    };

    /// <summary>Nadie edita una liquidación a mano: se ajusta con conceptos o se regenera.</summary>
    protected override bool PuedeEditar(Liquidacion item) => false;

    protected override bool PuedeEliminar(Liquidacion item) => _servicio.PuedeEliminar(item);

    protected override Liquidacion CrearNuevo() =>
        throw new NotSupportedException("Las liquidaciones se generan con GenerarCommand, que pasa por LiquidacionService.");

    protected override CrudEditorViewModelBase<Liquidacion> CrearEditor(Liquidacion item) =>
        throw new NotSupportedException("La liquidación no se edita en un formulario: se ajusta con conceptos.");

    // --- Acciones ---

    private void Generar()
    {
        var editor = new GenerarLiquidacionEditorViewModel(DataSourceFactory.CrearEmpleados());

        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var generada = editor.SujetoTipo == SujetoLiquidacion.Nucleo
                ? _servicio.GenerarParaNucleo(
                    Ambito.ExigirCodigoCam(),
                    Ambito.Actual!.Nombre,
                    editor.Desde, editor.Hasta,
                    _sesionActual.UsuarioActual?.Id ?? 0)
                : _servicio.GenerarParaEmpleado(
                    editor.EmpleadoSeleccionado!,
                    editor.Desde, editor.Hasta,
                    _sesionActual.UsuarioActual?.Id ?? 0);

            SeleccionarTrasRecargar(generada.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo generar la liquidación", ex.Message);
        }
    }

    private void AgregarConcepto()
    {
        if (SelectedItem is not { } liquidacion)
            return;

        var editor = new LineaConceptoEditorViewModel(liquidacion, DataSourceFactory.CrearConceptosNomina());
        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.AgregarLineaConcepto(
            liquidacion, editor.ConceptoSeleccionado!, editor.MontoValor));
    }

    private void QuitarLinea()
    {
        if (SelectedItem is not { } liquidacion || LineaSeleccionada is not { } linea)
            return;

        Aplicar(() => _servicio.QuitarLinea(liquidacion, linea));
    }

    private void Cerrar()
    {
        if (SelectedItem is not { } liquidacion)
            return;

        Aplicar(() => _servicio.Cerrar(liquidacion));
    }

    private void Pagar()
    {
        if (SelectedItem is not { } liquidacion)
            return;

        if (!_dialogos.Confirmar("Registrar pago",
                $"¿Registrar el pago de {liquidacion.NetoTexto} a {liquidacion.SujetoTexto}?"))
            return;

        Aplicar(() => _servicio.Pagar(liquidacion));
    }

    private void Anular()
    {
        if (SelectedItem is not { } liquidacion)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular liquidación Nº {liquidacion.Id}",
            $"{liquidacion.SujetoTexto} — {liquidacion.PeriodoTexto} — neto {liquidacion.NetoTexto}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Anular(liquidacion, editor.Motivo));
    }

    /// <summary>
    /// Ejecuta una transición. Si el servicio la rechaza se informa en vez de tragarse el error:
    /// el botón habilitado es cortesía, la regla la impone el servicio.
    ///
    /// La lista la repuebla la recarga que dispara la escritura del servicio; aquí solo se apunta
    /// qué liquidación dejar seleccionada.
    /// </summary>
    private void Aplicar(Func<Liquidacion> transicion)
    {
        try
        {
            SeleccionarTrasRecargar(transicion().Id);
            LineaSeleccionada = null;
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
    }
}
