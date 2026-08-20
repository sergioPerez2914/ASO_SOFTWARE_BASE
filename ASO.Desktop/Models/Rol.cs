namespace ASO.Desktop.Models;

/// <summary>
/// Roles del sistema. Cada uno trae un conjunto base de permisos
/// (<c>Services/MatrizPermisos.cs</c>) que el administrador puede ajustar por usuario
/// con <see cref="PermisoUsuario"/>.
/// </summary>
public enum Rol
{
    /// <summary>
    /// Opera lo del dia a dia en su nucleo: remesas, seguimiento, flota, mantenimiento,
    /// horarios y combustible. No anula documentos ni toca catalogos maestros, finanzas
    /// ni nomina: para eso levanta una <see cref="PeticionCambio"/>.
    /// </summary>
    Remesero,

    /// <summary>
    /// Manda dentro de SU nucleo: ve y modifica todo, resuelve las peticiones del remesero
    /// y administra los usuarios de su organizacion. No ve otras organizaciones.
    /// </summary>
    AdministradorNucleo,

    /// <summary>
    /// Nosotros. Puede todo y ademas cambiar de nucleo sin cerrar sesion, para dar soporte
    /// y probar. Es el unico rol que atraviesa organizaciones.
    /// </summary>
    Desarrollador
}
