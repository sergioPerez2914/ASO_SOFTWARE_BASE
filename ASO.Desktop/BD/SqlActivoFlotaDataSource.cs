using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlActivoFlotaDataSource : IActivoFlotaDataSource
{
    public IEnumerable<ActivoFlota> GetAll()
    {
        using var context = new AsoDbContext();
        return context.ActivosFlota.ToList();
    }

    public ActivoFlota? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.ActivosFlota.Find(id);
    }

    public ActivoFlota Add(ActivoFlota item)
    {
        using var context = new AsoDbContext();
        context.ActivosFlota.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(ActivoFlota item)
    {
        using var context = new AsoDbContext();
        context.ActivosFlota.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var activo = context.ActivosFlota.Find(id);
        if (activo != null)
        {
            context.ActivosFlota.Remove(activo);
            context.SaveChanges();
        }
    }
}
