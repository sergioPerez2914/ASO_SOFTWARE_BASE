using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Depósitos de combustible. Hoy la implementa un mock en memoria; mañana la implementará un
/// repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface ITanqueCombustibleDataSource : ICrudDataSource<TanqueCombustible, int>
{
}
