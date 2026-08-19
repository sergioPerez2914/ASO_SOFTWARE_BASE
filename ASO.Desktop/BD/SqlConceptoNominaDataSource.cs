using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlConceptoNominaDataSource : IConceptoNominaDataSource
{
    public IEnumerable<ConceptoNomina> GetAll()
    {
        using var context = new AsoDbContext();
        return context.ConceptosNomina.ToList();
    }

    public ConceptoNomina? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.ConceptosNomina.Find(id);
    }

    public ConceptoNomina Add(ConceptoNomina item)
    {
        using var context = new AsoDbContext();
        context.ConceptosNomina.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(ConceptoNomina item)
    {
        using var context = new AsoDbContext();
        context.ConceptosNomina.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var concepto = context.ConceptosNomina.Find(id);
        if (concepto != null)
        {
            context.ConceptosNomina.Remove(concepto);
            context.SaveChanges();
        }
    }
}
