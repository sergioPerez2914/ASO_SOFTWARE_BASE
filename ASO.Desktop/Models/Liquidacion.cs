using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de la liquidación. <c>Borrador</c> admite ajustes por conceptos; al cerrar queda
/// inmutable y lista para pago.
/// </summary>
public enum EstadoLiquidacion
{
    Borrador,
    Cerrada,
    Pagada,
    Anulada
}

/// <summary>
/// A quién se le liquida: al núcleo (por las toneladas que cortó, alzó o transportó) o al
/// empleado del centro (por las horas trabajadas).
/// </summary>
public enum SujetoLiquidacion
{
    Nucleo,
    Empleado
}

/// <summary>De dónde salió la línea: calculada por el sistema o agregada a mano.</summary>
public enum OrigenLinea
{
    Destajo,
    Horas,
    Concepto
}

/// <summary>
/// Liquidación de un período: cabecera con el sujeto y el rango, y líneas con lo devengado y lo
/// deducido. El neto es devengos menos deducciones.
///
/// Guarda los Ids de las remesas que ya computó (<see cref="RemesaIdsIncluidas"/>) porque esa
/// lista es lo que impide pagar dos veces la misma tonelada: al generar una liquidación nueva se
/// descartan las remesas que ya estén en otra liquidación no anulada del mismo sujeto.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class Liquidacion : IEntidad<int>
{
    public int Id { get; set; }

    public SujetoLiquidacion SujetoTipo { get; set; }

    /// <summary>Código del núcleo (C.O.D) o Id del empleado en texto, según el tipo de sujeto.</summary>
    public string SujetoCodigo { get; set; } = string.Empty;
    public string SujetoNombre { get; set; } = string.Empty;  // snapshot

    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    public List<LiquidacionLinea> Lineas { get; set; } = [];

    /// <summary>Remesas ya computadas aquí: evita liquidar dos veces la misma tonelada.</summary>
    public List<int> RemesaIdsIncluidas { get; set; } = [];

    public EstadoLiquidacion Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaCierre { get; set; }
    public DateTime? FechaPago { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public decimal TotalDevengos => Lineas.Where(l => !l.EsDeduccion).Sum(l => l.Monto);

    public decimal TotalDeducciones => Lineas.Where(l => l.EsDeduccion).Sum(l => l.Monto);

    public decimal Neto => TotalDevengos - TotalDeducciones;

    public string NetoTexto => Neto.ToString("N2");

    public string PeriodoTexto => $"{PeriodoDesde:dd/MM/yyyy} – {PeriodoHasta:dd/MM/yyyy}";

    public string SujetoTipoTexto => SujetoTipo == SujetoLiquidacion.Nucleo ? "Núcleo" : "Empleado";

    public string SujetoTexto => string.IsNullOrWhiteSpace(SujetoCodigo)
        ? SujetoNombre
        : $"{SujetoCodigo} · {SujetoNombre}";

    public string EstadoTexto => Estado switch
    {
        EstadoLiquidacion.Borrador => "Borrador",
        EstadoLiquidacion.Cerrada => "Cerrada",
        EstadoLiquidacion.Pagada => "Pagada",
        _ => "Anulada"
    };

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public Liquidacion Clonar()
    {
        var copia = (Liquidacion)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        copia.RemesaIdsIncluidas = [.. RemesaIdsIncluidas];
        return copia;
    }
}

/// <summary>
/// Línea de una liquidación. <see cref="TarifaMonto"/> es copia de la tarifa aplicada, no una
/// referencia: si el tarifario cambia mañana, esta liquidación debe seguir diciendo lo mismo.
/// </summary>
public class LiquidacionLinea
{
    public string Concepto { get; set; } = string.Empty;
    public OrigenLinea Origen { get; set; }

    public decimal Cantidad { get; set; }
    public string UnidadTexto { get; set; } = string.Empty;   // "t", "h", "—"

    public decimal? TarifaMonto { get; set; }
    public decimal Monto { get; set; }

    public bool EsDeduccion { get; set; }

    public string OrigenTexto => Origen switch
    {
        OrigenLinea.Destajo => "Destajo",
        OrigenLinea.Horas => "Horas",
        _ => "Concepto"
    };

    public string CantidadTexto => Cantidad == 0 ? "—" : $"{Cantidad:N2} {UnidadTexto}".Trim();

    public string TarifaTexto => TarifaMonto is { } tarifa ? tarifa.ToString("N2") : "—";

    public string MontoTexto => EsDeduccion ? $"-{Monto:N2}" : Monto.ToString("N2");

    public LiquidacionLinea Clonar() => (LiquidacionLinea)MemberwiseClone();
}
