namespace ASO.Desktop.Models;

/// <summary>
/// Regla de revisión periódica por tipo de activo: "cada X horas de uso" y/o "cada Y días".
/// Si define ambos intervalos rige el que se cumpla primero. Las reglas de transporte son por
/// días; el kilometraje se incorporará cuando Telemetría alimente el odómetro con regularidad.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class ReglaMantenimiento : IEntidad<int>
{
    public int Id { get; set; }
    public TipoActivo Tipo { get; set; }
    public string Revision { get; set; } = string.Empty;
    public decimal? IntervaloHoras { get; set; }
    public int? IntervaloDias { get; set; }

    public string IntervaloTexto => (IntervaloHoras, IntervaloDias) switch
    {
        ({ } h, { } d) => $"cada {h:N0} h o {d} días",
        ({ } h, null) => $"cada {h:N0} h",
        (null, { } d) => $"cada {d} días",
        _ => "—"
    };
}

/// <summary>Urgencia de una recomendación. El orden de declaración ES la prioridad.</summary>
public enum EstadoRecomendacion
{
    Vencido,
    Proximo,
    AlDia
}

/// <summary>
/// Resultado del cálculo de una regla contra el historial de un activo: cuánto del intervalo se
/// consumió y en qué estado queda la revisión.
/// </summary>
public sealed class RecomendacionMantenimiento
{
    public required ActivoFlota Activo { get; init; }
    public required ReglaMantenimiento Regla { get; init; }
    public required EstadoRecomendacion Estado { get; init; }

    /// <summary>Explicación legible, p. ej. "Hace 330 h del último cambio de aceite (intervalo 300 h)".</summary>
    public required string Detalle { get; init; }

    /// <summary>Fracción del intervalo consumida (1 = vencida). Desempata el orden de prioridad.</summary>
    public required decimal Avance { get; init; }

    public string EstadoTexto => Estado switch
    {
        EstadoRecomendacion.Vencido => "Vencido",
        EstadoRecomendacion.Proximo => "Próximo",
        _ => "Al día"
    };
}
