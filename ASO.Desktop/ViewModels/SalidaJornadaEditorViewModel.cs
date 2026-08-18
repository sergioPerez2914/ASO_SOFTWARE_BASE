using System;
using System.Globalization;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Cierre de una jornada abierta. No hereda de la base genérica porque no reconstruye la
/// entidad: solo captura la hora de salida que el servicio aplicará.
/// </summary>
public sealed class SalidaJornadaEditorViewModel : CrudEditorViewModelBase
{
    private readonly JornadaTrabajo _jornada;

    public SalidaJornadaEditorViewModel(JornadaTrabajo jornada)
    {
        _jornada = jornada;
        FechaSalida = DateTime.Today;
        HoraSalida = DateTime.Now.ToString("HH:mm");
    }

    public override string Titulo => $"Registrar salida — {_jornada.PersonaNombre}";

    public string Resumen =>
        $"{_jornada.CargoORol} · turno {_jornada.TurnoTexto.ToLowerInvariant()} · entrada {_jornada.EntradaTexto}";

    private DateTime _fechaSalida = DateTime.Today;
    public DateTime FechaSalida
    {
        get => _fechaSalida;
        set => SetProperty(ref _fechaSalida, value);
    }

    private string _horaSalida = string.Empty;
    public string HoraSalida
    {
        get => _horaSalida;
        set => SetProperty(ref _horaSalida, value);
    }

    /// <summary>Momento de salida ya compuesto, para pasárselo al servicio.</summary>
    public DateTime Salida =>
        FechaSalida.Date + (TryLeerHora(out var hora) ? hora : TimeSpan.Zero);

    protected override bool Validar(out string? error)
    {
        if (!TryLeerHora(out _))
        {
            error = "La hora de salida debe tener el formato HH:mm (por ejemplo, 17:30).";
            return false;
        }

        if (Salida <= _jornada.HoraEntrada)
        {
            error = $"La salida debe ser posterior a la entrada ({_jornada.EntradaTexto}). " +
                    "Si el turno cruzó la medianoche, ajuste también la fecha.";
            return false;
        }

        error = null;
        return true;
    }

    private bool TryLeerHora(out TimeSpan hora) =>
        TimeSpan.TryParseExact(HoraSalida?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out hora);
}
