using System.Collections.Generic;

namespace ASO.Desktop.Models;

/// <summary>
/// Catálogo de lubricantes, con existencia propia — reemplaza el hack anterior que creaba un
/// <see cref="InventoryItem"/> con Categoria "Lubricantes" por cada combinación Tipo × Grado.
///
/// Se identifica por Marca + Tipo + Grado de viscosidad: una sola fila de existencia por
/// producto, igual que <see cref="StockCombustible"/> con Diésel — no importa en qué envase haya
/// llegado cada recepción, todas suman a la misma fila. La presentación (envase) ya no es parte
/// de la identidad: es un dato puramente descriptivo que se anota en cada
/// <c>RecepcionMercanciaLinea</c>, no aquí.
///
/// <see cref="ExistenciaL"/> es lo que de verdad se captura al recibir mercancía (litros
/// recibidos, directo, nunca derivados de un envase) — mismo criterio que
/// <see cref="StockCombustible.ExistenciaL"/>.
/// </summary>
public class Lubricante : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int MarcaLubricanteId { get; set; }
    public string MarcaLubricanteNombre { get; set; } = string.Empty;   // snapshot

    /// <summary>Mineral/Sintético/Semi-sintético. Lista cerrada: ver <see cref="Tipos"/>.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Grado de viscosidad (p. ej. "20W50"). Lista cerrada: ver <see cref="GradosViscosidad"/>.</summary>
    public string GradoViscosidad { get; set; } = string.Empty;

    /// <summary>Litros en existencia. Capturado directo al recibir mercancía (mismo criterio que
    /// <see cref="StockCombustible.ExistenciaL"/>), nunca derivado de un envase.</summary>
    public decimal ExistenciaL { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Precio por litro, snapshot del último precio pagado. Lo estampa
    /// <c>ComprasService.ConfirmarRecepcion</c> a partir del precio unitario (por litro) de la
    /// línea de la Orden de Compra que trajo esta mercancía; no se captura a mano.</summary>
    public decimal CostoUnitario { get; set; }

    /// <summary>Tipos de aceite. Lista cerrada: son los que hay, no texto libre.</summary>
    public static readonly IReadOnlyList<string> Tipos = ["Mineral", "Sintético", "Semi-sintético"];

    /// <summary>Grados de viscosidad habituales en equipo diésel agrícola pesado.</summary>
    public static readonly IReadOnlyList<string> GradosViscosidad =
        ["15W40", "20W50", "10W40", "20W40", "15W30", "SAE 30", "SAE 40"];

    /// <summary>Presentaciones (envase) habituales de venta de lubricante industrial/agrícola.
    /// Puramente descriptivas — se anotan en la Recepción, no tienen litraje fijo asociado: el
    /// envase real varía según el proveedor, así que el litraje siempre se captura aparte,
    /// directo (ver <c>RecepcionMercanciaLinea.LitrosPorEnvase</c>), nunca derivado de aquí.</summary>
    public static readonly IReadOnlyList<string> Presentaciones =
        ["Barril", "Tambor/Cuñete", "Caneca", "Galón", "Granel"];

    public string Etiqueta => $"{MarcaLubricanteNombre} · {Tipo} {GradoViscosidad}".Trim();

    public string ExistenciaTexto => $"{ExistenciaL:N0} L";

    /// <summary>Valor de la existencia a costo, derivado de ExistenciaL × CostoUnitario. No se
    /// guarda: se desincronizaría si el costo unitario se actualiza en una recepción posterior.</summary>
    public decimal ValorTotal => ExistenciaL * CostoUnitario;

    public string ValorTotalTexto => ValorTotal.ToString("N2");

    public string CostoUnitarioTexto => CostoUnitario.ToString("N2");

    public Lubricante Clonar() => (Lubricante)MemberwiseClone();
}
