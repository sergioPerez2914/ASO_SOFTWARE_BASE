using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlPersonalCampoDataSource : IPersonalCampoDataSource
{
    public IEnumerable<PersonalCampo> GetAll()
    {
        using var context = new AsoDbContext();
        return context.PersonalCampo.ToList();
    }

    public PersonalCampo? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.PersonalCampo.Find(id);
    }

    public PersonalCampo Add(PersonalCampo item)
    {
        using var context = new AsoDbContext();
        context.PersonalCampo.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(PersonalCampo item)
    {
        using var context = new AsoDbContext();
        context.PersonalCampo.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var persona = context.PersonalCampo.Find(id);
        if (persona != null)
        {
            context.PersonalCampo.Remove(persona);
            context.SaveChanges();
        }
    }
}
