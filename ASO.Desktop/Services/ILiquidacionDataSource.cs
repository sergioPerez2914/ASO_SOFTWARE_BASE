using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Liquidaciones de nómina. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface ILiquidacionDataSource : ICrudDataSource<Liquidacion, int>
{
}
