using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlRecargaCombustibleDataSource : IRecargaCombustibleDataSource
{
    public IEnumerable<RecargaCombustible> GetAll()
    {
        using var context = new AsoDbContext();
        return context.RecargasCombustible.ToList();
    }

    public RecargaCombustible? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.RecargasCombustible.Find(id);
    }

    public IEnumerable<RecargaCombustible> GetByTanque(int tanqueId)
    {
        using var context = new AsoDbContext();
        return context.RecargasCombustible.Where(r => r.TanqueId == tanqueId).ToList();
    }

    public RecargaCombustible Add(RecargaCombustible item)
    {
        using var context = new AsoDbContext();
        context.RecargasCombustible.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(RecargaCombustible item)
    {
        using var context = new AsoDbContext();
        context.RecargasCombustible.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var recarga = context.RecargasCombustible.Find(id);
        if (recarga != null)
        {
            context.RecargasCombustible.Remove(recarga);
            context.SaveChanges();
        }
    }
}
