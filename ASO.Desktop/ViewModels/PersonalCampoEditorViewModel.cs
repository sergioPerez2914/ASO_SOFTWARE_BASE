using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de personal de campo. El núcleo (C.O.D) no se pregunta: toda la gente
/// pertenece al núcleo de la instalación, así que se estampa desde el ámbito.
/// </summary>
public sealed class PersonalCampoEditorViewModel : CrudEditorViewModelBase<PersonalCampo>
{
    private readonly PersonalCampo _original;
    private readonly IPersonalCampoDataSource _personal;

    public PersonalCampoEditorViewModel(PersonalCampo original,
                                        IPersonalCampoDataSource personal)
    {
        _original = original;
        _personal = personal;

        Nombre = original.Nombre;
        Cedula = original.Cedula;
        Rol = original.Rol;
        Activo = original.Activo;
    }

    public override string Titulo =>
        _original.Id == 0 ? "Nuevo personal de campo" : $"Editar personal Nº {_original.Id}";

    public IReadOnlyList<RolCampo> Roles { get; } = Enum.GetValues<RolCampo>();

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

    private RolCampo _rol;
    public RolCampo Rol
    {
        get => _rol;
        set => SetProperty(ref _rol, value);
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
            error = "Indique el nombre.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Cedula))
        {
            error = "Indique la cédula.";
            return false;
        }

        var repetida = _personal.GetAll()
            .Any(p => p.Id != _original.Id
                      && string.Equals(p.Cedula.Trim(), Cedula.Trim(), StringComparison.OrdinalIgnoreCase));

        if (repetida)
        {
            error = $"Ya existe personal de campo con la cédula {Cedula.Trim()}.";
            return false;
        }

        error = null;
        return true;
    }

    public override PersonalCampo ObtenerResultado() => new()
    {
        Id = _original.Id,
        Nombre = Nombre.Trim(),
        Cedula = Cedula.Trim(),
        Rol = Rol,
        // Todo el personal pertenece al núcleo de la instalación; se estampa su C.O.D en vez
        // de preguntarlo. Con esto se cierra la duda de si el chofer llevaba núcleo: lo lleva,
        // porque no hay otro.
        NucleoCodigo = Ambito.ExigirCodigoCam(),
        Activo = Activo
    };
}
