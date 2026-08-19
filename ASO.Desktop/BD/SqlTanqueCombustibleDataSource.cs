using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlTanqueCombustibleDataSource : ITanqueCombustibleDataSource
{
    public IEnumerable<TanqueCombustible> GetAll()
    {
        using var context = new AsoDbContext();
        return context.TanquesCombustible.ToList();
    }

    public TanqueCombustible? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.TanquesCombustible.Find(id);
    }

    public TanqueCombustible Add(TanqueCombustible item)
    {
        using var context = new AsoDbContext();
        context.TanquesCombustible.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(TanqueCombustible item)
    {
        using var context = new AsoDbContext();
        context.TanquesCombustible.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var tanque = context.TanquesCombustible.Find(id);
        if (tanque != null)
        {
            context.TanquesCombustible.Remove(tanque);
            context.SaveChanges();
        }
    }
}
