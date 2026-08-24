using System.Collections.ObjectModel;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Edición de una orden de compra en borrador: completar el precio unitario de cada línea
/// (copiadas de la requisición al armarla) y las notas. Las líneas no se agregan ni se quitan
/// aquí — vienen fijas de la requisición de origen; solo se les pone precio.
/// </summary>
public sealed class OrdenCompraEditorViewModel : CrudEditorViewModelBase<OrdenCompra>
{
    private readonly OrdenCompra _original;

    public OrdenCompraEditorViewModel(OrdenCompra original)
    {
        _original = original;
        Notas = original.Notas;
        Lineas = new ObservableCollection<OrdenCompraLinea>(original.Lineas.Select(l => l.Clonar()));
    }

    public override string Titulo => $"Editar orden de compra Nº {_original.Id}";

    public override double AnchoEditor => Ancho.Amplio;

    public string ResumenProveedor => $"{_original.ProveedorNombre} · requisición Nº {_original.RequisicionId}";

    public string MontoCotizadoTexto => _original.MontoCotizadoTexto;

    public ObservableCollection<OrdenCompraLinea> Lineas { get; }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    protected override bool Validar(out string? error)
    {
        if (Lineas.Any(l => l.PrecioUnitario <= 0))
        {
            error = "Indique el precio unitario de cada línea.";
            return false;
        }

        error = null;
        return true;
    }

    public override OrdenCompra ObtenerResultado()
    {
        var orden = _original.Clonar();
        orden.Notas = Notas.Trim();
        orden.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return orden;
    }
}
