using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Comparar precios entre proveedores para una requisición enviada, y elegir el ganador con el
/// que se arma la orden de compra. No hereda de la base genérica: no edita una entidad existente,
/// solo junta las cotizaciones con las que <see cref="ComprasService.CrearDesdeRequisicion"/>
/// arma la orden.
///
/// Cada cotización se guarda al agregarla, no al cerrar el editor: así el historial de precios
/// comparados queda aunque, al final, no se arme la orden en ese mismo momento.
/// </summary>
public sealed class CompararProveedoresEditorViewModel : CrudEditorViewModelBase
{
    private readonly ICotizacionProveedorDataSource _cotizacionesFuente;

    public CompararProveedoresEditorViewModel(Requisicion requisicion,
                                              IProveedorDataSource proveedores,
                                              ICotizacionProveedorDataSource cotizacionesFuente)
    {
        Requisicion = requisicion;
        _cotizacionesFuente = cotizacionesFuente;

        Proveedores = proveedores.GetAll().Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();
        ProveedorLineaSeleccionado = Proveedores.FirstOrDefault();

        Cotizaciones = new ObservableCollection<CotizacionProveedor>(
            cotizacionesFuente.GetByRequisicion(requisicion.Id));
        GanadoraSeleccionada = Cotizaciones.FirstOrDefault();

        AgregarCotizacionCommand = new RelayCommand(AgregarCotizacion);
    }

    public override string Titulo => $"Comparar proveedores — Requisición Nº {Requisicion.Id}";
    public override string TextoAccion => "Armar orden de compra";
    public override double AnchoEditor => Ancho.Amplio;

    public Requisicion Requisicion { get; }

    public IReadOnlyList<Proveedor> Proveedores { get; }

    public ObservableCollection<CotizacionProveedor> Cotizaciones { get; }

    public ICommand AgregarCotizacionCommand { get; }

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
        set => SetProperty(ref _ganadoraSeleccionada, value);
    }

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
