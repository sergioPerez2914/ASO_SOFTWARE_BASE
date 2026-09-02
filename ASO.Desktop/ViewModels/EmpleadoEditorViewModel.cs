using System;
using System.Collections.ObjectModel;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un empleado del centro (padrón administrativo y de taller).
/// La cédula identifica a la persona ante el CAM, así que se valida que no se repita.
/// </summary>
public sealed class EmpleadoEditorViewModel : CrudEditorViewModelBase<Empleado>
{
    private readonly Empleado _original;
    private readonly IEmpleadoDataSource _empleados;

    public EmpleadoEditorViewModel(Empleado original, IEmpleadoDataSource empleados)
    {
        _original = original;
        _empleados = empleados;

        Nombre = original.Nombre;
        Cedula = original.Cedula;
        Cargo = original.Cargo;
        Activo = original.Activo;

        CargosExistentes = new ObservableCollection<string>(
            empleados.GetAll()
                .Select(e => e.Cargo.Trim().ToUpperInvariant())
                .Where(c => c.Length > 0)
                .Distinct()
                .OrderBy(c => c, StringComparer.Ordinal));
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo empleado" : $"Editar empleado Nº {_original.Id}";

    public ObservableCollection<string> CargosExistentes { get; }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _cedula = string.Empty;
    public string Cedula
    {
        get => _cedula;
        set => SetProperty(ref _cedula, value);
    }

    private string _cargo = string.Empty;
    public string Cargo
    {
        get => _cargo;
        set => SetProperty(ref _cargo, value);
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
            error = "Indique el nombre del empleado.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Cedula))
        {
            error = "Indique la cédula del empleado.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Cargo))
        {
            error = "Indique el cargo del empleado.";
            return false;
        }

        var repetida = _empleados.GetAll()
            .Any(e => e.Id != _original.Id
                      && string.Equals(e.Cedula.Trim(), Cedula.Trim(), StringComparison.OrdinalIgnoreCase));

        if (repetida)
        {
            error = $"Ya existe un empleado con la cédula {Cedula.Trim()}.";
            return false;
        }

        error = null;
        return true;
    }

    public override Empleado ObtenerResultado() => new()
    {
        Id = _original.Id,
        Nombre = Nombre.Trim(),
        Cedula = Cedula.Trim(),
        Cargo = Cargo.Trim().ToUpperInvariant(),
        Activo = Activo
    };
}
