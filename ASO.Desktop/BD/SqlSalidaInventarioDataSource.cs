using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlSalidaInventarioDataSource : ISalidaInventarioDataSource
{
    public IEnumerable<SalidaInventario> GetAll()
    {
        using var context = new AsoDbContext();
        return context.SalidasInventario.ToList();
    }

    public SalidaInventario? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.SalidasInventario.Find(id);
    }

    public IEnumerable<SalidaInventario> GetByMantenimiento(int mantenimientoId)
    {
        using var context = new AsoDbContext();
        return context.SalidasInventario.Where(s => s.MantenimientoId == mantenimientoId).ToList();
    }

    public IEnumerable<SalidaInventario> GetByActivo(int activoId)
    {
        using var context = new AsoDbContext();
        return context.SalidasInventario.Where(s => s.ActivoId == activoId).ToList();
    }

    public SalidaInventario Add(SalidaInventario item)
    {
        using var context = new AsoDbContext();
        context.SalidasInventario.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(SalidaInventario item)
    {
        using var context = new AsoDbContext();
        context.SalidasInventario.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var salida = context.SalidasInventario.Find(id);
        if (salida != null)
        {
            context.SalidasInventario.Remove(salida);
            context.SaveChanges();
        }
    }
}
