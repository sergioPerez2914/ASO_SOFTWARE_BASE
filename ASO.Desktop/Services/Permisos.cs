namespace ASO.Desktop.Services;

/// <summary>
/// Catalogo de los permisos que existen, en formato "Modulo.Accion".
///
/// Los comandos los piden por cadena, y <c>CrudViewModelBase</c> arma tres por submodulo
/// interpolando <c>ModuloPermiso</c>. Tener el catalogo escrito en un solo sitio es lo que
/// permite revisar la matriz de un vistazo y detectar un permiso huerfano.
///
/// Los permisos de navegacion llevan el prefijo <c>Ver.</c> y su sufijo es la clave del
/// submodulo en <c>Navigation/ModuloCatalogo.cs</c>, para que no puedan desincronizarse.
/// </summary>
public static class Permisos
{
    public const string PrefijoVer = "Ver.";

    /// <summary>Permiso de navegacion de una clave de modulo o submodulo.</summary>
    public static string Ver(string clave) => PrefijoVer + clave;

    public static class Remesas
    {
        public const string Crear = "Remesas.Crear";
        public const string Editar = "Remesas.Editar";
        public const string Eliminar = "Remesas.Eliminar";
        public const string Confirmar = "Remesas.Confirmar";
        public const string Anular = "Remesas.Anular";
        public const string Recepcion = "Remesas.Recepcion";
    }

    public static class Seguimiento
    {
        public const string AgregarNota = "Seguimiento.AgregarNota";
    }

    public static class Fincas
    {
        public const string Crear = "Fincas.Crear";
        public const string Editar = "Fincas.Editar";
        public const string Eliminar = "Fincas.Eliminar";
    }

    public static class Nucleos
    {
        public const string Crear = "Nucleos.Crear";
        public const string Editar = "Nucleos.Editar";
        public const string Eliminar = "Nucleos.Eliminar";
    }

    public static class Flota
    {
        public const string Crear = "Flota.Crear";
        public const string Editar = "Flota.Editar";
        public const string CambiarEstado = "Flota.CambiarEstado";
    }

    public static class Mantenimiento
    {
        public const string Registrar = "Mantenimiento.Registrar";
    }

    public static class Inventario
    {
        public const string Crear = "Inventario.Crear";
        public const string Editar = "Inventario.Editar";
        public const string Eliminar = "Inventario.Eliminar";
        public const string RegistrarSalida = "Inventario.RegistrarSalida";
        public const string ConfirmarSalida = "Inventario.ConfirmarSalida";
        public const string AnularSalida = "Inventario.AnularSalida";

        /// <summary>Borrar una salida en borrador. Antes compartia cadena con Eliminar
        /// (borrar el articulo del catalogo), que son cosas muy distintas.</summary>
        public const string EliminarSalida = "Inventario.EliminarSalida";

        public const string OverrideStock = "Inventario.OverrideStock";
    }

    public static class Combustible
    {
        public const string Crear = "Combustible.Crear";
        public const string Editar = "Combustible.Editar";
        public const string Eliminar = "Combustible.Eliminar";
        public const string Confirmar = "Combustible.Confirmar";
        public const string Anular = "Combustible.Anular";
        public const string Recargar = "Combustible.Recargar";
    }

    public static class Empleados
    {
        public const string Crear = "Empleados.Crear";
        public const string Editar = "Empleados.Editar";
        public const string Eliminar = "Empleados.Eliminar";
    }

    /// <summary>Padron de campo, separado de Empleados: son dos padrones distintos.</summary>
    public static class PersonalCampo
    {
        public const string Crear = "PersonalCampo.Crear";
        public const string Editar = "PersonalCampo.Editar";
        public const string Eliminar = "PersonalCampo.Eliminar";
    }

    public static class Horarios
    {
        public const string Crear = "Horarios.Crear";
        public const string Editar = "Horarios.Editar";
        public const string Eliminar = "Horarios.Eliminar";
        public const string RegistrarSalida = "Horarios.RegistrarSalida";
    }

    public static class Nomina
    {
        public const string Crear = "Nomina.Crear";
        public const string Editar = "Nomina.Editar";
        public const string Eliminar = "Nomina.Eliminar";
        public const string Generar = "Nomina.Generar";
        public const string EditarLineas = "Nomina.EditarLineas";
        public const string Cerrar = "Nomina.Cerrar";
        public const string Pagar = "Nomina.Pagar";
        public const string Anular = "Nomina.Anular";
    }

    /// <summary>Facturas al ingenio. Antes compartia cadena con proveedores y sus facturas.</summary>
    public static class FacturasCliente
    {
        public const string Crear = "FacturasCliente.Crear";
        public const string Editar = "FacturasCliente.Editar";
        public const string Eliminar = "FacturasCliente.Eliminar";
    }

    public static class Proveedores
    {
        public const string Crear = "Proveedores.Crear";
        public const string Editar = "Proveedores.Editar";
        public const string Eliminar = "Proveedores.Eliminar";
    }

    public static class FacturasProveedor
    {
        public const string Crear = "FacturasProveedor.Crear";
        public const string Editar = "FacturasProveedor.Editar";
        public const string Eliminar = "FacturasProveedor.Eliminar";
    }

    public static class Finanzas
    {
        public const string Facturar = "Finanzas.Facturar";
        public const string RegistrarCobro = "Finanzas.RegistrarCobro";
        public const string Pagar = "Finanzas.Pagar";
        public const string Anular = "Finanzas.Anular";
    }

    public static class Tarifas
    {
        public const string Crear = "Tarifas.Crear";
        public const string Editar = "Tarifas.Editar";
        public const string Eliminar = "Tarifas.Eliminar";
    }

    public static class Peticiones
    {
        public const string Solicitar = "Peticiones.Solicitar";
        public const string Resolver = "Peticiones.Resolver";
    }

    public static class Usuarios
    {
        public const string Crear = "Usuarios.Crear";
        public const string Editar = "Usuarios.Editar";
        public const string Eliminar = "Usuarios.Eliminar";
    }

    public static class Organizaciones
    {
        /// <summary>Cambiar de nucleo sin cerrar sesion. Solo el Desarrollador.</summary>
        public const string Cambiar = "Organizaciones.Cambiar";
        public const string Crear = "Organizaciones.Crear";
        public const string Editar = "Organizaciones.Editar";

        /// <summary>
        /// Existe porque <c>CrudViewModelBase</c> lo pide para su comando Eliminar, pero
        /// <c>OrganizacionCrudViewModel.PuedeEliminar</c> devuelve siempre false: borrar un
        /// núcleo dejaría sus filas sin dueño e invisibles bajo el filtro fail-closed.
        /// Se declara para que no quede como permiso fantasma, y no se concede a nadie.
        /// </summary>
        public const string Eliminar = "Organizaciones.Eliminar";
    }
}
