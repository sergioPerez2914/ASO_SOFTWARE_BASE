namespace ASO.Desktop.Models;

/// <summary>
/// Empleado del centro. Dato maestro: cruza zafras, no lleva <c>ZafraId</c>.
/// Modelo de presentación temporal; se alineará con la entidad <c>Empleado</c>
/// del dominio cuando exista la capa de datos.
/// </summary>
public class Empleado : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
}
