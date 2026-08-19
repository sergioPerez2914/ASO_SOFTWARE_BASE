using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlEventoOperacionDataSource : IEventoOperacionDataSource
{
    public IEnumerable<EventoOperacion> GetAll()
    {
        using var context = new AsoDbContext();
        return context.EventosOperacion.ToList();
    }

    public EventoOperacion? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.EventosOperacion.Find(id);
    }

    public IEnumerable<EventoOperacion> GetByRemesa(int remesaId)
    {
        using var context = new AsoDbContext();
        return context.EventosOperacion.Where(e => e.RemesaId == remesaId).ToList();
    }

    public EventoOperacion Add(EventoOperacion item)
    {
        using var context = new AsoDbContext();
        context.EventosOperacion.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(EventoOperacion item)
    {
        using var context = new AsoDbContext();
        context.EventosOperacion.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var evento = context.EventosOperacion.Find(id);
        if (evento != null)
        {
            context.EventosOperacion.Remove(evento);
            context.SaveChanges();
        }
    }
}
