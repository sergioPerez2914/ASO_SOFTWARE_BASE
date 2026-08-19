using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlJornadaDataSource : IJornadaDataSource
{
    public IEnumerable<JornadaTrabajo> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Jornadas.ToList();
    }

    public JornadaTrabajo? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Jornadas.Find(id);
    }

    public IEnumerable<JornadaTrabajo> GetByPeriodo(DateTime desde, DateTime hasta)
    {
        using var context = new AsoDbContext();
        return context.Jornadas
            .Where(j => j.HoraEntrada >= desde && j.HoraEntrada <= hasta)
            .ToList();
    }

    public IEnumerable<JornadaTrabajo> GetByPersona(TipoPersonal tipo, int personaId)
    {
        using var context = new AsoDbContext();
        return context.Jornadas
            .Where(j => j.TipoPersonal == tipo && j.PersonaId == personaId)
            .ToList();
    }

    public JornadaTrabajo Add(JornadaTrabajo item)
    {
        using var context = new AsoDbContext();
        context.Jornadas.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(JornadaTrabajo item)
    {
        using var context = new AsoDbContext();
        context.Jornadas.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var jornada = context.Jornadas.Find(id);
        if (jornada != null)
        {
            context.Jornadas.Remove(jornada);
            context.SaveChanges();
        }
    }
}
