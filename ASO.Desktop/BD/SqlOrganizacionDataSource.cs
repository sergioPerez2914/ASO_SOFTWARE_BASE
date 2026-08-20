using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlOrganizacionDataSource : IOrganizacionDataSource
{
    public IEnumerable<Organizacion> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Organizaciones.OrderBy(o => o.Nombre).ToList();
    }

    public Organizacion? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Organizaciones.Find(id);
    }

    public Organizacion Add(Organizacion item)
    {
        using var context = new AsoDbContext();
        context.Organizaciones.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(Organizacion item)
    {
        using var context = new AsoDbContext();
        context.Organizaciones.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var organizacion = context.Organizaciones.Find(id);
        if (organizacion != null)
        {
            context.Organizaciones.Remove(organizacion);
            context.SaveChanges();
        }
    }
}
