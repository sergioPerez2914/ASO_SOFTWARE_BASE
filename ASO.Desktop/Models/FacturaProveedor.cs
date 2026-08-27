using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de una factura de compra. La factura tecleada a mano no tiene borrador: la emite el
/// proveedor, así que cuando llega al centro ya existe y solo queda pagarla o rechazarla.
/// <c>Borrador</c> es la excepción, y solo para la que genera el sistema al confirmar una
/// <see cref="RecepcionMercancia"/> (ver <see cref="OrdenCompraId"/>): nace con proveedor, líneas
/// y monto ya llenos, pero sin Nº de documento ni vencimiento — esos los trae el papel del
/// proveedor y el sistema no puede inventarlos. "Vencida" es condición derivada de la fecha (ver
/// <see cref="FacturaProveedor.EstaVencida"/>), no un estado guardado.
/// </summary>
public enum EstadoFacturaProveedor
{
    Pendiente,
    Pagada,
    Anulada,
    Borrador
}

/// <summary>
/// Factura de un proveedor: lo que el centro debe por repuestos, combustible o servicios.
///
/// PROVISIONAL: registro simple de la deuda y su vencimiento. Quedan pendientes de definición
/// del socio las retenciones y la conciliación bancaria (el "bancos" que el plan de fases
/// menciona y que todavía no tiene submódulo).
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class FacturaProveedor : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    /// <summary>Número que trae el documento del proveedor, no un correlativo del centro. Vacío
    /// mientras la factura esté en <see cref="EstadoFacturaProveedor.Borrador"/>.</summary>
    public string NumeroDocumento { get; set; } = string.Empty;

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;  // snapshot

    /// <summary>Orden de compra de origen, si esta factura nació al confirmar su recepción de
    /// mercancía. Null en una factura tecleada a mano desde Cuentas por Pagar.</summary>
    public int? OrdenCompraId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; }

    /// <summary>Nulo mientras la factura esté en Borrador: se completa junto con
    /// <see cref="NumeroDocumento"/> al confirmarla en Cuentas por Pagar.</summary>
    public DateTime? FechaVencimiento { get; set; }

    public decimal Monto { get; set; }

    /// <summary>Detalle de lo que se compró y su precio — snapshot de texto, no mueve stock (eso
    /// ya ocurrió al confirmar la recepción). Vacía en una factura tecleada a mano.</summary>
    public List<FacturaProveedorLinea> Lineas { get; set; } = [];

    public EstadoFacturaProveedor Estado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    /// <summary>Pendiente, con vencimiento definido y con el plazo cumplido. Es condición
    /// derivada, no un estado guardado.</summary>
    public bool EstaVencida =>
        Estado == EstadoFacturaProveedor.Pendiente
        && FechaVencimiento is { } vencimiento
        && vencimiento.Date < DateTime.Today;

    /// <summary>Días de atraso; negativo mientras falte para el vencimiento. 0 si todavía no hay
    /// vencimiento definido (Borrador).</summary>
    public int DiasParaVencer => FechaVencimiento is { } vencimiento
        ? (vencimiento.Date - DateTime.Today).Days
        : 0;

    public string EstadoTexto => EstaVencida
        ? "Vencida"
        : Estado switch
        {
            EstadoFacturaProveedor.Pendiente => "Pendiente",
            EstadoFacturaProveedor.Pagada => "Pagada",
            EstadoFacturaProveedor.Borrador => "Borrador",
            _ => "Anulada"
        };

    public string MontoTexto => Monto.ToString("N2");

    public string VencimientoTexto => FechaVencimiento is { } vencimiento
        ? vencimiento.ToString("dd/MM/yyyy")
        : "Sin definir";

    public string PlazoTexto => Estado switch
    {
        EstadoFacturaProveedor.Pagada => FechaPago is { } pago ? $"Pagada el {pago:dd/MM/yyyy}" : "Pagada",
        EstadoFacturaProveedor.Anulada => "Anulada",
        EstadoFacturaProveedor.Borrador => "Falta Nº de documento y vencimiento",
        _ when EstaVencida => $"Vencida hace {-DiasParaVencer} día(s)",
        _ => $"Vence en {DiasParaVencer} día(s)"
    };

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public FacturaProveedor Clonar()
    {
        var copia = (FacturaProveedor)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Línea de una factura de proveedor generada desde Compras: snapshot de lo que decía la Orden
/// de Compra (destino, cantidad y precio), sin los campos de tipo/artículo que sí necesita
/// <see cref="OrdenCompraLinea"/> para mover stock — aquí el stock ya se movió en la Recepción,
/// esto es solo lo que hay que mostrar en la factura.
/// </summary>
public class FacturaProveedorLinea
{
    public string DestinoTexto { get; set; } = string.Empty;
    public string CantidadTexto { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N2");
    public string SubtotalTexto => Subtotal.ToString("N2");

    public FacturaProveedorLinea Clonar() => (FacturaProveedorLinea)MemberwiseClone();
}
