using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;

namespace ASO.Desktop.Services;

/// <summary>
/// Que puede cada rol. Es el conjunto BASE; el administrador lo ajusta por usuario con
/// <see cref="PermisoUsuario"/> y la suma la resuelve <see cref="SesionActual.IniciarSesion"/>.
///
/// El universo de permisos se arma por reflexion sobre <see cref="Permisos"/> y sobre el
/// catalogo de navegacion, a proposito: agregar un permiso o un submodulo lo mete solo en
/// "todos", de modo que un rol total no se queda corto por olvido.
/// </summary>
public static class MatrizPermisos
{
    /// <summary>Todos los permisos de accion declarados en <see cref="Permisos"/>.</summary>
    private static readonly HashSet<string> _acciones =
        typeof(Permisos).GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

    /// <summary>
    /// Los permisos "Ver.*" que de verdad se consultan, derivados del catalogo de navegacion:
    /// los modulos fijados (Inicio, Peticiones, Administracion, Configuracion) y todos los
    /// submodulos.
    ///
    /// Los cinco modulos del negocio NO entran: su visibilidad la deciden sus submodulos
    /// (ver <see cref="NavegacionPermitida"/>), asi que "Ver.Finanzas" no se lee nunca.
    /// Ofrecerlo en el desplegable de ajustes solo serviria para que alguien lo conceda y no
    /// pase nada.
    /// </summary>
    private static readonly HashSet<string> _navegacion =
        ModuloCatalogo.TodosLosFijados.Select(m => m.Permiso)
            .Concat(ModuloCatalogo.Modulos.SelectMany(m => m.Submodulos).Select(s => s.Permiso))
            .ToHashSet();

    public static IReadOnlySet<string> Todos { get; } =
        _acciones.Concat(_navegacion).ToHashSet();

    /// <summary>
    /// Reservado al Desarrollador: repartir su propio rol. Un administrador manda en el
    /// nucleo, pero no fabrica usuarios con mas alcance del que el mismo tiene.
    /// </summary>
    private static readonly HashSet<string> _soloDesarrollador =
    [
        Permisos.Usuarios.CrearDesarrollador
    ];

    /// <summary>
    /// Lo del dia a dia en campo. Crea, edita y confirma lo suyo; no anula nada, no entra a
    /// finanzas ni a liquidaciones, y no toca catalogos maestros. Para todo eso, peticion.
    /// </summary>
    private static readonly HashSet<string> _remesero =
    [
        Permisos.Ver(ModuloCatalogo.Inicio.Clave),
        Permisos.Ver(ModuloCatalogo.Peticiones.Clave),
        Permisos.Ver(ModuloCatalogo.Configuracion.Clave),
        Permisos.Ver("Operaciones.Registro"),
        Permisos.Ver("Operaciones.Seguimiento"),
        Permisos.Ver("Flota.Gestion"),
        Permisos.Ver("Flota.Mantenimiento"),
        Permisos.Ver("Flota.Telemetria"),
        Permisos.Ver("Inventario.Combustible"),
        Permisos.Ver("Inventario.Compras"),
        Permisos.Ver("Nomina.Horarios"),

        Permisos.Remesas.Crear,
        Permisos.Remesas.Editar,
        Permisos.Remesas.Eliminar,
        Permisos.Remesas.Confirmar,

        Permisos.Seguimiento.AgregarNota,

        Permisos.Flota.CambiarEstado,
        Permisos.Mantenimiento.Registrar,

        Permisos.Combustible.Crear,
        Permisos.Combustible.Editar,
        Permisos.Combustible.Eliminar,
        Permisos.Combustible.Confirmar,

        Permisos.Horarios.Crear,
        Permisos.Horarios.Editar,
        Permisos.Horarios.RegistrarSalida,

        // Identifica y envía la necesidad; comparar proveedores y aprobar el gasto (la orden de
        // compra) queda exclusivo del administrador.
        Permisos.Requisicion.Crear,
        Permisos.Requisicion.Editar,
        Permisos.Requisicion.Eliminar,
        Permisos.Requisicion.Enviar,
        Permisos.Requisicion.Anular,

        Permisos.Peticiones.Solicitar
    ];

