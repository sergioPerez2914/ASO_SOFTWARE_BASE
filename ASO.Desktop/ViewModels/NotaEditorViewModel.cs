using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta de una nota en el seguimiento de una remesa. Es el único evento que el usuario crea a
/// mano; los demás los publican los módulos del sistema.
/// </summary>
public sealed class NotaEditorViewModel : CrudEditorViewModelBase
{
    private readonly Remesa _remesa;

    public NotaEditorViewModel(Remesa remesa) => _remesa = remesa;

    public override string Titulo => $"Agregar nota — Remesa Nº {_remesa.Id}";
    public override string TextoAccion => "Agregar la nota";

    public string ResumenRemesa =>
        $"{_remesa.FincaCodigoCam} · {_remesa.FincaNombre} — {_remesa.UbicacionTexto} — Placa {_remesa.VehiculoPlaca}";

    private string _texto = string.Empty;
    public string Texto
    {
        get => _texto;
        set => SetProperty(ref _texto, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Texto))
        {
            error = "Escriba el contenido de la nota.";
            return false;
        }

        error = null;
        return true;
    }
}
