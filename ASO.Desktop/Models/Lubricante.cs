using System.Collections.Generic;

namespace ASO.Desktop.Models;

/// <summary>
/// Catálogo de lubricantes, con existencia propia — reemplaza el hack anterior que creaba un
/// <see cref="InventoryItem"/> con Categoria "Lubricantes" por cada combinación Tipo × Grado.
///
/// Se identifica por Marca + Tipo + Grado de viscosidad + Presentación: cada combinación es su
/// propia fila de existencia (un Castrol 20W50 Sintético en Barril y el mismo producto en Galón
/// se cuentan aparte, porque no tiene sentido sumar "3 barriles + 2 galones" como una sola
/// cantidad de envases). Marca, Tipo, Grado y Presentación se deciden al armar la Orden de
/// Compra (ver <c>OrdenCompraLinea</c>) — la Requisición solo pide el grado; la fila de
/// <see cref="Lubricante"/> misma la resuelve <c>ComprasService.ConfirmarRecepcion</c> (búscala o
/// créala), ya no la escoge a mano quien recibe.
///
/// <see cref="Unidades"/> es lo que de verdad se cuenta al recibir (cuántos barriles/galones
/// llegaron) — es lo único que se guarda. <see cref="ExistenciaL"/> es solo una lectura derivada
/// para mostrar el total en litros, no un dato capturado.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
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

    /// <summary>Envase en que se cuenta esta fila. Lista cerrada: ver <see cref="Presentaciones"/>.</summary>
    public string Presentacion { get; set; } = string.Empty;

    /// <summary>Cuántos envases de <see cref="Presentacion"/> hay. Lo capturado de verdad al
    /// recibir mercancía; <see cref="ExistenciaL"/> se deriva de esto.</summary>
    public decimal Unidades { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Tipos de aceite. Lista cerrada: son los que hay, no texto libre.</summary>
    public static readonly IReadOnlyList<string> Tipos = ["Mineral", "Sintético", "Semi-sintético"];

    /// <summary>Grados de viscosidad habituales en equipo diésel agrícola pesado.</summary>
    public static readonly IReadOnlyList<string> GradosViscosidad =
        ["15W40", "20W50", "10W40", "20W40", "15W30", "SAE 30", "SAE 40"];

    /// <summary>Presentaciones (envase) habituales de venta de lubricante industrial/agrícola.</summary>
    public static readonly IReadOnlyList<string> Presentaciones =
        ["Barril", "Tambor/Cuñete", "Caneca", "Galón", "Granel"];

    /// <summary>
    /// Litros que contiene un envase de cada presentación (tambor metálico estándar de 208 L
    /// para Barril/Tambor-Cuñete, caneca de 20 L, galón de 3.785 L). "Granel" no tiene envase:
    /// se cuenta directo en litros, así que 1 unidad = 1 litro.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, decimal> LitrosPorPresentacion =
        new Dictionary<string, decimal>
        {
            ["Barril"] = 208m,
            ["Tambor/Cuñete"] = 208m,
            ["Caneca"] = 20m,
            ["Galón"] = 3.785m,
            ["Granel"] = 1m
        };

    public string Etiqueta => $"{MarcaLubricanteNombre} · {Tipo} {GradoViscosidad}".Trim();

    /// <summary>Total en litros, derivado de Unidades × litros del envase. No se guarda: si se
    /// guardara aparte, podría desincronizarse de lo que dicen Presentación y Unidades.</summary>
    public decimal ExistenciaL =>
        Unidades * (LitrosPorPresentacion.TryGetValue(Presentacion, out var litros) ? litros : 0m);

    public string ExistenciaTexto => $"{ExistenciaL:N0} L";

    public string UnidadesTexto => $"{Unidades:N2} {Presentacion}".Trim();

    public Lubricante Clonar() => (Lubricante)MemberwiseClone();
}
