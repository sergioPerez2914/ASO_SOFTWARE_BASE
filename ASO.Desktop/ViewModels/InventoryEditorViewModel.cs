using System;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Editor de alta/edición de un artículo de inventario. El <c>Codigo</c> es la clave:
/// se puede escribir al crear, pero queda bloqueado al editar.
/// </summary>
public class InventoryEditorViewModel : CrudEditorViewModelBase<InventoryItem>
{
    private readonly InventoryItem _original;
    private readonly Func<string, InventoryItem, bool> _codigoDisponible;

    public InventoryEditorViewModel(InventoryItem original,
                                    Func<string, InventoryItem, bool> codigoDisponible)
        : base(original)
    {
        _original = original;
        _codigoDisponible = codigoDisponible;

        _codigo = original.Codigo;
        _nombre = original.Nombre;
        _categoria = original.Categoria;
        _unidad = original.Unidad;
        _ubicacion = original.Ubicacion;
        _stockActual = original.StockActual;
        _stockMinimo = original.StockMinimo;
        _costoUnitario = original.CostoUnitario;
    }

    public bool EsNuevo => string.IsNullOrEmpty(_original.Codigo);

    /// <summary>El código solo es editable al dar de alta; en edición es de solo lectura (es la PK).</summary>
    public bool CodigoEditable => EsNuevo;

    public override string Titulo => EsNuevo ? "Nuevo artículo" : "Editar artículo";

    private string _codigo;
    public string Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    private string _nombre;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _categoria;
    public string Categoria
    {
        get => _categoria;
        set => SetProperty(ref _categoria, value);
    }

    private string _unidad;
    public string Unidad
    {
        get => _unidad;
        set => SetProperty(ref _unidad, value);
    }

    private string _ubicacion;
    public string Ubicacion
    {
        get => _ubicacion;
        set => SetProperty(ref _ubicacion, value);
    }

    private int _stockActual;
    public int StockActual
    {
        get => _stockActual;
        set => SetProperty(ref _stockActual, value);
    }

    private int _stockMinimo;
    public int StockMinimo
    {
        get => _stockMinimo;
        set => SetProperty(ref _stockMinimo, value);
    }

    private decimal _costoUnitario;
    public decimal CostoUnitario
    {
        get => _costoUnitario;
        set => SetProperty(ref _costoUnitario, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Codigo))
        {
            error = "El código es obligatorio.";
            return false;
        }

        if (EsNuevo && !_codigoDisponible(Codigo.Trim(), _original))
        {
            error = $"Ya existe un artículo con el código '{Codigo.Trim()}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "El nombre es obligatorio.";
            return false;
        }

        if (StockActual < 0 || StockMinimo < 0)
        {
            error = "El stock no puede ser negativo.";
            return false;
        }

        if (CostoUnitario < 0)
        {
            error = "El costo unitario no puede ser negativo.";
            return false;
        }

        error = null;
        return true;
    }

    public override InventoryItem ObtenerResultado() => new()
    {
        // En edición conserva el código original (la PK no cambia).
        Codigo = EsNuevo ? Codigo.Trim() : _original.Codigo,
        Nombre = Nombre.Trim(),
        Categoria = Categoria.Trim(),
        Unidad = Unidad.Trim(),
        Ubicacion = Ubicacion.Trim(),
        StockActual = StockActual,
        StockMinimo = StockMinimo,
        CostoUnitario = CostoUnitario
    };
}
