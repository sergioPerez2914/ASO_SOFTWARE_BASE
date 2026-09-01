using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Fila de solo lectura del panel "Necesidades": cuánto pide una línea de la requisición y cuánto
/// llevan cubierto, hasta ahora, las líneas de compra agregadas contra ella. Puramente informativa
/// — no bloquea agregar de más ni de menos, mismo criterio que "Pedido" vs. "Recibido" en Recepción.
/// </summary>
public sealed class NecesidadCobertura
{
    public required string DestinoTexto { get; init; }
    public required string PedidoTexto { get; init; }
    public required string CubiertoTexto { get; init; }

    /// <summary>"Sin cubrir" / "Falta X" / "Completo" — es lo que colorea el chip
    /// (<c>ChipCoberturaStyle</c>) para que se note de un vistazo, sin tener que leer los números.</summary>
    public required string EstadoTexto { get; init; }

    public required bool Completo { get; init; }
    public required bool SinCubrir { get; init; }
}

/// <summary>
/// Comparar precios entre proveedores para una requisición enviada y elegir el ganador. Cada
/// proveedor se cotiza como una factura propia: se arma su detalle línea por línea con "Agregar
/// línea" (mismo arquetipo que <see cref="RequisicionEditorViewModel"/>) y el monto sale solo,
/// sumando línea por línea — no se teclea un total suelto. No hereda de la base genérica: no edita
/// una entidad existente, solo junta las cotizaciones (cada una ya con su detalle completo) con la
/// que <see cref="ComprasService.CrearDesdeRequisicion"/> arma la orden — ya completa, lista para
/// que "Órdenes de compra" solo la autorice.
///
/// Una necesidad de la requisición (p. ej. "150 L de un grado de lubricante") puede cubrirse con
/// VARIAS líneas de compra — distintas marcas o presentaciones del mismo proveedor, cada una con su
/// propia cantidad y precio — así que "Agregar línea" no arma la grilla 1:1 con la requisición: el
/// usuario elige contra qué necesidad va cada línea, y el panel "Necesidades" muestra cuánto lleva
/// cubierto cada una, solo como referencia.
///
/// Cada cotización se guarda al agregarla, no al cerrar el editor: así el historial de precios
/// comparados queda aunque, al final, no se arme la orden en ese mismo momento.
/// </summary>
public sealed class CompararProveedoresEditorViewModel : CrudEditorViewModelBase
{
    private readonly IProveedorDataSource _proveedores;
    private readonly ICotizacionProveedorDataSource _cotizacionesFuente;
    private readonly IMarcaLubricanteDataSource _marcasLubricante;
    private readonly ComprasService _servicio;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    public CompararProveedoresEditorViewModel(Requisicion requisicion,
                                              IProveedorDataSource proveedores,
                                              ICotizacionProveedorDataSource cotizacionesFuente,
                                              IMarcaLubricanteDataSource marcasLubricante,
                                              ComprasService servicio,
                                              IServicioDialogo dialogos,
                                              ISesionActual sesion)
    {
        Requisicion = requisicion;
        _proveedores = proveedores;
        _cotizacionesFuente = cotizacionesFuente;
        _marcasLubricante = marcasLubricante;
        _servicio = servicio;
        _dialogos = dialogos;
        _sesion = sesion;

        Proveedores = new ObservableCollection<Proveedor>(
            proveedores.GetAll().Where(p => p.Activo).OrderBy(p => p.Nombre));
        ProveedorLineaSeleccionado = Proveedores.FirstOrDefault();

        Cotizaciones = new ObservableCollection<CotizacionProveedor>(
            cotizacionesFuente.GetByRequisicion(requisicion.Id));

        MarcasLubricante = new ObservableCollection<MarcaLubricante>(
            marcasLubricante.GetAll().Where(m => m.Activo).OrderBy(m => m.Nombre));

        LineasCotizacion = new ObservableCollection<CotizacionProveedorLinea>();

        NecesidadSeleccionada = Requisicion.Lineas.FirstOrDefault();
        MarcaLineaSeleccionada = MarcasLubricante.FirstOrDefault();
        ClaseLubricanteLineaSeleccionada = Lubricante.Tipos[0];
        PresentacionLineaSeleccionada = Lubricante.Presentaciones[0];

        AgregarLineaCotizacionCommand = new RelayCommand(AgregarLineaCotizacion);
        QuitarLineaCotizacionCommand = new RelayCommand<CotizacionProveedorLinea>(QuitarLineaCotizacion);
        AgregarCotizacionCommand = new RelayCommand(AgregarCotizacion);
        NuevoProveedorCommand = new RelayCommand(NuevoProveedor, () => _sesion.Puede(Permisos.Proveedores.Crear));
        NuevoMarcaCommand = new RelayCommand(NuevoMarca, () => _sesion.Puede(Permisos.Lubricantes.Crear));

        RecalcularCobertura();

        GanadoraSeleccionada = Cotizaciones.FirstOrDefault();
    }

