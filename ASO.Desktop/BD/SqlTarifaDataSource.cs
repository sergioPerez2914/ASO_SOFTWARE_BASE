using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlTarifaDataSource : ITarifaDataSource
{
    public IEnumerable<Tarifa> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Tarifas.ToList();
    }

    public Tarifa? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Tarifas.Find(id);
    }

    // RigeEn() es un método C# (no traducible a SQL); se filtra en memoria como hace el mock.
    public IEnumerable<Tarifa> GetVigentes(DateTime fecha)
    {
        using var context = new AsoDbContext();
        return context.Tarifas.ToList().Where(t => t.Activa && t.RigeEn(fecha)).ToList();
    }

    public Tarifa Add(Tarifa item)
    {
        using var context = new AsoDbContext();
        context.Tarifas.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(Tarifa item)
    {
        using var context = new AsoDbContext();
        context.Tarifas.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var tarifa = context.Tarifas.Find(id);
        if (tarifa != null)
        {
            context.Tarifas.Remove(tarifa);
            context.SaveChanges();
        }
    }
}
