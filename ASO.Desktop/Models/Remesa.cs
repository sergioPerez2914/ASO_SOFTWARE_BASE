using System;

namespace ASO.Desktop.Models;

/// <summary>Forma en que se cortó la caña, según el reglamento de remesas.</summary>
public enum TipoCosecha
{
    Manual,
    Mecanizada
}

/// <summary>
/// Estados del documento. <c>Borrador</c> es la ventana de corrección; al confirmar
/// la remesa queda inmutable; <c>Recibida</c> la marca personal del central al pesar la carga.
/// </summary>
public enum EstadoRemesa
{
    Borrador,
    Confirmada,
    Recibida,
    Anulada
}

/// <summary>
/// Remesa de caña: documento de movimiento que acompaña una carga desde el corte en la
/// finca hasta su entrega en el Central Azucarero Las Majaguas. Los campos siguen el
/// "Reglamento de llenado de la Remesa de caña" (ver <c>docs/</c>).
///
/// Los datos de catálogo se guardan por Id <b>y</b> como texto: una remesa confirmada es un
/// documento y debe conservar lo que decía el papel aunque el catálogo cambie después.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class Remesa : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    // --- Ubicación de la cosecha ---
    public int FincaId { get; set; }
    public string FincaCodigoCam { get; set; } = string.Empty;
    public string FincaNombre { get; set; } = string.Empty;
    public string LoteNombre { get; set; } = string.Empty;
    public string TablonNombre { get; set; } = string.Empty;
    public TipoCosecha TipoCosecha { get; set; }

    // --- Personal y vehículo (C.O.D = código del núcleo en el sistema del CAM) ---
    public int OperadorId { get; set; }
    public string OperadorNombre { get; set; } = string.Empty;
    public string OperadorNucleoCodigo { get; set; } = string.Empty;

    public int TractoristaId { get; set; }
    public string TractoristaNombre { get; set; } = string.Empty;
    public string TractoristaNucleoCodigo { get; set; } = string.Empty;

    public int ChoferId { get; set; }
    public string ChoferNombre { get; set; } = string.Empty;

    public int VehiculoId { get; set; }
    public string VehiculoPlaca { get; set; } = string.Empty;

    public int RemeseroId { get; set; }
    public string RemeseroNombre { get; set; } = string.Empty;

    // --- Núcleos que determinan el pago ---
    public string NucleoCorteCodigo { get; set; } = string.Empty;
    public string NucleoAlzaEmpujeCodigo { get; set; } = string.Empty;
    public string NucleoTransporteCodigo { get; set; } = string.Empty;

    // --- Tiempos ---
    public DateTime InicioCarga { get; set; }
    public DateTime FinCarga { get; set; }

    /// <summary>La llena personal del CAM en la Pre-Romana, no quien registra la remesa.</summary>
    public DateTime? LlegadaCentral { get; set; }

    // --- Pesaje en la romana del central (toneladas) ---
    public decimal? PesoBrutoT { get; set; }
    public decimal? TaraT { get; set; }
    public decimal? PesoNetoT => PesoBrutoT - TaraT;

    // --- Documento ---
    public EstadoRemesa Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    /// <summary>Cuándo se firmó cada decisión sobre el documento (auditoría y seguimiento).</summary>
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    /// <summary>
    /// Factura de Cuentas por Cobrar que incluyó esta remesa. Va en un campo aparte y no como
    /// un valor más de <see cref="Estado"/> a propósito: "Recibida" sigue siendo el estado
    /// terminal de la operación, y facturar es un hecho de Finanzas que no debe alterar la
    /// máquina de estados del documento de campo. Además, así la bandera sirve de control
    /// antifacturación doble: si tiene factura, no vuelve a ofrecerse para facturar.
    /// </summary>
    public int? FacturaClienteId { get; set; }

    public bool Facturada => FacturaClienteId is not null;

    public string EstadoTexto => Estado switch
    {
        EstadoRemesa.Borrador => "Borrador",
        EstadoRemesa.Confirmada => "Confirmada",
        EstadoRemesa.Recibida => "Recibida",
        _ => "Anulada"
    };

    public string TipoCosechaTexto => TipoCosecha == TipoCosecha.Manual ? "Manual" : "Mecanizada";

    public string UbicacionTexto => $"{LoteNombre} · {TablonNombre}";

    public string FacturadaTexto => FacturaClienteId is { } id ? $"FC-{id:D4}" : "No facturada";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public Remesa Clonar() => (Remesa)MemberwiseClone();
}