    public override string Titulo => $"Comparar proveedores — Requisición Nº {Requisicion.Id}";
    public override string TextoAccion => "Armar orden de compra";
    public override double AnchoEditor => Ancho.Amplio;

    public Requisicion Requisicion { get; }

    public ObservableCollection<Proveedor> Proveedores { get; }

    public ObservableCollection<CotizacionProveedor> Cotizaciones { get; }

    /// <summary>Líneas de compra ya agregadas contra el proveedor que se está cotizando ahora
    /// mismo — se reinicia después de cada "Agregar cotización" para cotizar al siguiente.</summary>
    public ObservableCollection<CotizacionProveedorLinea> LineasCotizacion { get; private set; }

    public ObservableCollection<MarcaLubricante> MarcasLubricante { get; }

    public IReadOnlyList<string> ClasesLubricante => Lubricante.Tipos;
    public IReadOnlyList<string> Presentaciones => Lubricante.Presentaciones;

    /// <summary>Panel de solo lectura: pedido vs. cubierto por cada línea de la requisición, con
    /// las líneas de compra agregadas hasta ahora. Se reconstruye entero en cada Agregar/Quitar —
    /// los modelos no implementan INotifyPropertyChanged, mismo criterio que el resto de la app.</summary>
    public IReadOnlyList<NecesidadCobertura> NecesidadesCobertura { get; private set; } = [];

    public ICommand AgregarLineaCotizacionCommand { get; }
    public ICommand QuitarLineaCotizacionCommand { get; }
    public ICommand AgregarCotizacionCommand { get; }
    public ICommand NuevoProveedorCommand { get; }
    public ICommand NuevoMarcaCommand { get; }

    private void NuevoProveedor()
    {
        var editor = new ProveedorEditorViewModel(new Proveedor(), _proveedores);
        if (!_dialogos.MostrarEditor(editor))
            return;

        var nuevo = _proveedores.Add(editor.ObtenerResultado());
        Proveedores.Add(nuevo);
        ProveedorLineaSeleccionado = nuevo;
    }

    private void NuevoMarca()
    {
        var editor = new MarcaLubricanteEditorViewModel();
        if (!_dialogos.MostrarEditor(editor))
            return;

        var nueva = _marcasLubricante.Add(editor.ObtenerResultado());
        MarcasLubricante.Add(nueva);
        MarcaLineaSeleccionada = nueva;
    }

    private Proveedor? _proveedorLineaSeleccionado;
    public Proveedor? ProveedorLineaSeleccionado
    {
        get => _proveedorLineaSeleccionado;
        set => SetProperty(ref _proveedorLineaSeleccionado, value);
    }

    private string _notasLineaTexto = string.Empty;
    public string NotasLineaTexto
    {
        get => _notasLineaTexto;
        set => SetProperty(ref _notasLineaTexto, value);
    }

    private RequisicionLinea? _necesidadSeleccionada;
    /// <summary>A qué línea de la requisición corresponde la próxima línea de compra a agregar.</summary>
    public RequisicionLinea? NecesidadSeleccionada
    {
        get => _necesidadSeleccionada;
        set
        {
            if (SetProperty(ref _necesidadSeleccionada, value))
            {
                OnPropertyChanged(nameof(EsNecesidadLubricante));
                OnPropertyChanged(nameof(EtiquetaCantidadLinea));
            }
        }
    }

    public bool EsNecesidadLubricante =>
        NecesidadSeleccionada?.TipoCombustibleSolicitado == TipoCombustible.Lubricante;

    /// <summary>Aclara en qué unidad se está pidiendo la cantidad de la línea — mismo criterio que
    /// <see cref="RequisicionEditorViewModel.EtiquetaCantidad"/>, para que "Cantidad" nunca quede
    /// ambiguo entre litros y unidades de repuesto.</summary>
    public string EtiquetaCantidadLinea => NecesidadSeleccionada?.TipoInsumo == TipoInsumo.Combustible
        ? "Cantidad (litros)"
        : "Cantidad (unidades)";

    private MarcaLubricante? _marcaLineaSeleccionada;
    public MarcaLubricante? MarcaLineaSeleccionada
    {
        get => _marcaLineaSeleccionada;
        set => SetProperty(ref _marcaLineaSeleccionada, value);
    }

    private string _claseLubricanteLineaSeleccionada = string.Empty;
    public string ClaseLubricanteLineaSeleccionada
    {
        get => _claseLubricanteLineaSeleccionada;
        set => SetProperty(ref _claseLubricanteLineaSeleccionada, value);
    }

    private string _presentacionLineaSeleccionada = string.Empty;
    public string PresentacionLineaSeleccionada
    {
        get => _presentacionLineaSeleccionada;
        set => SetProperty(ref _presentacionLineaSeleccionada, value);
    }

