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
    public static IEmpleadoDataSource CrearEmpleados() =>
        AppConfig.UseMock
            ? new MockEmpleadoDataSource()
            : new SqlEmpleadoDataSource();

    public static IInventoryDataSource CrearInventario() =>
        AppConfig.UseMock
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
}
