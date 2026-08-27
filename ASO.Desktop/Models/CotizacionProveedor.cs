using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Precio cotizado por un proveedor para atender una <see cref="Requisicion"/>. No es un
/// documento con máquina de estados propia — es el apunte que deja quien compara precios antes
/// de armar la orden de compra, para que la comparación quede a la vista y no solo en la memoria
/// de quien decidió. Se captura una fila por proveedor consultado; la ganadora la marca
/// <see cref="OrdenCompra.CotizacionSeleccionadaId"/>.
///
/// Documento con líneas propias, como una factura del proveedor: cada cotización lleva su propio
/// detalle (precio unitario, marca, presentación), no solo un monto suelto — así, al elegir la
/// ganadora, <c>ComprasService.CrearDesdeRequisicion</c> reutiliza ese detalle tal cual, sin volver
/// a pedirlo. Mismo patrón que <see cref="Requisicion"/>, <see cref="OrdenCompra"/> y
/// <see cref="RecepcionMercancia"/>.
/// </summary>
public class CotizacionProveedor : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int RequisicionId { get; set; }

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;   // snapshot

    public List<CotizacionProveedorLinea> Lineas { get; set; } = [];

    public string Notas { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    /// <summary>
    /// Suma de las líneas, no un campo guardado: guardarlo aparte arriesgaría que se desincronice
    /// de lo que dicen las líneas si una se corrige antes de agregar la cotización. Mismo criterio
    /// que <see cref="OrdenCompra.MontoTotal"/>.
    /// </summary>
    public decimal MontoTotal => Lineas.Sum(l => l.Subtotal);

    public string MontoTexto => MontoTotal.ToString("N2");

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public CotizacionProveedor Clonar()
    {
        var copia = (CotizacionProveedor)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Línea de una cotización: copia de una línea de la requisición de origen, más el precio que
/// ofrece este proveedor en particular. Mismo shape que <see cref="OrdenCompraLinea"/> — es lo
/// que se copia 1:1 hacia la orden de compra cuando esta cotización resulta ganadora.
/// </summary>
public class CotizacionProveedorLinea
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

    /// <summary>Litros pedidos, de referencia (viene de la requisición y no se toca aquí). Para
    /// Lubricante, lo que de verdad se compra es <see cref="Unidades"/> — esto solo ayuda a saber
    /// cuánto volumen hay que cubrir con envases.</summary>
    public decimal Cantidad { get; set; }
    public string UnidadTexto { get; set; } = string.Empty;

    /// <summary>Cuántos envases de <see cref="Presentacion"/> se cotizan. Solo aplica a
    /// Lubricante — es el dato que faltaba: sin él, "Precio unitario" quedaba ambiguo entre
    /// precio por litro y precio por envase.</summary>
    public decimal Unidades { get; set; }

    /// <summary>Precio por envase en Lubricante (por <see cref="Unidades"/>), precio por litro/
    /// unidad de repuesto en Diésel/Repuesto (por <see cref="Cantidad"/>).</summary>
    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal => (EsLubricante ? Unidades : Cantidad) * PrecioUnitario;

    public string TipoInsumoTexto => TipoInsumo == TipoInsumo.Combustible ? "Combustible" : "Repuesto";

    public bool EsCombustible => TipoInsumo == TipoInsumo.Combustible;

    public bool EsDiesel => EsCombustible && TipoCombustibleSolicitado == TipoCombustible.Diesel;

    public bool EsLubricante => EsCombustible && TipoCombustibleSolicitado == TipoCombustible.Lubricante;

    public string DestinoTexto => TipoInsumo == TipoInsumo.Combustible
        ? (TipoCombustibleSolicitado == TipoCombustible.Lubricante
            ? $"Lubricante {TipoLubricante}".TrimEnd()
            : "Diésel")
        : ArticuloNombre;

    public string UnidadDestinoTexto => TipoInsumo == TipoInsumo.Repuesto
        ? (string.IsNullOrWhiteSpace(ActivoEtiqueta) ? "Almacén / otro" : ActivoEtiqueta)
        : string.Empty;

    public string CantidadTexto => $"{Cantidad:N2} {UnidadTexto}".Trim();

    public string UnidadesTexto => $"{Unidades:N2} {Presentacion}".Trim();

    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N2");

    public string SubtotalTexto => Subtotal.ToString("N2");

    public CotizacionProveedorLinea Clonar() => (CotizacionProveedorLinea)MemberwiseClone();
}