    private string _litrosPorEnvaseLineaTexto = string.Empty;
    public string LitrosPorEnvaseLineaTexto
    {
        get => _litrosPorEnvaseLineaTexto;
        set
        {
            if (SetProperty(ref _litrosPorEnvaseLineaTexto, value))
                OnPropertyChanged(nameof(EquivalenciaLineaTexto));
        }
    }

    private string _unidadesLineaTexto = string.Empty;
    /// <summary>Cuántos envases de <see cref="PresentacionLineaSeleccionada"/> se compran. Junto
    /// con <see cref="LitrosPorEnvaseLineaTexto"/> sirve para no tener que calcular a mano cuántos
    /// litros son — si se completan los dos y se deja "Cantidad" vacío, "Agregar línea" calcula
    /// la cantidad sola (envases × litros por envase). Sigue siendo posible escribir "Cantidad"
    /// directo si no se sabe con precisión cuántos envases entran.</summary>
    public string UnidadesLineaTexto
    {
        get => _unidadesLineaTexto;
        set
        {
            if (SetProperty(ref _unidadesLineaTexto, value))
                OnPropertyChanged(nameof(EquivalenciaLineaTexto));
        }
    }

    /// <summary>Vista previa en vivo de "envases × litros por envase", para que la relación entre
    /// los dos campos sea visible antes de agregar la línea.</summary>
    public string EquivalenciaLineaTexto =>
        decimal.TryParse(UnidadesLineaTexto, out var unidades) && unidades > 0
        && decimal.TryParse(LitrosPorEnvaseLineaTexto, out var litrosPorEnvase) && litrosPorEnvase > 0
            ? $"= {unidades * litrosPorEnvase:N2} L"
            : string.Empty;

    private string _cantidadLineaTexto = string.Empty;
    public string CantidadLineaTexto
    {
        get => _cantidadLineaTexto;
        set => SetProperty(ref _cantidadLineaTexto, value);
    }

    /// <summary>Suma de subtotales de la cotización en borrador, como el total de una factura.
    /// Igual que el resto de la app, no se refresca solo mientras se escriben los precios línea
    /// por línea (los modelos no implementan INotifyPropertyChanged) — se recalcula al agregar o
    /// quitar una línea, y al terminar de agregar una cotización.</summary>
    public string TotalLineasCotizacionTexto => LineasCotizacion.Sum(l => l.Subtotal).ToString("N2");

    private CotizacionProveedor? _ganadoraSeleccionada;
    public CotizacionProveedor? GanadoraSeleccionada
    {
        get => _ganadoraSeleccionada;
        set
        {
            if (!SetProperty(ref _ganadoraSeleccionada, value))
                return;

            OnPropertyChanged(nameof(HayGanadora));
            OnPropertyChanged(nameof(LineasGanadora));
        }
    }

    public bool HayGanadora => GanadoraSeleccionada is not null;

    /// <summary>Vista previa de solo lectura del detalle ya completo de la cotización ganadora —
    /// no hace falta volver a llenar nada, ya se cargó al cotizar a ese proveedor.</summary>
    public IReadOnlyList<CotizacionProveedorLinea> LineasGanadora => GanadoraSeleccionada?.Lineas ?? [];

