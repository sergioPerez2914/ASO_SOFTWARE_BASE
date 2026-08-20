using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Vales de combustible despachados. La implementa
/// una fuente EF Core; la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IValeCombustibleDataSource : ICrudDataSource<ValeCombustible, int>
{
    /// <summary>Vales de un activo: con ellos se calcula su consumo y su historial de uso.</summary>
    IEnumerable<ValeCombustible> GetByActivo(int activoId);
}
