using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Recargas de cisterna. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IRecargaCombustibleDataSource : ICrudDataSource<RecargaCombustible, int>
{
    IEnumerable<RecargaCombustible> GetByTanque(int tanqueId);
}
