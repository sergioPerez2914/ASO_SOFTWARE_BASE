using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos del inventario, con operaciones CRUD sobre el catálogo de artículos
/// (identificados por su <c>Codigo</c>). Hoy la implementa un mock en memoria; mañana la
/// implementará un repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IInventoryDataSource : ICrudDataSource<InventoryItem, string>
{
}
