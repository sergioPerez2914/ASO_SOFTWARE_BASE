using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta rápida de un producto de stock de combustible, invocada desde el botón "+" de los
/// editores de vale y de recarga: no es una pantalla propia, solo evita que registrar un despacho
/// o una recarga se trabe porque el producto todavía no existe en el catálogo.
/// </summary>
public sealed class StockCombustibleEditorViewModel : CrudEditorViewModelBase<StockCombustible>
{
    public override string Titulo => "Nuevo stock de combustible";

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _capacidadTexto = string.Empty;
    public string CapacidadTexto
    {
        get => _capacidadTexto;
        set => SetProperty(ref _capacidadTexto, value);
    }

    private string _existenciaInicialTexto = "0";
    public string ExistenciaInicialTexto
    {
        get => _existenciaInicialTexto;
        set => SetProperty(ref _existenciaInicialTexto, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el producto (ej. Diesel, Aceite hidráulico).";
            return false;
        }

        if (!decimal.TryParse(CapacidadTexto, out var capacidad) || capacidad <= 0)
        {
            error = "La capacidad debe ser un número mayor que cero.";
            return false;
        }

        if (!decimal.TryParse(ExistenciaInicialTexto, out var existencia) || existencia < 0)
        {
            error = "La existencia inicial debe ser un número mayor o igual a cero.";
            return false;
        }

        if (existencia > capacidad)
        {
            error = "La existencia inicial no puede superar la capacidad.";
            return false;
        }

        error = null;
        return true;
    }

    public override StockCombustible ObtenerResultado() => new()
    {
        Nombre = Nombre.Trim(),
        CapacidadL = decimal.TryParse(CapacidadTexto, out var capacidad) ? capacidad : 0m,
        ExistenciaL = decimal.TryParse(ExistenciaInicialTexto, out var existencia) ? existencia : 0m,
        Activo = true
    };
}
