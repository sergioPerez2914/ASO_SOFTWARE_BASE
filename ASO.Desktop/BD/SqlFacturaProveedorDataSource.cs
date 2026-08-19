using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlFacturaProveedorDataSource : IFacturaProveedorDataSource
{
    public IEnumerable<FacturaProveedor> GetAll()
    {
        using var context = new AsoDbContext();
        return context.FacturasProveedor.ToList();
    }

    public FacturaProveedor? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.FacturasProveedor.Find(id);
    }

    public IEnumerable<FacturaProveedor> GetByProveedor(int proveedorId)
    {
        using var context = new AsoDbContext();
        return context.FacturasProveedor.Where(f => f.ProveedorId == proveedorId).ToList();
    }

    public FacturaProveedor Add(FacturaProveedor item)
    {
        using var context = new AsoDbContext();
        context.FacturasProveedor.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(FacturaProveedor item)
    {
        using var context = new AsoDbContext();
        context.FacturasProveedor.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var factura = context.FacturasProveedor.Find(id);
        if (factura != null)
        {
            context.FacturasProveedor.Remove(factura);
            context.SaveChanges();
        }
    }
}
