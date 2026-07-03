namespace ASO.Desktop.Models;

/// <summary>
/// Roles del sistema. Filtran el acceso a secciones y, más adelante,
/// las reglas de aprobación de tickets (ver plan SIGZ/ASO).
/// </summary>
public enum Rol
{
    Admin,
    Operaciones,
    Taller,
    Finanzas,
    RRHH,
    Consulta
}
