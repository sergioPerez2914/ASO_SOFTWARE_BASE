using System;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un artículo de almacén. El código es la identidad del artículo, así que
/// solo se escribe al crearlo: cambiarlo después dejaría huérfanas las salidas que lo citan.
/// </summary>
public sealed class InventoryItemEditorViewModel : CrudEditorViewModelBase<InventoryItem>
{
    private readonly InventoryItem _original;
    private readonly IInventoryDataSource _articulos;

    public InventoryItemEditorViewModel(InventoryItem original, IInventoryDataSource articulos, bool esNuevo)
    {
        _original = original;
        _articulos = articulos;
        EsNuevo = esNuevo;

        Codigo = original.Codigo;
        Nombre = original.Nombre;
        Categoria = original.Categoria;
        Unidad = string.IsNullOrWhiteSpace(original.Unidad) ? "und" : original.Unidad;
        Ubicacion = original.Ubicacion;
        StockActual = original.StockActual.ToString("0.##");
        StockMinimo = original.StockMinimo.ToString("0.##");
        CostoUnitario = original.CostoUnitario.ToString("0.##");
    }

    public override string Titulo => EsNuevo ? "Nuevo artículo" : $"Editar artículo {_original.Codigo}";
    public override double AnchoEditor => 460;

    /// <summary>El código solo es editable en el alta (la vista lo bloquea con esta bandera).</summary>
    public bool EsNuevo { get; }
    public bool CodigoBloqueado => !EsNuevo;

    private string _codigo = string.Empty;
    public string Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _categoria = string.Empty;
    public string Categoria
    {
        get => _categoria;
        set => SetProperty(ref _categoria, value);
    }

    private string _unidad = "und";
    public string Unidad
    {
        get => _unidad;
        set => SetProperty(ref _unidad, value);
    }

    private string _ubicacion = string.Empty;
    public string Ubicacion
    {
        get => _ubicacion;
        set => SetProperty(ref _ubicacion, value);
    }

    private string _stockActual = "0";
    public string StockActual
    {
        get => _stockActual;
        set => SetProperty(ref _stockActual, value);
    }

    private string _stockMinimo = "0";
    public string StockMinimo
    {
        get => _stockMinimo;
        set => SetProperty(ref _stockMinimo, value);
    }

    private string _costoUnitario = "0";
    public string CostoUnitario
    {
        get => _costoUnitario;
        set => SetProperty(ref _costoUnitario, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Codigo))
        {
            error = "Indique el código del artículo.";
            return false;
        }

        if (EsNuevo && _articulos.GetAll()
                .Any(a => string.Equals(a.Codigo, Codigo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            error = $"Ya existe un artículo con el código {Codigo.Trim()}.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el nombre del artículo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Unidad))
        {
            error = "Indique la unidad de medida (und, m, L, kg…).";
            return false;
        }

        if (!decimal.TryParse(StockActual, out var stock) || stock < 0)
        {
            error = "La existencia actual debe ser un número mayor o igual a cero.";
            return false;
        }

        if (!decimal.TryParse(StockMinimo, out var minimo) || minimo < 0)
        {
            error = "El stock mínimo debe ser un número mayor o igual a cero.";
            return false;
        }

        if (!decimal.TryParse(CostoUnitario, out var costo) || costo < 0)
        {
            error = "El costo unitario debe ser un número mayor o igual a cero.";
            return false;
        }

        error = null;
        return true;
    }

    public override InventoryItem ObtenerResultado() => new()
    {
        Codigo = EsNuevo ? Codigo.Trim() : _original.Codigo,
        Nombre = Nombre.Trim(),
        Categoria = Categoria.Trim(),
        Unidad = Unidad.Trim(),
        Ubicacion = Ubicacion.Trim(),
        StockActual = decimal.TryParse(StockActual, out var stock) ? stock : 0m,
        StockMinimo = decimal.TryParse(StockMinimo, out var minimo) ? minimo : 0m,
        CostoUnitario = decimal.TryParse(CostoUnitario, out var costo) ? costo : 0m
    };
}
