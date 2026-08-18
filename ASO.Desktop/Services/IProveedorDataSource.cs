using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Maestro de proveedores. Hoy la implementa un mock en memoria; mañana la implementará un
/// repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IProveedorDataSource : ICrudDataSource<Proveedor, int>
{
}
