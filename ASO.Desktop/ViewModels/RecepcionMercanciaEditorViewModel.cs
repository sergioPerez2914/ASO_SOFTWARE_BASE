using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Edición de una recepción en borrador: corregir la cantidad realmente recibida de cada línea
/// (nace prellenada con la cantidad pedida) y, para las líneas de diésel, elegir en qué
/// presentación llegó — la empresa no tiene una cisterna común que asignar, así que
/// <c>ComprasService.ConfirmarRecepcion</c> resuelve solo el stock general "Diesel" al que se
/// suma. Las líneas de lubricante no piden nada aquí: Marca, Clase y Presentación ya se fijaron
/// al armar la orden de compra, y el <see cref="Lubricante"/> concreto lo resuelve
/// <c>ComprasService.ConfirmarRecepcion</c> solo (lo busca o lo crea). Las líneas no se agregan
/// ni se quitan aquí — vienen fijas de la orden de compra de origen.
/// </summary>
public sealed class RecepcionMercanciaEditorViewModel : CrudEditorViewModelBase<RecepcionMercancia>
{
    private readonly RecepcionMercancia _original;

    public RecepcionMercanciaEditorViewModel(RecepcionMercancia original)
    {
        _original = original;
        Notas = original.Notas;

        Lineas = new ObservableCollection<RecepcionMercanciaLinea>(original.Lineas.Select(l => l.Clonar()));
    }

    public override string Titulo => $"Editar recepción Nº {_original.Id} — Orden de compra Nº {_original.OrdenCompraId}";

    public override double AnchoEditor => Ancho.Amplio;

    public string ResumenProveedor => $"{_original.ProveedorNombre} · orden de compra Nº {_original.OrdenCompraId}";

    public IReadOnlyList<string> PresentacionesCombustible => RecepcionMercanciaLinea.PresentacionesDiesel;

    public ObservableCollection<RecepcionMercanciaLinea> Lineas { get; }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    protected override bool Validar(out string? error)
    {
        if (Lineas.Any(l => l.CantidadRecibida < 0))
        {
            error = "La cantidad recibida no puede ser negativa.";
            return false;
        }

        if (Lineas.All(l => l.CantidadRecibida <= 0))
        {
            error = "Indique al menos una cantidad recibida mayor que cero.";
            return false;
        }

        if (Lineas.Any(l => l.EsDiesel && l.CantidadRecibida > 0 && string.IsNullOrWhiteSpace(l.Presentacion)))
        {
            error = "Seleccione la presentación de cada línea de diésel recibida.";
            return false;
        }

        error = null;
        return true;
    }

    public override RecepcionMercancia ObtenerResultado()
    {
        var recepcion = _original.Clonar();
        recepcion.Notas = Notas.Trim();
        recepcion.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return recepcion;
    }
}
