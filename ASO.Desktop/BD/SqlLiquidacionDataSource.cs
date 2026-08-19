using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlLiquidacionDataSource : ILiquidacionDataSource
{
    public IEnumerable<Liquidacion> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Liquidaciones.Include(l => l.Lineas).ToList();
    }

    public Liquidacion? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Liquidaciones.Include(l => l.Lineas).FirstOrDefault(l => l.Id == id);
    }

    public Liquidacion Add(Liquidacion item)
    {
        using var context = new AsoDbContext();
        context.Liquidaciones.Add(item);
        context.SaveChanges();
        return item;
    }

    /// <summary>
    /// Cada método abre un contexto nuevo (desconectado): un Update ingenuo de la cabecera
    /// no borraría las líneas quitadas del lado cliente. Por eso se carga el grafo rastreado,
    /// se eliminan las líneas viejas y recién ahí se asignan las nuevas.
    /// </summary>
    public void Update(Liquidacion item)
    {
        using var context = new AsoDbContext();
        var existente = context.Liquidaciones.Include(l => l.Lineas).First(l => l.Id == item.Id);

        context.RemoveRange(existente.Lineas);
        context.SaveChanges();

        context.Entry(existente).CurrentValues.SetValues(item);
        existente.Lineas = item.Lineas;
        existente.RemesaIdsIncluidas = item.RemesaIdsIncluidas;
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var liquidacion = context.Liquidaciones.Include(l => l.Lineas).FirstOrDefault(l => l.Id == id);
        if (liquidacion != null)
        {
            context.Liquidaciones.Remove(liquidacion);
            context.SaveChanges();
        }
    }
}
