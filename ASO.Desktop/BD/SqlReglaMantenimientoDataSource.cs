using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlReglaMantenimientoDataSource : IReglaMantenimientoDataSource
{
    public IEnumerable<ReglaMantenimiento> GetAll()
    {
        using var context = new AsoDbContext();
        return context.ReglasMantenimiento.ToList();
    }

    public ReglaMantenimiento? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.ReglasMantenimiento.Find(id);
    }

    public IEnumerable<ReglaMantenimiento> GetByTipo(TipoActivo tipo)
    {
        using var context = new AsoDbContext();
        return context.ReglasMantenimiento.Where(r => r.Tipo == tipo).ToList();
    }

    public ReglaMantenimiento Add(ReglaMantenimiento item)
    {
        using var context = new AsoDbContext();
        context.ReglasMantenimiento.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(ReglaMantenimiento item)
    {
        using var context = new AsoDbContext();
        context.ReglasMantenimiento.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var regla = context.ReglasMantenimiento.Find(id);
        if (regla != null)
        {
            context.ReglasMantenimiento.Remove(regla);
            context.SaveChanges();
        }
    }
}
