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
    /// los modulos fijados (Inicio, Peticiones, Administracion) y todos los submodulos.
    ///
    /// Los cinco modulos del negocio NO entran: su visibilidad la deciden sus submodulos
    /// (ver <see cref="NavegacionPermitida"/>), asi que "Ver.Finanzas" no se lee nunca.
    /// Ofrecerlo en el desplegable de ajustes solo serviria para que alguien lo conceda y no
    /// pase nada.
    /// </summary>
    private static readonly HashSet<string> _navegacion =
        ModuloCatalogo.Fijados.Select(m => m.Permiso)
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
        Permisos.Ver("Operaciones.Registro"),
        Permisos.Ver("Operaciones.Seguimiento"),
        Permisos.Ver("Flota.Gestion"),
        Permisos.Ver("Flota.Mantenimiento"),
        Permisos.Ver("Flota.Telemetria"),
        Permisos.Ver("Inventario.Combustible"),
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

        Permisos.Peticiones.Solicitar
    ];

    private static readonly HashSet<string> _administrador =
        Todos.Where(p => !_soloDesarrollador.Contains(p)).ToHashSet();

    /// <summary>Conjunto base del rol, antes de los ajustes por usuario.</summary>
    public static IReadOnlySet<string> Base(Rol rol) => rol switch
    {
        Rol.Remesero => _remesero,
        Rol.AdministradorNucleo => _administrador,
        _ => Todos
    };

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
