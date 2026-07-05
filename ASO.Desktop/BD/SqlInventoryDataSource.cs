using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services; // Namespace donde reside IInventoryDataSource

namespace ASO.Desktop.BD;

public class SqlInventoryDataSource : IInventoryDataSource
{
    // 1. OBTENER TODOS
    public IEnumerable<InventoryItem> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Inventarios.ToList();
    }

    // 2. OBTENER POR CODIGO (clave primaria)
    public InventoryItem? GetById(string id)
    {
        using var context = new AsoDbContext();
        return context.Inventarios.Find(id);
    }

    // 3. AGREGAR (el Codigo lo escribe el usuario; no es autonumerico)
    public InventoryItem Add(InventoryItem item)
    {
        using var context = new AsoDbContext();
        context.Inventarios.Add(item);
        context.SaveChanges();
        return item;
    }

    // 4. ACTUALIZAR
    public void Update(InventoryItem item)
    {
        using var context = new AsoDbContext();
        context.Inventarios.Update(item);
        context.SaveChanges();
    }

    // 5. ELIMINAR
    public void Delete(string id)
    {
        using var context = new AsoDbContext();
        var articulo = context.Inventarios.Find(id);
        if (articulo != null)
        {
            context.Inventarios.Remove(articulo);
            context.SaveChanges();
        }
    }
}