    private void AgregarLineaCotizacion()
    {
        if (NecesidadSeleccionada is not { } necesidad)
        {
            ErrorValidacion = "Seleccione a qué necesidad de la requisición corresponde esta línea.";
            return;
        }

        var hayUnidades = decimal.TryParse(UnidadesLineaTexto, out var unidades) && unidades > 0;
        var hayLitrosPorEnvase = decimal.TryParse(LitrosPorEnvaseLineaTexto, out var litrosPorEnvase) && litrosPorEnvase > 0;
        var hayCantidad = decimal.TryParse(CantidadLineaTexto, out var cantidad) && cantidad > 0;

        if (!hayCantidad && hayUnidades && hayLitrosPorEnvase)
        {
            // No hizo falta calcular a mano "cuántos litros son": si se sabe cuántos envases se
            // compran y cuánto trae cada uno, la cantidad sale sola.
            cantidad = unidades * litrosPorEnvase;
            hayCantidad = true;
        }

        if (!hayCantidad)
        {
            ErrorValidacion = "Indique la cantidad de la línea, o los envases y litros por envase para calcularla.";
            return;
        }

        var linea = new CotizacionProveedorLinea
        {
            RequisicionLineaIndex = Requisicion.Lineas.IndexOf(necesidad),
            TipoInsumo = necesidad.TipoInsumo,
            TipoCombustibleSolicitado = necesidad.TipoCombustibleSolicitado,
            TipoLubricante = necesidad.TipoLubricante,
            ArticuloCodigo = necesidad.ArticuloCodigo,
            ArticuloNombre = necesidad.ArticuloNombre,
            ActivoId = necesidad.ActivoId,
            ActivoEtiqueta = necesidad.ActivoEtiqueta,
            Cantidad = cantidad,
            UnidadTexto = necesidad.UnidadTexto,
            PrecioUnitario = 0m
        };

        if (EsNecesidadLubricante)
        {
            if (MarcaLineaSeleccionada is not { } marca)
            {
                ErrorValidacion = "Seleccione la marca del lubricante.";
                return;
            }

            if (string.IsNullOrWhiteSpace(PresentacionLineaSeleccionada))
            {
                ErrorValidacion = "Seleccione la presentación del lubricante.";
                return;
            }

            linea.MarcaLubricanteId = marca.Id;
            linea.MarcaLubricanteNombre = marca.Nombre;
            linea.ClaseLubricante = ClaseLubricanteLineaSeleccionada;
            linea.Presentacion = PresentacionLineaSeleccionada;
            linea.LitrosPorEnvase = hayLitrosPorEnvase ? litrosPorEnvase : null;
            linea.Unidades = hayUnidades ? unidades : null;
        }

        LineasCotizacion.Add(linea);
        RecalcularCobertura();
        OnPropertyChanged(nameof(TotalLineasCotizacionTexto));

        ErrorValidacion = null;
        CantidadLineaTexto = string.Empty;
        UnidadesLineaTexto = string.Empty;
        LitrosPorEnvaseLineaTexto = string.Empty;
        // La necesidad, marca, clase y presentación seleccionadas se conservan a propósito: es lo
        // que permite agregar varias líneas seguidas contra la misma necesidad (otra marca u otro
        // envase) sin volver a elegir todo desde cero.
    }

    private void QuitarLineaCotizacion(CotizacionProveedorLinea? linea)
    {
        if (linea is null)
            return;

        LineasCotizacion.Remove(linea);
        RecalcularCobertura();
        OnPropertyChanged(nameof(TotalLineasCotizacionTexto));
    }

    private void RecalcularCobertura()
    {
        NecesidadesCobertura = Requisicion.Lineas.Select((necesidad, indice) =>
        {
            var cubierto = LineasCotizacion.Where(l => l.RequisicionLineaIndex == indice).Sum(l => l.Cantidad);
            var completo = cubierto >= necesidad.Cantidad;
            var sinCubrir = cubierto <= 0;

            return new NecesidadCobertura
            {
                DestinoTexto = necesidad.DestinoTexto,
                PedidoTexto = necesidad.CantidadTexto,
                CubiertoTexto = $"{cubierto:N2} {necesidad.UnidadTexto}".Trim(),
                Completo = completo,
                SinCubrir = sinCubrir,
                EstadoTexto = completo
                    ? "Completo"
                    : sinCubrir
                        ? "Sin cubrir"
                        : $"Falta {necesidad.Cantidad - cubierto:N2}"
            };
        }).ToList();

        OnPropertyChanged(nameof(NecesidadesCobertura));
    }

    private void AgregarCotizacion()
    {
        if (ProveedorLineaSeleccionado is not { } proveedor)
        {
            ErrorValidacion = "Seleccione el proveedor cotizado.";
            return;
        }

        if (!ComprasService.CotizacionEstaCompleta(Requisicion, [.. LineasCotizacion], out var faltantes))
        {
            ErrorValidacion = $"Faltan datos para cotizar: {faltantes}.";
            return;
        }

        var cotizacion = _cotizacionesFuente.Add(new CotizacionProveedor
        {
            RequisicionId = Requisicion.Id,
            ProveedorId = proveedor.Id,
            ProveedorNombre = proveedor.Nombre,
            Notas = NotasLineaTexto.Trim(),
            Fecha = DateTime.Today,
            Lineas = LineasCotizacion.Select(l => l.Clonar()).ToList()
        });

        Cotizaciones.Add(cotizacion);
        GanadoraSeleccionada ??= cotizacion;

        // Se reinicia para cotizar al siguiente proveedor: no vale la pena conservar lo que ya
        // quedó guardado en esta cotización.
        LineasCotizacion = new ObservableCollection<CotizacionProveedorLinea>();
        OnPropertyChanged(nameof(LineasCotizacion));
        OnPropertyChanged(nameof(TotalLineasCotizacionTexto));
        RecalcularCobertura();

        ErrorValidacion = null;
        NotasLineaTexto = string.Empty;
    }

    protected override bool Validar(out string? error)
    {
        if (Cotizaciones.Count == 0)
        {
            error = "Capture al menos una cotización antes de armar la orden de compra.";
            return false;
        }

        if (GanadoraSeleccionada is null)
        {
            error = "Seleccione la cotización ganadora.";
            return false;
        }

        error = null;
        return true;
    }
}
