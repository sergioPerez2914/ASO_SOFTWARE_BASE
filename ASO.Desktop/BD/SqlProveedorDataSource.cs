using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlProveedorDataSource : IProveedorDataSource
{
    public IEnumerable<Proveedor> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Proveedores.ToList();
    }

    public Proveedor? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Proveedores.Find(id);
    }

    public Proveedor Add(Proveedor item)
    {
        using var context = new AsoDbContext();
        context.Proveedores.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(Proveedor item)
    {
        using var context = new AsoDbContext();
        context.Proveedores.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var proveedor = context.Proveedores.Find(id);
        if (proveedor != null)
        {
            context.Proveedores.Remove(proveedor);
            context.SaveChanges();
        }
    }
}
