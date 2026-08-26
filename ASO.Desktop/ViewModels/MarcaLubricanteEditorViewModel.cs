using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta rápida de una marca de lubricante. Sin pantalla de administración propia: se llega
/// aquí solo por el botón "+ Nuevo" desde donde se elige la marca (Orden de Compra, editor de
/// <see cref="Lubricante"/>), mismo patrón que <see cref="StockCombustibleEditorViewModel"/>.
/// </summary>
public sealed class MarcaLubricanteEditorViewModel : CrudEditorViewModelBase<MarcaLubricante>
{
    private readonly MarcaLubricante _original;

    public MarcaLubricanteEditorViewModel() : this(new MarcaLubricante { Activo = true })
    {
    }

    public MarcaLubricanteEditorViewModel(MarcaLubricante original)
    {
        _original = original;
        Nombre = original.Nombre;
        Activo = original.Id == 0 || original.Activo;
    }

    public override string Titulo => _original.Id == 0 ? "Nueva marca de lubricante" : $"Editar {_original.Nombre}";

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el nombre de la marca.";
            return false;
        }

        error = null;
        return true;
    }

    public override MarcaLubricante ObtenerResultado()
    {
        var marca = _original.Clonar();
        marca.Nombre = Nombre.Trim();
        marca.Activo = Activo;
        return marca;
    }
}
