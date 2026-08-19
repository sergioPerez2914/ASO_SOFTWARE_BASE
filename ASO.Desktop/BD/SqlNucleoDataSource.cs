using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlNucleoDataSource : INucleoDataSource
{
    public IEnumerable<Nucleo> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Nucleos.ToList();
    }

    public Nucleo? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Nucleos.Find(id);
    }

    public Nucleo Add(Nucleo item)
    {
        using var context = new AsoDbContext();
        context.Nucleos.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(Nucleo item)
    {
        using var context = new AsoDbContext();
        context.Nucleos.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var nucleo = context.Nucleos.Find(id);
        if (nucleo != null)
        {
            context.Nucleos.Remove(nucleo);
            context.SaveChanges();
        }
    }
}
