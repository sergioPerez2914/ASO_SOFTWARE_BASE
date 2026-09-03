using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de la zafra. <c>Abierta</c> es la temporada en curso: a lo sumo una por núcleo a la
/// vez (lo exige <c>ZafraService.Abrir</c>). <c>Cerrada</c> es el archivo — sigue existiendo
/// para que sus documentos históricos puedan filtrarse por ella, pero ya no recibe nuevos.
/// </summary>
public enum EstadoZafra
{
    Abierta,
    Cerrada
}

/// <summary>
/// Temporada de cosecha. Es el catálogo que le da sentido temporal a los reportes: una vez que
/// los documentos lleven <c>ZafraId</c> (fase posterior, pendiente de confirmar con el socio),
/// todo saldo y liquidación se acota a la zafra activa en vez de mezclar toda la historia de la
/// instalación.
///
/// A diferencia de <see cref="Organizacion"/> (fija de por vida de la instalación), una zafra es
/// de uno a muchos dentro del mismo núcleo: se abre, corre la temporada, se cierra, y se abre la
/// siguiente. Por eso implementa <see cref="IDeOrganizacion"/> como <see cref="Tarifa"/> — cada
/// núcleo lleva su propio calendario — y no es el ámbito de sesión: ver <c>Services/ZafraActiva.cs</c>.
/// </summary>
public class Zafra : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    /// <summary>Código corto de la temporada, p. ej. "2026-2027".</summary>
    public string Codigo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; } = DateTime.Today;

    /// <summary>Estimado, informativo — no dispara nada. El cierre real lo marca <see cref="FechaCierre"/>.</summary>
    public DateTime? FechaFinPrevista { get; set; }

    public EstadoZafra Estado { get; set; } = EstadoZafra.Abierta;

    public DateTime? FechaCierre { get; set; }
    public string? MotivoCierre { get; set; }

    public string Notas { get; set; } = string.Empty;

    public string EstadoTexto => Estado == EstadoZafra.Abierta ? "Abierta" : "Cerrada";

    public string VigenciaTexto => FechaFinPrevista is { } f
        ? $"{FechaInicio:dd/MM/yyyy} – {f:dd/MM/yyyy}"
        : $"desde {FechaInicio:dd/MM/yyyy}";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public Zafra Clonar() => (Zafra)MemberwiseClone();
}
