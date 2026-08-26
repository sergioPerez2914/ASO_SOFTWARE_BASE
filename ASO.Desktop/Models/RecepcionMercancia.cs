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
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
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

    /// <summary>Quién firmó al recibir. Texto libre — PROVISIONAL, como ResponsableNombre en
    /// ValeCombustible, hasta que el padrón de empleados esté conectado a la operación.</summary>
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
/// Una línea de combustible necesita, además, decir a qué <see cref="StockCombustible"/> del
/// catálogo se suma: la orden de compra solo dice diésel/lubricante (<see cref="TipoCombustibleSolicitado"/>),
/// no un producto concreto — eso lo elige quien recibe, aquí.
/// </summary>
public class RecepcionMercanciaLinea
{
    public TipoInsumo TipoInsumo { get; set; }

    // --- Combustible ---
    public TipoCombustible? TipoCombustibleSolicitado { get; set; }
    public string? TipoLubricante { get; set; }

    public int? StockCombustibleId { get; set; }
    public string StockCombustibleNombre { get; set; } = string.Empty;   // snapshot

    /// <summary>Marca concreta de lubricante a la que se suma la cantidad recibida. Solo aplica
    /// cuando <see cref="TipoCombustibleSolicitado"/> es <see cref="TipoCombustible.Lubricante"/> —
    /// la orden de compra solo decía diésel/lubricante, no una marca; eso se elige aquí.</summary>
    public int? LubricanteId { get; set; }
    public string LubricanteNombre { get; set; } = string.Empty;   // snapshot

    // --- Repuesto ---
    public string? ArticuloCodigo { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty;    // snapshot
    public int? ActivoId { get; set; }
    public string ActivoEtiqueta { get; set; } = string.Empty;    // snapshot

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
