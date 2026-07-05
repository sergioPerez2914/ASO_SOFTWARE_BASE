using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services; // Namespace donde reside IInventoryDataSource

namespace ASO.Desktop.BD;

public class SqlInventoryDataSource : IInventoryDataSource
{
    public IEnumerable<InventoryItem> GetItems()
    {
        using var context = new AsoDbContext();
        // Carga los artículos directo desde la base de datos SQL
        return context.Inventarios.ToList();
    }
}