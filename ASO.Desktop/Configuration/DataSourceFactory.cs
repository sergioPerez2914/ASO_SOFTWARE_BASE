using ASO.Desktop.BD;
using ASO.Desktop.Services;

namespace ASO.Desktop.Configuration;

/// <summary>
/// Punto UNICO de composicion de las fuentes de datos.
///
/// Desde que se eliminaron los mocks solo hay un camino: SQL Server via EF Core.
/// La fabrica se mantiene porque sigue siendo el sitio donde los ViewModels resuelven
/// sus dependencias sin conocer la implementacion, y donde se da de alta una entidad nueva.
///
/// Las fuentes Sql son sin estado (abren un <see cref="AsoDbContext"/> por metodo), asi que
/// se cachean con <c>??=</c> por ahorro, no por correccion: el ambito de organizacion lo lee
/// el contexto al construirse, de modo que cambiar de nucleo no exige invalidar este cache.
/// </summary>
public static class DataSourceFactory
{
    private static IEmpleadoDataSource? _empleados;
    private static IInventoryDataSource? _inventario;
    private static IRemesaDataSource? _remesas;
    private static IFincaDataSource? _fincas;
    private static INucleoDataSource? _nucleos;
    private static IPersonalCampoDataSource? _personalCampo;
    private static IEventoOperacionDataSource? _eventosOperacion;
    private static IActivoFlotaDataSource? _activosFlota;
    private static IMantenimientoRegistroDataSource? _mantenimientos;
    private static IReglaMantenimientoDataSource? _reglasMantenimiento;
    private static ITarifaDataSource? _tarifas;
    private static ISalidaInventarioDataSource? _salidasInventario;
    private static IJornadaDataSource? _jornadas;
    private static ILiquidacionDataSource? _liquidaciones;
    private static IFacturaClienteDataSource? _facturasCliente;
    private static IProveedorDataSource? _proveedores;
    private static IFacturaProveedorDataSource? _facturasProveedor;
    private static ITanqueCombustibleDataSource? _tanquesCombustible;
    private static IValeCombustibleDataSource? _valesCombustible;
    private static IRecargaCombustibleDataSource? _recargasCombustible;
    private static IConceptoNominaDataSource? _conceptosNomina;
    private static IOrganizacionDataSource? _organizaciones;
    private static IUsuarioDataSource? _usuarios;
    private static IPermisoUsuarioDataSource? _permisosUsuario;
    private static IPeticionCambioDataSource? _peticiones;
    private static IAuthService? _auth;

    public static IEmpleadoDataSource CrearEmpleados() =>
        _empleados ??= new SqlEmpleadoDataSource();

    public static IInventoryDataSource CrearInventario() =>
        _inventario ??= new SqlInventoryDataSource();

    public static IRemesaDataSource CrearRemesas() =>
        _remesas ??= new SqlRemesaDataSource();

    public static IFincaDataSource CrearFincas() =>
        _fincas ??= new SqlFincaDataSource();

    public static INucleoDataSource CrearNucleos() =>
        _nucleos ??= new SqlNucleoDataSource();

    public static IPersonalCampoDataSource CrearPersonalCampo() =>
        _personalCampo ??= new SqlPersonalCampoDataSource();

    public static IEventoOperacionDataSource CrearEventosOperacion() =>
        _eventosOperacion ??= new SqlEventoOperacionDataSource();

    public static IActivoFlotaDataSource CrearActivosFlota() =>
        _activosFlota ??= new SqlActivoFlotaDataSource();

    public static IMantenimientoRegistroDataSource CrearMantenimientos() =>
        _mantenimientos ??= new SqlMantenimientoRegistroDataSource();

    public static IReglaMantenimientoDataSource CrearReglasMantenimiento() =>
        _reglasMantenimiento ??= new SqlReglaMantenimientoDataSource();

    public static ITarifaDataSource CrearTarifas() =>
        _tarifas ??= new SqlTarifaDataSource();

    public static ISalidaInventarioDataSource CrearSalidasInventario() =>
        _salidasInventario ??= new SqlSalidaInventarioDataSource();

    public static IJornadaDataSource CrearJornadas() =>
        _jornadas ??= new SqlJornadaDataSource();

    public static ILiquidacionDataSource CrearLiquidaciones() =>
        _liquidaciones ??= new SqlLiquidacionDataSource();

    public static IFacturaClienteDataSource CrearFacturasCliente() =>
        _facturasCliente ??= new SqlFacturaClienteDataSource();

    public static IProveedorDataSource CrearProveedores() =>
        _proveedores ??= new SqlProveedorDataSource();

    public static IFacturaProveedorDataSource CrearFacturasProveedor() =>
        _facturasProveedor ??= new SqlFacturaProveedorDataSource();

    public static ITanqueCombustibleDataSource CrearTanquesCombustible() =>
        _tanquesCombustible ??= new SqlTanqueCombustibleDataSource();

    public static IValeCombustibleDataSource CrearValesCombustible() =>
        _valesCombustible ??= new SqlValeCombustibleDataSource();

    public static IRecargaCombustibleDataSource CrearRecargasCombustible() =>
        _recargasCombustible ??= new SqlRecargaCombustibleDataSource();

    public static IConceptoNominaDataSource CrearConceptosNomina() =>
        _conceptosNomina ??= new SqlConceptoNominaDataSource();

    public static IOrganizacionDataSource CrearOrganizaciones() =>
        _organizaciones ??= new SqlOrganizacionDataSource();

    public static IUsuarioDataSource CrearUsuarios() =>
        _usuarios ??= new SqlUsuarioDataSource();

    public static IPermisoUsuarioDataSource CrearPermisosUsuario() =>
        _permisosUsuario ??= new SqlPermisoUsuarioDataSource();

    public static IPeticionCambioDataSource CrearPeticiones() =>
        _peticiones ??= new SqlPeticionCambioDataSource();

    public static IAuthService CrearAuth() =>
        _auth ??= new AuthService(CrearUsuarios());
}
