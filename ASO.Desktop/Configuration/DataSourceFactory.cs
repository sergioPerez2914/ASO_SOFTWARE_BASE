using ASO.Desktop.BD;
using ASO.Desktop.Services;

namespace ASO.Desktop.Configuration;

/// <summary>
/// Punto UNICO de composicion de las fuentes de datos.
/// Segun el flag AppConfig.UseMock decide si entrega implementaciones mock
/// (en memoria, sin BD) o SQL Server real. Alternar entre desarrollo sin BD
/// y produccion es cambiar "UseMock" en appsettings(.local).json: ni los
/// ViewModels ni las Vistas se enteran.
/// </summary>
public static class DataSourceFactory
{
    // Empleados e inventario conmutan Mock/SQL segun el flag, pero tambien se cachean:
    // varios submodulos comparten estas fuentes y con mocks (sin BD) cada instancia nueva
    // perderia lo capturado al navegar entre pantallas.
    private static IEmpleadoDataSource? _empleados;
    private static IInventoryDataSource? _inventario;

    public static IEmpleadoDataSource CrearEmpleados() =>
        _empleados ??= AppConfig.UseMock
            ? new MockEmpleadoDataSource()
            : new SqlEmpleadoDataSource();

    public static IInventoryDataSource CrearInventario() =>
        _inventario ??= AppConfig.UseMock
            ? new MockInventoryDataSource()
            : new SqlInventoryDataSource();

    // Las fuentes de abajo se cachean: los mocks guardan su estado en memoria y cada
    // navegación crea un ViewModel nuevo, así que devolver una instancia distinta cada vez
    // haría perder lo capturado al salir y volver a entrar al submodulo.
    // TODO socio BD: agregar aqui las implementaciones EF (EfRemesaDataSource, etc.).
    private static IRemesaDataSource? _remesas;
    private static IFincaDataSource? _fincas;
    private static INucleoDataSource? _nucleos;
    private static IPersonalCampoDataSource? _personalCampo;
    private static IVehiculoDataSource? _vehiculos;
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

    public static IRemesaDataSource CrearRemesas() =>
        _remesas ??= new MockRemesaDataSource();

    public static IFincaDataSource CrearFincas() =>
        _fincas ??= new MockFincaDataSource();

    public static INucleoDataSource CrearNucleos() =>
        _nucleos ??= new MockNucleoDataSource();

    public static IPersonalCampoDataSource CrearPersonalCampo() =>
        _personalCampo ??= new MockPersonalCampoDataSource();

    // Los vehículos del combo de remesas son una proyección del catálogo único de flota.
    public static IVehiculoDataSource CrearVehiculos() =>
        _vehiculos ??= new VehiculoDataSourceAdapter(CrearActivosFlota());

    public static IEventoOperacionDataSource CrearEventosOperacion() =>
        _eventosOperacion ??= new MockEventoOperacionDataSource();

    public static IActivoFlotaDataSource CrearActivosFlota() =>
        _activosFlota ??= new MockActivoFlotaDataSource();

    public static IMantenimientoRegistroDataSource CrearMantenimientos() =>
        _mantenimientos ??= new MockMantenimientoRegistroDataSource();

    public static IReglaMantenimientoDataSource CrearReglasMantenimiento() =>
        _reglasMantenimiento ??= new MockReglaMantenimientoDataSource();

    public static ITarifaDataSource CrearTarifas() =>
        _tarifas ??= new MockTarifaDataSource();

    public static ISalidaInventarioDataSource CrearSalidasInventario() =>
        _salidasInventario ??= new MockSalidaInventarioDataSource();

    public static IJornadaDataSource CrearJornadas() =>
        _jornadas ??= new MockJornadaDataSource();

    public static ILiquidacionDataSource CrearLiquidaciones() =>
        _liquidaciones ??= new MockLiquidacionDataSource();

    public static IFacturaClienteDataSource CrearFacturasCliente() =>
        _facturasCliente ??= new MockFacturaClienteDataSource();

    public static IProveedorDataSource CrearProveedores() =>
        _proveedores ??= new MockProveedorDataSource();

    public static IFacturaProveedorDataSource CrearFacturasProveedor() =>
        _facturasProveedor ??= new MockFacturaProveedorDataSource();

    public static ITanqueCombustibleDataSource CrearTanquesCombustible() =>
        _tanquesCombustible ??= new MockTanqueCombustibleDataSource();

    public static IValeCombustibleDataSource CrearValesCombustible() =>
        _valesCombustible ??= new MockValeCombustibleDataSource();

    public static IRecargaCombustibleDataSource CrearRecargasCombustible() =>
        _recargasCombustible ??= new MockRecargaCombustibleDataSource();

    public static IConceptoNominaDataSource CrearConceptosNomina() =>
        _conceptosNomina ??= new MockConceptoNominaDataSource();
}
