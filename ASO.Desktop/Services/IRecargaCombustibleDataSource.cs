using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Recargas de cisterna. Hoy la implementa un mock en memoria; mañana la implementará un
/// repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IRecargaCombustibleDataSource : ICrudDataSource<RecargaCombustible, int>
{
    IEnumerable<RecargaCombustible> GetByTanque(int tanqueId);
}
