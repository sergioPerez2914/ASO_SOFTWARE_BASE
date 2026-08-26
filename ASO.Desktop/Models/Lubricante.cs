using System.Collections.Generic;

namespace ASO.Desktop.Models;

/// <summary>
/// Catálogo de lubricantes, con existencia propia — reemplaza el hack anterior que creaba un
/// <see cref="InventoryItem"/> con Categoria "Lubricantes" por cada combinación Tipo × Grado.
///
/// Se identifica por Marca + Tipo + Grado de viscosidad: cada marca es su propia fila de
/// existencia (un Castrol 20W50 Sintético y un Mobil 20W50 Sintético son productos distintos).
/// La Requisición solo pide Tipo + Grado (como Diésel nunca referencia un StockCombustible
/// concreto); la marca se decide recién al Recibir mercancía, que es cuando se sabe qué trajo
/// el proveedor.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class Lubricante : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public string Marca { get; set; } = string.Empty;

    /// <summary>Mineral/Sintético/Semi-sintético. Lista cerrada: ver <see cref="Tipos"/>.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Grado de viscosidad (p. ej. "20W50"). Lista cerrada: ver <see cref="GradosViscosidad"/>.</summary>
    public string GradoViscosidad { get; set; } = string.Empty;

    public decimal ExistenciaL { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Tipos de aceite. Lista cerrada: son los que hay, no texto libre.</summary>
    public static readonly IReadOnlyList<string> Tipos = ["Mineral", "Sintético", "Semi-sintético"];

    /// <summary>Grados de viscosidad habituales en equipo diésel agrícola pesado.</summary>
    public static readonly IReadOnlyList<string> GradosViscosidad =
        ["15W40", "20W50", "10W40", "20W40", "15W30", "SAE 30", "SAE 40"];

    public string Etiqueta => $"{Marca} · {Tipo} {GradoViscosidad}".Trim();

    public string ExistenciaTexto => $"{ExistenciaL:N0} L";

    public Lubricante Clonar() => (Lubricante)MemberwiseClone();
}
