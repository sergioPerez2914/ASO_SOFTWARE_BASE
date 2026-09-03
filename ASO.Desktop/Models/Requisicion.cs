using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de la requisición. <c>Borrador</c> admite ajustes de líneas; al enviarla queda
/// inmutable y disponible para armar una orden de compra. <c>Atendida</c> marca que una orden
/// de compra ya la tomó — una requisición no alimenta dos órdenes.
/// </summary>
public enum EstadoRequisicion
{
    Borrador,
    Enviada,
    Atendida,
    Anulada
}

/// <summary>
/// Primer eslabón del flujo de compras: quien está en el campo o el taller identifica cuánto
/// combustible/aceite o qué repuestos hacen falta, antes de que nadie compare precios ni compre
/// nada. No lleva monto — eso lo decide la cotización, más adelante en la cadena.
/// </summary>
public class Requisicion : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public List<RequisicionLinea> Lineas { get; set; } = [];

    public EstadoRequisicion Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaEnvio { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public string EstadoTexto => Estado switch
    {
        EstadoRequisicion.Borrador => "Borrador",
        EstadoRequisicion.Enviada => "Enviada",
        EstadoRequisicion.Atendida => "Atendida",
        _ => "Anulada"
    };

    public string LineasTexto => Lineas.Count == 0
        ? "Sin líneas"
        : string.Join(" · ", Lineas.Select(l => $"{l.DestinoTexto} ({l.CantidadTexto})"));

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public Requisicion Clonar()
    {
        var copia = (Requisicion)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Línea de una requisición: cuánto hace falta de qué.
///
/// Combustible no tiene un catálogo que elegir todavía (ver <see cref="TipoCombustible"/>): solo
/// dice si es diésel o lubricante, y si es lubricante, su grado. Repuesto sí es del catálogo de
/// <see cref="InventoryItem"/>, guardado por código y como texto — la requisición debe seguir
/// diciendo lo mismo aunque el catálogo cambie después — y opcionalmente dice para qué unidad de
/// <see cref="ActivoFlota"/> es, que es de donde sale la marca y el modelo sin tener que repetirlos.
/// </summary>
public class RequisicionLinea
{
    public TipoInsumo TipoInsumo { get; set; }

    // --- Combustible ---
    public TipoCombustible? TipoCombustibleSolicitado { get; set; }

    /// <summary>Grado/viscosidad del lubricante (p. ej. "20W50"). Solo aplica cuando
    /// <see cref="TipoCombustibleSolicitado"/> es <see cref="TipoCombustible.Lubricante"/>.</summary>
    public string? TipoLubricante { get; set; }

    /// <summary>Mineral/Sintético/Semi-sintético (ver <see cref="Lubricante.Tipos"/>). Solo aplica
    /// cuando <see cref="TipoCombustibleSolicitado"/> es <see cref="TipoCombustible.Lubricante"/>;
    /// viaja a <c>OrdenCompraLinea.ClaseLubricante</c> al armar la orden, para no volver a
    /// pedirlo.</summary>
    public string? ClaseLubricante { get; set; }

    // --- Repuesto ---
    public string? ArticuloCodigo { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty;    // snapshot

    /// <summary>Unidad de flota a la que se destina el repuesto. Opcional: puede pedirse para
    /// reponer almacén sin apuntar a una máquina en concreto.</summary>
    public int? ActivoId { get; set; }
    public string ActivoEtiqueta { get; set; } = string.Empty;    // snapshot ("COS-01 · Marca Modelo")

    public decimal Cantidad { get; set; }
    public string UnidadTexto { get; set; } = string.Empty;       // "L" o InventoryItem.Unidad

    public string TipoInsumoTexto => TipoInsumo == TipoInsumo.Combustible ? "Combustible" : "Repuesto";

    public string DestinoTexto => TipoInsumo == TipoInsumo.Combustible
        ? (TipoCombustibleSolicitado == TipoCombustible.Lubricante
            ? $"Lubricante {TipoLubricante}".TrimEnd()
            : "Diésel")
        : ArticuloNombre;

    /// <summary>Para qué unidad es el repuesto; vacío en una línea de combustible.</summary>
    public string UnidadDestinoTexto => TipoInsumo == TipoInsumo.Repuesto
        ? (string.IsNullOrWhiteSpace(ActivoEtiqueta) ? "Almacén / otro" : ActivoEtiqueta)
        : string.Empty;

    public string CantidadTexto => $"{Cantidad:N2} {UnidadTexto}".Trim();

    public RequisicionLinea Clonar() => (RequisicionLinea)MemberwiseClone();
}
