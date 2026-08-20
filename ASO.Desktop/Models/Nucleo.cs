namespace ASO.Desktop.Models;

/// <summary>
/// Núcleo de productores. Su código (C.O.D) es el que lo identifica en el sistema del CAM
/// y es la base del pago por corte, alza y empuje, y transporte.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class Nucleo : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    public string Etiqueta => $"{Codigo} · {Nombre}";
}
