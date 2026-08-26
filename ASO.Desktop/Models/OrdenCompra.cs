using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de la orden de compra. <c>Borrador</c> es donde se completan los precios por línea;
/// <c>Aprobada</c> es a la vez la aprobación del gasto y la emisión al proveedor — no hace falta
/// un estado propio para "emitida", la entrega es un hecho externo al sistema. <c>Cerrada</c> es
/// el estado terminal, una vez que la recepción y la factura del proveedor cotejan contra ella
/// (ver <c>ComprasService.RevisarCotejo</c>, Fase 3).
///
/// Registrar la Recepción de mercancía (<see cref="RecepcionMercancia"/>) NO mueve el estado a
/// <c>Cerrada</c> por sí sola: se marca aparte, en <see cref="OrdenCompra.RecepcionMercanciaId"/>,
/// mismo criterio que <c>Remesa.FacturaClienteId</c> — un hecho de inventario no tiene por qué
/// inventarle un estado propio a la máquina de otro documento. <c>Cerrada</c> sigue reservada
/// para cuando exista el cotejo a tres vías con Cuentas por Pagar.
/// </summary>
public enum EstadoOrdenCompra
{
    Borrador,
    Aprobada,
    Cerrada,
    Anulada
}

/// <summary>
/// Orden de compra armada a partir de una <see cref="Requisicion"/> enviada y de la cotización
/// del proveedor elegido tras comparar precios (<see cref="CotizacionProveedor"/>). Autorizar el
/// gasto es aprobarla; nada se refleja en inventario hasta que la mercancía se reciba (fuera de
/// esta fase: ver Recepción de mercancía).
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class OrdenCompra : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public int RequisicionId { get; set; }

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;   // snapshot, el ganador de las cotizaciones

    public int CotizacionSeleccionadaId { get; set; }

    /// <summary>Monto total de la cotización ganadora, congelado al armar la orden. Sirve de
    /// referencia al completar el precio unitario de cada línea, y de único punto de comparación
    /// cuando la orden tiene varias líneas y el sistema no reparte el total entre ellas.</summary>
    public decimal MontoCotizado { get; set; }

    public List<OrdenCompraLinea> Lineas { get; set; } = [];

    public string Notas { get; set; } = string.Empty;

    public EstadoOrdenCompra Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    public int? AprobadoPorId { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    /// <summary>Recepción de mercancía activa de esta orden (Borrador o Confirmada); null si aún
    /// no se registró ninguna. Se limpia si esa recepción se anula — a diferencia de
    /// FacturaClienteId, anular existe aquí para corregir y volver a registrar, no para dejar la
    /// orden bloqueada para siempre. Mientras tenga valor, no se ofrece una segunda recepción.</summary>
    public int? RecepcionMercanciaId { get; set; }

    public bool TieneRecepcionActiva => RecepcionMercanciaId is not null;

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    /// <summary>
    /// Suma de las líneas, no un campo guardado: guardarlo aparte arriesgaría que se desincronice
    /// de lo que dicen las líneas si una se edita.
    /// </summary>
    public decimal MontoTotal => Lineas.Sum(l => l.Subtotal);

    public string MontoTotalTexto => MontoTotal.ToString("N2");

    public string MontoCotizadoTexto => MontoCotizado.ToString("N2");

    public string LineasTexto => Lineas.Count == 0
        ? "Sin líneas"
        : string.Join(" · ", Lineas.Select(l => $"{l.DestinoTexto} ({l.CantidadTexto})"));

    public string EstadoTexto => Estado switch
    {
        EstadoOrdenCompra.Borrador => "Borrador",
        EstadoOrdenCompra.Aprobada => "Aprobada",
        EstadoOrdenCompra.Cerrada => "Cerrada",
        _ => "Anulada"
    };

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public OrdenCompra Clonar()
    {
        var copia = (OrdenCompra)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Línea de una orden de compra: copia de una línea de la requisición de origen, más el precio
/// que se negoció. El precio lo llena a mano quien arma la orden, informado por la cotización
/// ganadora — el sistema no reparte un monto total entre líneas por su cuenta.
/// </summary>
public class OrdenCompraLinea
{
    public TipoInsumo TipoInsumo { get; set; }

    // --- Combustible ---
    public TipoCombustible? TipoCombustibleSolicitado { get; set; }
    public string? TipoLubricante { get; set; }    // grado/viscosidad, p. ej. "20W50"

    public int? MarcaLubricanteId { get; set; }
    public string MarcaLubricanteNombre { get; set; } = string.Empty;   // snapshot

    /// <summary>Mineral/Sintético/Semi-sintético. Lista cerrada: ver <see cref="Lubricante.Tipos"/>.</summary>
    public string? ClaseLubricante { get; set; }

    /// <summary>Envase en que viene. Lista cerrada: ver <see cref="Lubricante.Presentaciones"/>.</summary>
    public string? Presentacion { get; set; }

    // --- Repuesto ---
    public string? ArticuloCodigo { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty;    // snapshot
    public int? ActivoId { get; set; }
    public string ActivoEtiqueta { get; set; } = string.Empty;    // snapshot

    public decimal Cantidad { get; set; }
    public string UnidadTexto { get; set; } = string.Empty;

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    public string TipoInsumoTexto => TipoInsumo == TipoInsumo.Combustible ? "Combustible" : "Repuesto";

    public bool EsCombustible => TipoInsumo == TipoInsumo.Combustible;

    public bool EsDiesel => EsCombustible && TipoCombustibleSolicitado == TipoCombustible.Diesel;

    public bool EsLubricante => EsCombustible && TipoCombustibleSolicitado == TipoCombustible.Lubricante;

    /// <summary>Para el TextBox de Cantidad al armar la orden: en Diésel/Repuesto queda fija a lo
    /// pedido en la requisición; en Lubricante se puede ajustar (en litros) a lo que realmente
    /// vende el proveedor — p. ej. redondear a lo que rinde un número entero de envases.</summary>
    public bool CantidadSoloLectura => !EsLubricante;

    public string DestinoTexto => TipoInsumo == TipoInsumo.Combustible
        ? (TipoCombustibleSolicitado == TipoCombustible.Lubricante
            ? $"Lubricante {TipoLubricante}".TrimEnd()
            : "Diésel")
        : ArticuloNombre;

    public string UnidadDestinoTexto => TipoInsumo == TipoInsumo.Repuesto
        ? (string.IsNullOrWhiteSpace(ActivoEtiqueta) ? "Almacén / otro" : ActivoEtiqueta)
        : string.Empty;

    public string CantidadTexto => $"{Cantidad:N2} {UnidadTexto}".Trim();

    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N2");

    public string SubtotalTexto => Subtotal.ToString("N2");

    public OrdenCompraLinea Clonar() => (OrdenCompraLinea)MemberwiseClone();
}
