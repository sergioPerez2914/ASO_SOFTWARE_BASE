using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Existencias de combustible/aceite por producto. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IStockCombustibleDataSource : ICrudDataSource<StockCombustible, int>
{
}
