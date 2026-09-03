using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de la recepción de mercancía. <c>Borrador</c> nace con la cantidad recibida
/// prellenada igual a la pedida, a corregir si hubo faltante o sobrante real del proveedor;
/// <c>Confirmada</c> es cuando el stock ya se movió y el documento queda inmutable.
/// </summary>
public enum EstadoRecepcionMercancia
{
    Borrador,
    Confirmada,
    Anulada
}

/// <summary>
/// Recepción de la mercancía de una orden de compra aprobada: "la carta de recibimiento" que
/// de verdad mueve <see cref="InventoryItem.StockActual"/> o <see cref="StockCombustible.ExistenciaL"/>,
/// según lo REALMENTE recibido y no lo pedido — el camión trae lo que trae. Una orden de compra
/// admite una sola recepción activa a la vez (<see cref="OrdenCompra.RecepcionMercanciaId"/>).
/// </summary>
public class RecepcionMercancia : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public int OrdenCompraId { get; set; }

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;   // snapshot, de la orden de compra

    public List<RecepcionMercanciaLinea> Lineas { get; set; } = [];

    /// <summary>Quién firmó al recibir. Snapshot de texto del <c>Empleado</c> elegido al confirmar
    /// (<c>ComprasService.ConfirmarRecepcion</c>, vía <c>ConfirmarRecepcionEditorViewModel</c>)
    /// — no se teclea a mano.</summary>
    public string RecibidoPor { get; set; } = string.Empty;

    public string Notas { get; set; } = string.Empty;

    public EstadoRecepcionMercancia Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public string EstadoTexto => Estado switch
    {
        EstadoRecepcionMercancia.Borrador => "Borrador",
        EstadoRecepcionMercancia.Confirmada => "Confirmada",
        _ => "Anulada"
    };

    public string LineasTexto => Lineas.Count == 0
        ? "Sin líneas"
        : string.Join(" · ", Lineas.Select(l => $"{l.DestinoTexto} ({l.CantidadRecibidaTexto})"));

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public RecepcionMercancia Clonar()
    {
        var copia = (RecepcionMercancia)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Línea de una recepción: copia de una línea de la orden de compra de origen, más lo que
/// realmente llegó. <see cref="CantidadPedida"/> es solo de referencia; <see cref="CantidadRecibida"/>
/// es la que mueve el stock al confirmar, y puede diferir de la pedida.
///
/// Una línea de diésel elige aquí, además, su <see cref="Presentacion"/> — la empresa no tiene una
/// cisterna común que asignar, así que <c>ComprasService.ConfirmarRecepcion</c> resuelve solo (la
/// busca o la crea) el único <see cref="StockCombustible"/> general "Diesel" al que se suma la
/// cantidad recibida.
/// </summary>
public class RecepcionMercanciaLinea
{
    /// <summary>Presentaciones habituales en que llega el diésel. Solo descriptiva — a diferencia
    /// de <see cref="Lubricante.Presentaciones"/> no tiene una conversión a litros fija, porque el
    /// envase real no tiene una capacidad estándar (un tambor puede traer 180 L o 210 L según de
    /// dónde salga): por eso el litraje siempre se captura aparte, directo, nunca derivado del
    /// envase. Se elige aquí, al recibir, no antes: a diferencia de Lubricante, el precio del
    /// diésel no depende de en qué venga envasado, así que no hace falta fijarlo desde la
    /// cotización.</summary>
    public static readonly IReadOnlyList<string> PresentacionesDiesel =
        ["Tambor/Barril", "Cisterna móvil (pipa)", "Bidón/Galonera", "Granel"];

    public TipoInsumo TipoInsumo { get; set; }

    // --- Combustible ---
    public TipoCombustible? TipoCombustibleSolicitado { get; set; }
    public string? TipoLubricante { get; set; }

    public int? StockCombustibleId { get; set; }
    public string StockCombustibleNombre { get; set; } = string.Empty;   // snapshot

    /// <summary>Marca/clase, heredadas de la orden de compra que decidió qué comprar — de solo
    /// lectura aquí, no se eligen en la recepción.</summary>
    public int? MarcaLubricanteId { get; set; }
    public string MarcaLubricanteNombre { get; set; } = string.Empty;   // snapshot
    public string? ClaseLubricante { get; set; }

    /// <summary>Envase de la línea. En Diésel se elige aquí, al recibir (ver
    /// <see cref="PresentacionesDiesel"/>); en Lubricante viene fijado de la orden de compra (solo
    /// lectura aquí) — ahí es donde se sabe a qué proveedor y en qué presentación se está
    /// comprando. No participa en ningún cálculo de stock.</summary>
    public string? Presentacion { get; set; }

    /// <summary>Precio por litro de esta línea, copiado 1:1 de la línea de la orden de compra de
    /// origen al armar la recepción (<c>ComprasService.CrearRecepcionDesdeOrdenCompra</c>) — no se
    /// vuelve a buscar por Marca+Clase+Grado al confirmar, porque una necesidad puede cubrirse con
    /// varias líneas que comparten esa combinación (mismo lubricante en dos presentaciones), y ahí
    /// una búsqueda por contenido sería ambigua. Solo aplica a Lubricante.</summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>Marca concreta de lubricante (catálogo <see cref="Lubricante"/>) a la que se
    /// suma la cantidad recibida. Ya no lo elige el almacenista: <c>ComprasService.ConfirmarRecepcion</c>
    /// lo resuelve solo (lo busca o lo crea) a partir de Marca+Clase+Grado, que ya vienen fijados
    /// desde la orden de compra. Se conserva en la línea para trazabilidad y para poder anular.</summary>
    public int? LubricanteId { get; set; }
    public string LubricanteNombre { get; set; } = string.Empty;   // snapshot

    // --- Repuesto ---
    public string? ArticuloCodigo { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty;    // snapshot
    public int? ActivoId { get; set; }
    public string ActivoEtiqueta { get; set; } = string.Empty;    // snapshot

    /// <summary>Dónde queda el repuesto en el almacén. Se elige aquí, al recibir — no antes: quien
    /// arma la orden de compra no tiene el repuesto físico enfrente para decidir el anaquel. Nace
    /// prellenada con la ubicación que ya tuviera el artículo en el catálogo (en blanco si es uno
    /// recién creado) y <c>ComprasService.ConfirmarRecepcion</c> la escribe en el catálogo al
    /// confirmar, mismo momento en que suma el stock.</summary>
    public string? UbicacionArticulo { get; set; }

    /// <summary>Referencia: lo que decía la orden de compra. No se usa para mover stock.</summary>
    public decimal CantidadPedida { get; set; }

    /// <summary>Lo que de verdad llegó. Puede diferir de <see cref="CantidadPedida"/> — esto es
    /// lo que mueve el stock al confirmar.</summary>
    public decimal CantidadRecibida { get; set; }

    public string UnidadTexto { get; set; } = string.Empty;

    public bool EsCombustible => TipoInsumo == TipoInsumo.Combustible;

    /// <summary>Distingue, dentro de una línea de combustible, a qué catálogo se suma la
    /// cantidad recibida: Diésel va a <see cref="StockCombustible"/>, Lubricante a <see cref="Lubricante"/>.</summary>
    public bool EsDiesel => EsCombustible && TipoCombustibleSolicitado == TipoCombustible.Diesel;

    public bool EsLubricante => EsCombustible && TipoCombustibleSolicitado == TipoCombustible.Lubricante;

    public bool EsRepuesto => TipoInsumo == TipoInsumo.Repuesto;

    public string TipoInsumoTexto => EsCombustible ? "Combustible" : "Repuesto";

    public string DestinoTexto => EsCombustible
        ? (TipoCombustibleSolicitado == TipoCombustible.Lubricante
            ? $"Lubricante {TipoLubricante}".TrimEnd()
            : "Diésel")
        : ArticuloNombre;

    public string CantidadPedidaTexto => $"{CantidadPedida:N2} {UnidadTexto}".Trim();

    public string CantidadRecibidaTexto => $"{CantidadRecibida:N2} {UnidadTexto}".Trim();

    /// <summary>Diferencia entre lo pedido y lo recibido; "—" si coincide.</summary>
    public string DiferenciaTexto
    {
        get
        {
            var diferencia = CantidadRecibida - CantidadPedida;
            if (diferencia == 0) return "—";
            return diferencia > 0 ? $"Sobrante {diferencia:N2}" : $"Faltante {-diferencia:N2}";
        }
    }

    public RecepcionMercanciaLinea Clonar() => (RecepcionMercanciaLinea)MemberwiseClone();
}
