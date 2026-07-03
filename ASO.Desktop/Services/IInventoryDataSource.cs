using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos del inventario. Hoy la implementa un mock en memoria;
/// mañana la implementará un repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IInventoryDataSource
{
    IEnumerable<InventoryItem> GetItems();
}
