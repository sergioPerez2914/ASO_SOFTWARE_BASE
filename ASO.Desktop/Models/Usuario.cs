namespace ASO.Desktop.Models;

/// <summary>
/// Usuario autenticado del sistema.
/// Modelo de presentación temporal; se alineará con la entidad
/// <c>Usuario</c> del dominio (tabla Usuario/Rol) cuando exista la capa de datos.
/// </summary>
public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public Rol Rol { get; set; }
}
