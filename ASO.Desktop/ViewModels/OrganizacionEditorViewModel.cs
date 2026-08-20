using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>Alta y edición de un núcleo.</summary>
public sealed class OrganizacionEditorViewModel : CrudEditorViewModelBase<Organizacion>
{
    private readonly Organizacion _original;

    public OrganizacionEditorViewModel(Organizacion original) : base(original)
    {
        _original = original;
        _codigo = original.Codigo;
        _nombre = original.Nombre;
        _activa = original.Id == 0 || original.Activa;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo núcleo" : $"Núcleo: {_original.Codigo}";

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

    private bool _activa;
    public bool Activa
    {
        get => _activa;
        set => SetProperty(ref _activa, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Codigo))
        {
            error = "Indique el código del núcleo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el nombre del núcleo.";
            return false;
        }

        error = null;
        return true;
    }

    public override Organizacion ObtenerResultado()
    {
        var resultado = _original.Clonar();
        resultado.Codigo = Codigo.Trim().ToUpperInvariant();
        resultado.Nombre = Nombre.Trim();
        resultado.Activa = Activa;
        return resultado;
    }
}
