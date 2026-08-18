using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de personal de campo. El núcleo (C.O.D) es el que determina el pago de
/// corte, alza y transporte en la remesa, por eso se elige del catálogo y no se escribe a mano.
/// </summary>
public sealed class PersonalCampoEditorViewModel : CrudEditorViewModelBase<PersonalCampo>
{
    private readonly PersonalCampo _original;
    private readonly IPersonalCampoDataSource _personal;

    public PersonalCampoEditorViewModel(PersonalCampo original,
                                        IPersonalCampoDataSource personal,
                                        INucleoDataSource nucleos)
        : base(original)
    {
        _original = original;
        _personal = personal;

        Nucleos = nucleos.GetAll().ToList();

        Nombre = original.Nombre;
        Cedula = original.Cedula;
        Rol = original.Rol;
        Activo = original.Activo;
        NucleoSeleccionado = Nucleos.FirstOrDefault(n => n.Codigo == original.NucleoCodigo);
    }

    public override string Titulo =>
        _original.Id == 0 ? "Nuevo personal de campo" : $"Editar personal Nº {_original.Id}";

    public IReadOnlyList<Nucleo> Nucleos { get; }
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
        set
        {
            if (SetProperty(ref _rol, value))
                OnPropertyChanged(nameof(NucleoObligatorio));
        }
    }

    private Nucleo? _nucleoSeleccionado;
    public Nucleo? NucleoSeleccionado
    {
        get => _nucleoSeleccionado;
        set => SetProperty(ref _nucleoSeleccionado, value);
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    /// <summary>
    /// El chofer es el único que la remesa no asocia a un C.O.D.
    /// PROVISIONAL: pendiente de confirmar con el socio si el chofer también lleva núcleo.
    /// </summary>
    public bool NucleoObligatorio => Rol != RolCampo.Chofer;

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

        if (NucleoObligatorio && NucleoSeleccionado is null)
        {
            error = "Seleccione el núcleo (C.O.D) al que pertenece.";
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
        NucleoCodigo = NucleoObligatorio ? NucleoSeleccionado?.Codigo ?? string.Empty : string.Empty,
        Activo = Activo
    };
}
