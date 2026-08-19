using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlMantenimientoRegistroDataSource : IMantenimientoRegistroDataSource
{
    public IEnumerable<MantenimientoRegistro> GetAll()
    {
        using var context = new AsoDbContext();
        return context.MantenimientoRegistros.ToList();
    }

    public MantenimientoRegistro? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.MantenimientoRegistros.Find(id);
    }

    public IEnumerable<MantenimientoRegistro> GetByActivo(int activoId)
    {
        using var context = new AsoDbContext();
        return context.MantenimientoRegistros.Where(m => m.ActivoId == activoId).ToList();
    }

    public MantenimientoRegistro Add(MantenimientoRegistro item)
    {
        using var context = new AsoDbContext();
        context.MantenimientoRegistros.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(MantenimientoRegistro item)
    {
        using var context = new AsoDbContext();
        context.MantenimientoRegistros.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var registro = context.MantenimientoRegistros.Find(id);
        if (registro != null)
        {
            context.MantenimientoRegistros.Remove(registro);
            context.SaveChanges();
        }
    }
}
