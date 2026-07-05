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
}