    /// <summary>
    /// El deposito. Manda en Inventario y responde el otro extremo de Compras: atiende las
    /// requisiciones que le llegan, cotiza, arma la orden de compra y recibe la mercancia.
    /// Ve Flota de solo lectura porque una salida de repuestos se imputa a una maquina, y
    /// Cuentas por Pagar porque el padron de Proveedores vive ahi.
    /// </summary>
    private static readonly HashSet<string> _almacenista =
    [
        Permisos.Ver(ModuloCatalogo.Inicio.Clave),
        Permisos.Ver(ModuloCatalogo.Peticiones.Clave),
        Permisos.Ver(ModuloCatalogo.Configuracion.Clave),
        Permisos.Ver("Inventario.Repuestos"),
        Permisos.Ver("Inventario.Combustible"),
        Permisos.Ver("Inventario.Compras"),
        Permisos.Ver("Flota.Gestion"),
        Permisos.Ver("Flota.Mantenimiento"),
        Permisos.Ver("Finanzas.CuentasPorPagar"),

        Permisos.Inventario.Crear,
        Permisos.Inventario.Editar,
        Permisos.Inventario.Eliminar,
        Permisos.Inventario.RegistrarSalida,
        Permisos.Inventario.ConfirmarSalida,
        Permisos.Inventario.AnularSalida,
        Permisos.Inventario.EliminarSalida,

        Permisos.Combustible.Crear,
        Permisos.Combustible.Editar,
        Permisos.Combustible.Eliminar,
        Permisos.Combustible.Confirmar,
        Permisos.Combustible.Anular,
        Permisos.Combustible.Recargar,

        Permisos.Lubricantes.Crear,
        Permisos.Lubricantes.Editar,
        Permisos.Lubricantes.Eliminar,

        Permisos.Requisicion.Crear,
        Permisos.Requisicion.Editar,
        Permisos.Requisicion.Eliminar,
        Permisos.Requisicion.Enviar,
        Permisos.Requisicion.Anular,

        // Cotiza y arma la orden, pero NO la aprueba ni la anula: aprobar es autorizar el gasto
        // y anular es deshacer un compromiso ya autorizado. Quien compra y recibe no firma el
        // dinero. Sus ordenes en Borrador si las borra, con Eliminar.
        Permisos.OrdenCompra.Crear,
        Permisos.OrdenCompra.Editar,
        Permisos.OrdenCompra.Eliminar,

        Permisos.RecepcionMercancia.Crear,
        Permisos.RecepcionMercancia.Editar,
        Permisos.RecepcionMercancia.Eliminar,
        Permisos.RecepcionMercancia.Confirmar,
        Permisos.RecepcionMercancia.Anular,

        // Los da de alta al vuelo mientras compara cotizaciones. No registra sus facturas:
        // ve la deuda en Cuentas por Pagar, no la escribe.
        Permisos.Proveedores.Crear,
        Permisos.Proveedores.Editar,

        // Resuelve peticiones, pero solo las de su dominio: la regla esta en
        // PeticionService.EsDeSuDominio, no aqui.
        Permisos.Peticiones.Solicitar,
        Permisos.Peticiones.Resolver

        // Fuera a proposito:
        // - Inventario.OverrideStock: forzar una salida sin existencia. Es justo el custodio
        //   del almacen quien no debe poder tapar un descuadre.
        // - Configuracion.Preferencias: detras va el umbral de alerta de consumo, y aqui el
        //   encargado del combustible se apagaria sus propias alertas.
    ];

    private static readonly HashSet<string> _administrador =
        Todos.Where(p => !_soloDesarrollador.Contains(p)).ToHashSet();

    /// <summary>Conjunto base del rol, antes de los ajustes por usuario.</summary>
    // Sin arco de descarte, y es deliberado: con `_ => Todos` un rol nuevo que se olvide aqui
    // se convierte en superusuario EN SILENCIO. Ahora el compilador avisa (CS8509) en cuanto se
    // declare un rol y no se mapee. Se calla CS8524, que solo se dispararia con un entero que no
    // corresponde a ningun miembro: fila corrupta, y ahi preferimos la excepcion al disimulo.
    // Mismo criterio que los switches de TipoEventoOperacion.
#pragma warning disable CS8524
    public static IReadOnlySet<string> Base(Rol rol) => rol switch
    {
        Rol.Remesero => _remesero,
        Rol.Almacenista => _almacenista,
        Rol.AdministradorNucleo => _administrador,
        Rol.Desarrollador => Todos
    };
#pragma warning restore CS8524

    /// <summary>
    /// Permisos que, si faltan, no dejan el boton muerto: abren una peticion al administrador.
    ///
    /// La lista es EXACTAMENTE lo que un remesero puede encontrarse, no todo lo que suena
    /// sensible: son las acciones que aparecen en las pantallas que su rol ve (Registro de
    /// Operacion, Gestion de Flota y Combustible). Prometer peticion para algo que el
    /// solicitante no tiene delante seria una regla muerta. Al ampliar lo que ve un rol,
    /// ampliar esta lista y cablear el comando con <see cref="SolicitudesDeCambio"/>.
    /// </summary>
    public static IReadOnlySet<string> Solicitables { get; } = new HashSet<string>
    {
        Permisos.Remesas.Anular,
        Permisos.Remesas.Recepcion,
        Permisos.Flota.Crear,
        Permisos.Flota.Editar,
        Permisos.Combustible.Anular,
        Permisos.Combustible.Recargar
    };

    public static bool EsSolicitable(string permiso) => Solicitables.Contains(permiso);
}
