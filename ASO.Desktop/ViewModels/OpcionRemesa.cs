using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Opción de un combo de remesas, con una entrada sin remesa al frente de la lista.
///
/// Qué significa esa entrada depende de quién la use —"Sin vínculo" al vincular un mantenimiento,
/// "Todos los frentes" al filtrar horarios—, así que el rótulo se pasa al construirla en vez de
/// fijarlo aquí.
/// </summary>
public sealed class OpcionRemesa
{
    private readonly string _etiquetaSinRemesa;

    public OpcionRemesa(Remesa? remesa, string etiquetaSinRemesa = "Sin vínculo")
    {
        Remesa = remesa;
        _etiquetaSinRemesa = etiquetaSinRemesa;
    }

    public Remesa? Remesa { get; }

    public string Etiqueta => Remesa is { } r
        ? $"Nº {r.Id} · {r.InicioCarga:dd/MM} · {r.FincaNombre} ({r.VehiculoPlaca})"
        : _etiquetaSinRemesa;
}
