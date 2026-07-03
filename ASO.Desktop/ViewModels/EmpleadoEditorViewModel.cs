using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Editor de alta/edición de un Empleado.
/// </summary>
public class EmpleadoEditorViewModel : CrudEditorViewModelBase<Empleado>
{
    private readonly int _id;

    public EmpleadoEditorViewModel(Empleado original) : base(original)
    {
        _id = original.Id;
        _nombre = original.Nombre;
        _cedula = original.Cedula;
        _cargo = original.Cargo;
        _activo = original.Activo;
    }

    public bool EsNuevo => _id == 0;
    public override string Titulo => EsNuevo ? "Nuevo empleado" : "Editar empleado";

    private string _nombre;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _cedula;
    public string Cedula
    {
        get => _cedula;
        set => SetProperty(ref _cedula, value);
    }

    private string _cargo;
    public string Cargo
    {
        get => _cargo;
        set => SetProperty(ref _cargo, value);
    }

    private bool _activo;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "El nombre es obligatorio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Cedula))
        {
            error = "La cédula es obligatoria.";
            return false;
        }

        error = null;
        return true;
    }

    public override Empleado ObtenerResultado() => new()
    {
        Id = _id,
        Nombre = Nombre.Trim(),
        Cedula = Cedula.Trim(),
        Cargo = Cargo.Trim(),
        Activo = Activo
    };
}
