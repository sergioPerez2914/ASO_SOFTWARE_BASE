using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados del documento. <c>Borrador</c> es la ventana de corrección; al confirmar se
/// descuenta la cisterna, se actualiza la lectura del activo y el vale queda inmutable.
/// </summary>
public enum EstadoVale
{
    Borrador,
    Confirmado,
    Anulado
}

/// <summary>
/// Vale de combustible: documento de movimiento que despacha litros de una cisterna a un
/// activo y registra la lectura de su instrumento (horómetro en máquinas, odómetro en
/// transporte). Sigue el mismo patrón que la remesa: máquina de estados, inmutabilidad tras
/// confirmar, efectos en una sola operación, auditoría.
///
/// Al confirmar se calcula el consumo del período (litros por hora o por kilómetro recorrido
/// desde el vale anterior) y se compara con el promedio histórico del activo: si se dispara,
/// el vale queda marcado con alerta. Ese consumo se guarda dentro del documento, no se
/// recalcula al vuelo, porque es la foto de lo que pasó ese día.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class ValeCombustible : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    // --- Origen (snapshots: el documento debe leerse igual aunque el catálogo cambie) ---
    public int TanqueId { get; set; }
    public string TanqueNombre { get; set; } = string.Empty;

    // --- Destino ---
    public int ActivoId { get; set; }
    public string ActivoCodigo { get; set; } = string.Empty;
    public string ActivoEtiqueta { get; set; } = string.Empty;

    /// <summary>Snapshot: define si la lectura son kilómetros u horas, y cómo se lee el consumo.</summary>
    public bool EsTransporte { get; set; }

    public decimal Litros { get; set; }

    /// <summary>Lectura del instrumento al despachar: horómetro (h) u odómetro (km).</summary>
    public decimal? Lectura { get; set; }

    // PROVISIONAL: pasará a EmpleadoId cuando el padrón de empleados esté conectado a la operación.
    public string ResponsableNombre { get; set; } = string.Empty;

    // --- Rendimiento calculado al confirmar (copias, no se recalculan) ---
    public decimal? ConsumoPorUnidad { get; set; }
    public decimal? PromedioHistorico { get; set; }
    public bool AlertaConsumo { get; set; }

    // --- Documento ---
    public EstadoVale Estado { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Notas { get; set; } = string.Empty;
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public string EstadoTexto => Estado switch
    {
        EstadoVale.Borrador => "Borrador",
        EstadoVale.Confirmado => "Confirmado",
        _ => "Anulado"
    };

    public string UnidadLectura => EsTransporte ? "km" : "h";

    public string LitrosTexto => $"{Litros:N2} L";

    public string LecturaTexto => Lectura is { } lectura ? $"{lectura:N0} {UnidadLectura}" : "—";

    public string ConsumoTexto => ConsumoPorUnidad is { } consumo
        ? $"{consumo:N2} L/{UnidadLectura}"
        : "—";

    public string PromedioTexto => PromedioHistorico is { } promedio
        ? $"{promedio:N2} L/{UnidadLectura}"
        : "—";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public ValeCombustible Clonar() => (ValeCombustible)MemberwiseClone();
}
