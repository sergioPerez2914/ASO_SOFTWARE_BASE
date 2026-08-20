using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de una factura de compra. No hay borrador: la factura la emite el proveedor, así que
/// cuando llega al centro ya existe y solo queda pagarla o rechazarla. "Vencida" es condición
/// derivada de la fecha (ver <see cref="FacturaProveedor.EstaVencida"/>), no un estado guardado.
/// </summary>
public enum EstadoFacturaProveedor
{
    Pendiente,
    Pagada,
    Anulada
}

/// <summary>
/// Factura de un proveedor: lo que el centro debe por repuestos, combustible o servicios.
///
/// PROVISIONAL: registro simple de la deuda y su vencimiento. Quedan pendientes de definición
/// del socio la orden de compra, las retenciones y la conciliación bancaria (el "bancos" que el
/// plan de fases menciona y que todavía no tiene submódulo).
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class FacturaProveedor : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    /// <summary>Número que trae el documento del proveedor, no un correlativo del centro.</summary>
    public string NumeroDocumento { get; set; } = string.Empty;

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;  // snapshot

    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }

    public decimal Monto { get; set; }

    public EstadoFacturaProveedor Estado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    /// <summary>Pendiente y con el plazo cumplido. Es condición derivada, no un estado guardado.</summary>
    public bool EstaVencida =>
        Estado == EstadoFacturaProveedor.Pendiente && FechaVencimiento.Date < DateTime.Today;

    /// <summary>Días de atraso; negativo mientras falte para el vencimiento.</summary>
    public int DiasParaVencer => (FechaVencimiento.Date - DateTime.Today).Days;

    public string EstadoTexto => EstaVencida
        ? "Vencida"
        : Estado switch
        {
            EstadoFacturaProveedor.Pendiente => "Pendiente",
            EstadoFacturaProveedor.Pagada => "Pagada",
            _ => "Anulada"
        };

    public string MontoTexto => Monto.ToString("N2");

    public string VencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy");

    public string PlazoTexto => Estado switch
    {
        EstadoFacturaProveedor.Pagada => FechaPago is { } pago ? $"Pagada el {pago:dd/MM/yyyy}" : "Pagada",
        EstadoFacturaProveedor.Anulada => "Anulada",
        _ when EstaVencida => $"Vencida hace {-DiasParaVencer} día(s)",
        _ => $"Vence en {DiasParaVencer} día(s)"
    };

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public FacturaProveedor Clonar() => (FacturaProveedor)MemberwiseClone();
}
