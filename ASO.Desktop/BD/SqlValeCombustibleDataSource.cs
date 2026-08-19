using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlValeCombustibleDataSource : IValeCombustibleDataSource
{
    public IEnumerable<ValeCombustible> GetAll()
    {
        using var context = new AsoDbContext();
        return context.ValesCombustible.ToList();
    }

    public ValeCombustible? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.ValesCombustible.Find(id);
    }

    public IEnumerable<ValeCombustible> GetByActivo(int activoId)
    {
        using var context = new AsoDbContext();
        return context.ValesCombustible.Where(v => v.ActivoId == activoId).ToList();
    }

    public ValeCombustible Add(ValeCombustible item)
    {
        using var context = new AsoDbContext();
        context.ValesCombustible.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(ValeCombustible item)
    {
        using var context = new AsoDbContext();
        context.ValesCombustible.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var vale = context.ValesCombustible.Find(id);
        if (vale != null)
        {
            context.ValesCombustible.Remove(vale);
            context.SaveChanges();
        }
    }
}
