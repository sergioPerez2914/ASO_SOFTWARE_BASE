using System;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un núcleo de productores. El código (C.O.D) es la clave que usan Personal
/// de Campo y la Remesa para atribuir el pago de corte, alza y transporte, así que no puede
/// repetirse.
/// </summary>
public sealed class NucleoEditorViewModel : CrudEditorViewModelBase<Nucleo>
{
    private readonly Nucleo _original;
    private readonly INucleoDataSource _nucleos;

    public NucleoEditorViewModel(Nucleo original, INucleoDataSource nucleos)
        : base(original)
    {
        _original = original;
        _nucleos = nucleos;

        Codigo = original.Codigo;
        Nombre = original.Nombre;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo núcleo" : $"Editar núcleo Nº {_original.Id}";

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

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Codigo))
        {
            error = "Indique el código (C.O.D) del núcleo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el nombre del núcleo.";
            return false;
        }

        var repetido = _nucleos.GetAll()
            .Any(n => n.Id != _original.Id
                      && string.Equals(n.Codigo.Trim(), Codigo.Trim(), StringComparison.OrdinalIgnoreCase));

        if (repetido)
        {
            error = $"Ya existe un núcleo con el código {Codigo.Trim()}.";
            return false;
        }

        error = null;
        return true;
    }

    public override Nucleo ObtenerResultado() => new()
    {
        Id = _original.Id,
        Codigo = Codigo.Trim(),
        Nombre = Nombre.Trim()
    };
}
