using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Comparar precios entre proveedores para una requisición enviada y elegir el ganador. Cada
/// proveedor se cotiza como una factura propia: se completa su detalle de línea (precio unitario;
/// marca, clase y presentación en lubricante) y el monto sale solo, sumando línea por línea — no
/// se teclea un total suelto. No hereda de la base genérica: no edita una entidad existente, solo
/// junta las cotizaciones (cada una ya con su detalle completo) con la que
/// <see cref="ComprasService.CrearDesdeRequisicion"/> arma la orden — ya completa, lista para que
/// "Órdenes de compra" solo la autorice.
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

        LineasCotizacion = new ObservableCollection<CotizacionProveedorLinea>(
            ComprasService.ArmarLineasCotizacion(Requisicion));

        AgregarCotizacionCommand = new RelayCommand(AgregarCotizacion);
        NuevoProveedorCommand = new RelayCommand(NuevoProveedor, () => _sesion.Puede(Permisos.Proveedores.Crear));
        NuevoMarcaCommand = new RelayCommand<CotizacionProveedorLinea>(NuevoMarca, _ => _sesion.Puede(Permisos.Lubricantes.Crear));

        GanadoraSeleccionada = Cotizaciones.FirstOrDefault();
    }

    public override string Titulo => $"Comparar proveedores — Requisición Nº {Requisicion.Id}";
    public override string TextoAccion => "Armar orden de compra";
    public override double AnchoEditor => Ancho.Amplio;

    public Requisicion Requisicion { get; }

    public ObservableCollection<Proveedor> Proveedores { get; }

    public ObservableCollection<CotizacionProveedor> Cotizaciones { get; }

    /// <summary>Detalle en borrador del proveedor que se está cotizando ahora mismo — se reinicia
    /// después de cada "Agregar cotización" para cotizar al siguiente.</summary>
    public ObservableCollection<CotizacionProveedorLinea> LineasCotizacion { get; private set; }

    public ObservableCollection<MarcaLubricante> MarcasLubricante { get; }

    public IReadOnlyList<string> ClasesLubricante => Lubricante.Tipos;
    public IReadOnlyList<string> Presentaciones => Lubricante.Presentaciones;

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

    private void NuevoMarca(CotizacionProveedorLinea? linea)
    {
        if (linea is null)
            return;

        var editor = new MarcaLubricanteEditorViewModel();
        if (!_dialogos.MostrarEditor(editor))
            return;

        var nueva = _marcasLubricante.Add(editor.ObtenerResultado());
        MarcasLubricante.Add(nueva);
        linea.MarcaLubricanteId = nueva.Id;
        linea.MarcaLubricanteNombre = nueva.Nombre;
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

    /// <summary>Suma de subtotales de la cotización en borrador, como el total de una factura.
    /// Igual que el resto de la app, no se refresca solo mientras se escriben los precios línea
    /// por línea (los modelos no implementan INotifyPropertyChanged) — se recalcula al reabrir el
    /// editor o al terminar de agregar una cotización.</summary>
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

    private void AgregarCotizacion()
    {
        if (ProveedorLineaSeleccionado is not { } proveedor)
        {
            ErrorValidacion = "Seleccione el proveedor cotizado.";
            return;
        }

        // El ComboBox de Marca liga por Id (SelectedValue); el nombre snapshot de cada línea no
        // se sincroniza solo al elegir una marca distinta, así que se recalcula aquí, justo antes
        // de congelar la cotización.
        foreach (var linea in LineasCotizacion)
            linea.MarcaLubricanteNombre = MarcasLubricante.FirstOrDefault(m => m.Id == linea.MarcaLubricanteId)?.Nombre ?? string.Empty;

        // Cantidad (litros) de una línea de lubricante deja de ser lo pedido en la requisición y
        // pasa a reflejar lo que de verdad se compra: Unidades × litros del envase elegido. Se
        // recalcula aquí, no en vivo, por el mismo motivo que el resto de los totales de la app
        // (los modelos no implementan INotifyPropertyChanged).
        foreach (var linea in LineasCotizacion.Where(l => l.EsLubricante))
        {
            var litrosPorUnidad = Lubricante.LitrosPorPresentacion.GetValueOrDefault(linea.Presentacion ?? string.Empty, 0m);
            linea.Cantidad = linea.Unidades * litrosPorUnidad;
        }

        if (!ComprasService.CotizacionEstaCompleta([.. LineasCotizacion], out var faltantes))
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
        LineasCotizacion = new ObservableCollection<CotizacionProveedorLinea>(
            ComprasService.ArmarLineasCotizacion(Requisicion));
        OnPropertyChanged(nameof(LineasCotizacion));
        OnPropertyChanged(nameof(TotalLineasCotizacionTexto));

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
