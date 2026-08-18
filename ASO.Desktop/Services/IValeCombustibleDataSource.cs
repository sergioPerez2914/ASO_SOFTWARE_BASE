using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Vales de combustible despachados. Hoy la implementa un mock en memoria; mañana la
/// implementará un repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IValeCombustibleDataSource : ICrudDataSource<ValeCombustible, int>
{
    /// <summary>Vales de un activo: con ellos se calcula su consumo y su historial de uso.</summary>
    IEnumerable<ValeCombustible> GetByActivo(int activoId);
}
