using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos del inventario, con operaciones CRUD sobre el catálogo de artículos
/// (identificados por su <c>Codigo</c>). La implementa
/// una fuente EF Core; la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IInventoryDataSource : ICrudDataSource<InventoryItem, string>
{
}
