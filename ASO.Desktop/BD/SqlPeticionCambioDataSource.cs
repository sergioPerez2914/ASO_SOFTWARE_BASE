using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlPeticionCambioDataSource : IPeticionCambioDataSource
{
    public IEnumerable<PeticionCambio> GetAll()
    {
        using var context = new AsoDbContext();
        return context.PeticionesCambio
            .OrderByDescending(p => p.SolicitadoEn)
            .ToList();
    }

    public PeticionCambio? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.PeticionesCambio.Find(id);
    }

    public PeticionCambio Add(PeticionCambio item)
    {
        using var context = new AsoDbContext();
        context.PeticionesCambio.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(PeticionCambio item)
    {
        using var context = new AsoDbContext();
        context.PeticionesCambio.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var peticion = context.PeticionesCambio.Find(id);
        if (peticion != null)
        {
            context.PeticionesCambio.Remove(peticion);
            context.SaveChanges();
        }
    }
}
