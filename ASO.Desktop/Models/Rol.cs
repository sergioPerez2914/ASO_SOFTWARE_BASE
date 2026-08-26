namespace ASO.Desktop.Models;

/// <summary>
/// Roles del sistema. Cada uno trae un conjunto base de permisos
/// (<c>Services/MatrizPermisos.cs</c>) que el administrador puede ajustar por usuario
/// con <see cref="PermisoUsuario"/>.
///
/// Se persisten como ORDINAL (columna <c>Usuarios.Rol int</c>, sin <c>HasConversion</c>), asi
/// que los miembros se agregan SIEMPRE al final: declarar uno en medio reinterpretaria los
/// usuarios ya guardados. Misma regla que <see cref="TipoEventoOperacion"/>.
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
    Desarrollador,

    /// <summary>
    /// Duenno del deposito. Manda en Inventario —repuestos, combustible, lubricantes— y
    /// responde el otro extremo de Compras: atiende las requisiciones que le llegan, cotiza,
    /// arma la orden de compra y recibe la mercancia. No aprueba el gasto (eso es del
    /// administrador), no entra a Operaciones ni a Nomina, y del dinero solo ve la deuda con
    /// proveedores. En la bandeja resuelve las peticiones de SU dominio, no las demas.
    /// </summary>
    Almacenista
}
