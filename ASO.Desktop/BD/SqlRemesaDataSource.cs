using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlRemesaDataSource : IRemesaDataSource
{
    public IEnumerable<Remesa> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Remesas.ToList();
    }

    public Remesa? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Remesas.Find(id);
    }

    public Remesa Add(Remesa item)
    {
        using var context = new AsoDbContext();
        context.Remesas.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(Remesa item)
    {
        using var context = new AsoDbContext();
        context.Remesas.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var remesa = context.Remesas.Find(id);
        if (remesa != null)
        {
            context.Remesas.Remove(remesa);
            context.SaveChanges();
        }
    }
}
