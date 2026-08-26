using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Comparar precios entre proveedores para una requisición enviada, elegir el ganador, y
/// completar el detalle de cada línea (precio unitario; marca, clase y presentación en
/// lubricante) con el que se arma la orden de compra. No hereda de la base genérica: no edita
/// una entidad existente, solo junta las cotizaciones y las líneas con las que
/// <see cref="ComprasService.CrearDesdeRequisicion"/> arma la orden — ya completa, lista para
/// que "Órdenes de compra" solo la autorice.
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

        LineasOrden = [];

        AgregarCotizacionCommand = new RelayCommand(AgregarCotizacion);
        NuevoProveedorCommand = new RelayCommand(NuevoProveedor, () => _sesion.Puede(Permisos.Proveedores.Crear));
        NuevoMarcaCommand = new RelayCommand<OrdenCompraLinea>(NuevoMarca, _ => _sesion.Puede(Permisos.Lubricantes.Crear));

        GanadoraSeleccionada = Cotizaciones.FirstOrDefault();
    }

    public override string Titulo => $"Comparar proveedores — Requisición Nº {Requisicion.Id}";
    public override string TextoAccion => "Armar orden de compra";
    public override double AnchoEditor => Ancho.Amplio;

    public Requisicion Requisicion { get; }

    public ObservableCollection<Proveedor> Proveedores { get; }

    public ObservableCollection<CotizacionProveedor> Cotizaciones { get; }

    public ObservableCollection<OrdenCompraLinea> LineasOrden { get; private set; }

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

    private void NuevoMarca(OrdenCompraLinea? linea)
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

    private string _montoLineaTexto = string.Empty;
    public string MontoLineaTexto
    {
        get => _montoLineaTexto;
        set => SetProperty(ref _montoLineaTexto, value);
    }

    private string _notasLineaTexto = string.Empty;
    public string NotasLineaTexto
    {
        get => _notasLineaTexto;
        set => SetProperty(ref _notasLineaTexto, value);
    }

    private CotizacionProveedor? _ganadoraSeleccionada;
    public CotizacionProveedor? GanadoraSeleccionada
    {
        get => _ganadoraSeleccionada;
        set
        {
            if (!SetProperty(ref _ganadoraSeleccionada, value))
                return;

            // El detalle de línea se rearma desde cero cada vez que cambia la ganadora: es lo
            // último que se completa antes de armar, así que no vale la pena conservar ediciones
            // de una cotización que ya se dejó de lado.
            LineasOrden = value is null
                ? []
                : new ObservableCollection<OrdenCompraLinea>(ComprasService.ArmarLineasIniciales(Requisicion, value));

            OnPropertyChanged(nameof(LineasOrden));
            OnPropertyChanged(nameof(HayGanadora));
            OnPropertyChanged(nameof(TotalTexto));
        }
    }

    public bool HayGanadora => GanadoraSeleccionada is not null;

    /// <summary>Suma de subtotales, en dólares. Igual que el resto de la app, no se refresca
    /// solo mientras se escriben los precios línea por línea — se recalcula al reabrir.</summary>
    public string TotalTexto => LineasOrden.Sum(l => l.Subtotal).ToString("N2");

    private void AgregarCotizacion()
    {
        if (ProveedorLineaSeleccionado is not { } proveedor)
        {
            ErrorValidacion = "Seleccione el proveedor cotizado.";
            return;
        }

        if (!decimal.TryParse(MontoLineaTexto, out var monto) || monto <= 0)
        {
            ErrorValidacion = "El monto cotizado debe ser un número mayor que cero.";
            return;
        }

        var cotizacion = _cotizacionesFuente.Add(new CotizacionProveedor
        {
            RequisicionId = Requisicion.Id,
            ProveedorId = proveedor.Id,
            ProveedorNombre = proveedor.Nombre,
            MontoTotal = monto,
            Notas = NotasLineaTexto.Trim(),
            Fecha = DateTime.Today
        });

        Cotizaciones.Add(cotizacion);
        GanadoraSeleccionada ??= cotizacion;

        ErrorValidacion = null;
        MontoLineaTexto = string.Empty;
        NotasLineaTexto = string.Empty;
    }

    protected override bool Validar(out string? error)
    {
        // El ComboBox de Marca liga por Id (SelectedValue); el nombre snapshot de cada línea no
        // se sincroniza solo al elegir una marca distinta, así que se recalcula aquí, justo antes
        // de dejar pasar la línea — mismo criterio que antes aplicaba OrdenCompraEditorViewModel
        // al guardar.
        foreach (var linea in LineasOrden)
            linea.MarcaLubricanteNombre = MarcasLubricante.FirstOrDefault(m => m.Id == linea.MarcaLubricanteId)?.Nombre ?? string.Empty;

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

        if (LineasOrden.Any(l => l.PrecioUnitario <= 0))
        {
            error = "Indique el precio unitario de cada línea.";
            return false;
        }

        if (LineasOrden.Any(l => l.EsLubricante && l.MarcaLubricanteId is null))
        {
            error = "Seleccione la marca de cada línea de lubricante.";
            return false;
        }

        if (LineasOrden.Any(l => l.EsLubricante && string.IsNullOrWhiteSpace(l.ClaseLubricante)))
        {
            error = "Seleccione la clase (mineral/sintético) de cada línea de lubricante.";
            return false;
        }

        if (LineasOrden.Any(l => l.EsLubricante && string.IsNullOrWhiteSpace(l.Presentacion)))
        {
            error = "Seleccione la presentación de cada línea de lubricante.";
            return false;
        }

        error = null;
        return true;
    }
}
