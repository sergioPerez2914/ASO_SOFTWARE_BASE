using System;
using System.Globalization;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta de una remesa. Solo pregunta cuándo empezó la carga, que es lo único que se sabe con
/// certeza en ese momento: la finca, el lote, quién opera, quién conduce y con qué placa se
/// conocen después, y se completan con "Editar" antes de confirmar.
///
/// El resto del formulario sigue viviendo en <see cref="RemesaEditorViewModel"/>. La normativa
/// ("todos los datos de la remesa deben ser llenados") se sigue cumpliendo donde toca:
/// <see cref="Services.RemesaService.EstaCompleta"/> impide confirmar un documento a medias y
/// enumera exactamente lo que falta.
/// </summary>
public sealed class NuevaRemesaEditorViewModel : CrudEditorViewModelBase<Remesa>
{
    private const string FormatoHora = @"hh\:mm";

    private readonly Remesa _original;
    private DateTime _inicio;

    public NuevaRemesaEditorViewModel(Remesa original)
    {
        _original = original;
        InicioFecha = DateTime.Today;
        InicioHora = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    public override string Titulo => "Nueva remesa";
    public override string TextoAccion => "Crear la remesa";

    private DateTime? _inicioFecha;
    public DateTime? InicioFecha
    {
        get => _inicioFecha;
        set => SetProperty(ref _inicioFecha, value);
    }

    private string _inicioHora = string.Empty;
    public string InicioHora
    {
        get => _inicioHora;
        set => SetProperty(ref _inicioHora, value);
    }

    protected override bool Validar(out string? error)
    {
        if (InicioFecha is null ||
            !TimeSpan.TryParseExact(InicioHora?.Trim(), FormatoHora, CultureInfo.InvariantCulture, out var hora))
        {
            error = "Indique la fecha y la hora de inicio de carga (formato HH:mm).";
            return false;
        }

        _inicio = InicioFecha.Value.Date + hora;
        error = null;
        return true;
    }

    public override Remesa ObtenerResultado()
    {
        var copia = _original.Clonar();
        copia.InicioCarga = _inicio;
        return copia;
    }
}
