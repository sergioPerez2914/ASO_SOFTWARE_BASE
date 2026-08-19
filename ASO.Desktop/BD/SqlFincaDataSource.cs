using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlFincaDataSource : IFincaDataSource
{
    public IEnumerable<Finca> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Fincas.Include(f => f.Lotes).ThenInclude(l => l.Tablones).ToList();
    }

    public Finca? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Fincas.Include(f => f.Lotes).ThenInclude(l => l.Tablones)
            .FirstOrDefault(f => f.Id == id);
    }

    public Finca Add(Finca item)
    {
        using var context = new AsoDbContext();
        context.Fincas.Add(item);
        context.SaveChanges();
        return item;
    }

    /// <summary>
    /// Cada método abre un contexto nuevo (desconectado): un Update ingenuo de la cabecera
    /// no borraría los Lotes/Tablones quitados del lado cliente. Por eso se carga el grafo
    /// rastreado, se eliminan los hijos viejos y recién ahí se asignan los nuevos.
    /// </summary>
    public void Update(Finca item)
    {
        using var context = new AsoDbContext();
        var existente = context.Fincas.Include(f => f.Lotes).ThenInclude(l => l.Tablones)
            .First(f => f.Id == item.Id);

        context.RemoveRange(existente.Lotes);
        context.SaveChanges();

        context.Entry(existente).CurrentValues.SetValues(item);
        existente.Lotes = item.Lotes;
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var finca = context.Fincas.Include(f => f.Lotes).ThenInclude(l => l.Tablones)
            .FirstOrDefault(f => f.Id == id);
        if (finca != null)
        {
            context.Fincas.Remove(finca);
            context.SaveChanges();
        }
    }
}
