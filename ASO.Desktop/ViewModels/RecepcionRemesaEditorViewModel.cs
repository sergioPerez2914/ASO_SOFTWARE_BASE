using System;
using System.Globalization;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Recepción de la carga en el central: llegada y pesaje en la romana. Según el reglamento
/// este paso lo hace personal del CAM en la Pre-Romana, no quien registró la remesa en el campo.
///
/// Hereda de la base no genérica porque no reconstruye la entidad: expone los valores
/// capturados y la transición la aplica <see cref="Services.RemesaService"/>.
/// </summary>
public sealed class RecepcionRemesaEditorViewModel : CrudEditorViewModelBase
{
    private const string FormatoHora = @"hh\:mm";

    private readonly Remesa _remesa;

    public RecepcionRemesaEditorViewModel(Remesa remesa)
    {
        _remesa = remesa;
        LlegadaFecha = remesa.FinCarga.Date;
    }

    public override string Titulo => $"Registrar recepción · Remesa Nº {_remesa.Id}";
    public override string TextoAccion => "Registrar la recepción";

    public string ResumenRemesa =>
        $"{_remesa.FincaCodigoCam} · {_remesa.FincaNombre} — {_remesa.UbicacionTexto} — Placa {_remesa.VehiculoPlaca}";

    public string FinCargaTexto => $"Fin de carga: {_remesa.FinCarga:dd/MM/yyyy HH:mm}";

    private DateTime? _llegadaFecha;
    public DateTime? LlegadaFecha
    {
        get => _llegadaFecha;
        set => SetProperty(ref _llegadaFecha, value);
    }

    private string _llegadaHora = string.Empty;
    public string LlegadaHora
    {
        get => _llegadaHora;
        set => SetProperty(ref _llegadaHora, value);
    }

    private string _pesoBrutoTexto = string.Empty;
    public string PesoBrutoTexto
    {
        get => _pesoBrutoTexto;
        set
        {
            if (SetProperty(ref _pesoBrutoTexto, value))
                OnPropertyChanged(nameof(PesoNetoTexto));
        }
    }

    private string _taraTexto = string.Empty;
    public string TaraTexto
    {
        get => _taraTexto;
        set
        {
            if (SetProperty(ref _taraTexto, value))
                OnPropertyChanged(nameof(PesoNetoTexto));
        }
    }

    /// <summary>Neto calculado en vivo mientras se teclea; "—" mientras no haya dos números válidos.</summary>
    public string PesoNetoTexto =>
        TryPeso(PesoBrutoTexto, out var bruto) && TryPeso(TaraTexto, out var tara)
            ? (bruto - tara).ToString("N2", CultureInfo.CurrentCulture)
            : "—";

    // Valores ya parseados, para que el ViewModel de la lista no vuelva a interpretar texto.
    public DateTime Llegada { get; private set; }
    public decimal PesoBrutoT { get; private set; }
    public decimal TaraT { get; private set; }

    private static bool TryPeso(string texto, out decimal valor)
        => decimal.TryParse(texto?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out valor);

    protected override bool Validar(out string? error)
    {
        if (LlegadaFecha is null ||
            !TimeSpan.TryParseExact(LlegadaHora?.Trim(), FormatoHora, CultureInfo.InvariantCulture, out var hora))
        {
            error = "Indique la fecha y la hora de llegada al central (formato HH:mm).";
            return false;
        }

        var llegada = LlegadaFecha.Value.Date + hora;
        if (llegada < _remesa.FinCarga)
        {
            error = "La llegada al central no puede ser anterior al fin de carga.";
            return false;
        }

        if (!TryPeso(PesoBrutoTexto, out var bruto) || !TryPeso(TaraTexto, out var tara))
        {
            error = "El peso bruto y la tara deben ser números en toneladas.";
            return false;
        }

        if (tara <= 0)
        {
            error = "La tara debe ser mayor que cero.";
            return false;
        }

        if (bruto <= tara)
        {
            error = "El peso bruto debe ser mayor que la tara.";
            return false;
        }

        Llegada = llegada;
        PesoBrutoT = bruto;
        TaraT = tara;

        error = null;
        return true;
    }
}
