using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Depósitos de combustible. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface ITanqueCombustibleDataSource : ICrudDataSource<TanqueCombustible, int>
{
}
