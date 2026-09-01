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
    /// <summary>Índice (0-based) de la línea de <see cref="Requisicion.Lineas"/> que esta línea de
    /// compra cubre. Una necesidad puede tener varias líneas de compra — p. ej. 150 L de un grado
    /// cubiertos con 100 L de una marca en barril más 50 L de otra en galón — así que la relación
    /// no es 1:1 como antes; esto es lo que permite agruparlas y calcular la cobertura.</summary>
    public int RequisicionLineaIndex { get; set; }

    public TipoInsumo TipoInsumo { get; set; }

    // --- Combustible ---
    public TipoCombustible? TipoCombustibleSolicitado { get; set; }
    public string? TipoLubricante { get; set; }    // grado/viscosidad, p. ej. "20W50"

    public int? MarcaLubricanteId { get; set; }
    public string MarcaLubricanteNombre { get; set; } = string.Empty;   // snapshot

    /// <summary>Mineral/Sintético/Semi-sintético. Lista cerrada: ver <see cref="Lubricante.Tipos"/>.</summary>
    public string? ClaseLubricante { get; set; }

    /// <summary>Envase en que viene. Lista cerrada: ver <see cref="Lubricante.Presentaciones"/>.
    /// Se define aquí, al armar la orden de compra — es donde se sabe qué se está comprando a cada
    /// proveedor, no al recibir.</summary>
    public string? Presentacion { get; set; }

    /// <summary>Litros que trae el envase de <see cref="Presentacion"/> en esta compra en
    /// particular — no es una tabla fija, el envase real varía según el proveedor. Opcional.</summary>
    public decimal? LitrosPorEnvase { get; set; }

    /// <summary>Cuántos envases de <see cref="Presentacion"/> se compran en esta línea. Opcional y
    /// puramente informativo — junto con <see cref="LitrosPorEnvase"/> ayuda a calcular
    /// <see cref="Cantidad"/> al agregar la línea (envases × litros por envase), pero no la
    /// reemplaza: <see cref="Cantidad"/> sigue siendo lo que de verdad se guarda y se puede
    /// corregir a mano si el envase real no coincide con lo estimado aquí.</summary>
    public decimal? Unidades { get; set; }

    // --- Repuesto ---
    public string? ArticuloCodigo { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty;    // snapshot
    public int? ActivoId { get; set; }
    public string ActivoEtiqueta { get; set; } = string.Empty;    // snapshot

    /// <summary>Litros/unidades de ESTA línea de compra — no tiene que igualar lo pedido en la
    /// requisición: varias líneas contra la misma necesidad se suman para cubrirla.</summary>
    public decimal Cantidad { get; set; }
    public string UnidadTexto { get; set; } = string.Empty;

    /// <summary>Precio por litro en combustible, por unidad de repuesto en Repuesto — siempre
    /// por <see cref="Cantidad"/>.</summary>
    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

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

    public string UnidadesTexto => Unidades is > 0 ? $"{Unidades:N2} {Presentacion}".Trim() : "—";

    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N2");

    public string SubtotalTexto => Subtotal.ToString("N2");

    public CotizacionProveedorLinea Clonar() => (CotizacionProveedorLinea)MemberwiseClone();
}
