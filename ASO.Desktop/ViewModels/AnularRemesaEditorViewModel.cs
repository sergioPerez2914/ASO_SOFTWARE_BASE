using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Anulación de una remesa. Exige motivo: toda decisión sobre un documento queda registrada
/// con su comentario (capa de auditoría del diseño de autorización).
/// </summary>
public sealed class AnularRemesaEditorViewModel : CrudEditorViewModelBase
{
    private readonly Remesa _remesa;

    public AnularRemesaEditorViewModel(Remesa remesa) => _remesa = remesa;

    public override string Titulo => $"Anular remesa Nº {_remesa.Id}";
    public override string TextoAccion => "Anular la remesa";

    public string ResumenRemesa =>
        $"{_remesa.FincaCodigoCam} · {_remesa.FincaNombre} — {_remesa.UbicacionTexto} — Placa {_remesa.VehiculoPlaca}";

    private string _motivo = string.Empty;
    public string Motivo
    {
        get => _motivo;
        set => SetProperty(ref _motivo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Motivo))
        {
            error = "Indique el motivo de la anulación.";
            return false;
        }

        error = null;
        return true;
    }
}
