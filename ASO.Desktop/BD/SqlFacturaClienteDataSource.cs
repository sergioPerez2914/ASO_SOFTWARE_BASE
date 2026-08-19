using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlFacturaClienteDataSource : IFacturaClienteDataSource
{
    public IEnumerable<FacturaCliente> GetAll()
    {
        using var context = new AsoDbContext();
        return context.FacturasCliente.Include(f => f.Lineas).ToList();
    }

    public FacturaCliente? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.FacturasCliente.Include(f => f.Lineas).FirstOrDefault(f => f.Id == id);
    }

    public FacturaCliente Add(FacturaCliente item)
    {
        using var context = new AsoDbContext();
        context.FacturasCliente.Add(item);
        context.SaveChanges();
        return item;
    }

    /// <summary>
    /// Cada método abre un contexto nuevo (desconectado): un Update ingenuo de la cabecera
    /// no borraría las líneas quitadas del lado cliente. Por eso se carga el grafo rastreado,
    /// se eliminan las líneas viejas y recién ahí se asignan las nuevas.
    /// </summary>
    public void Update(FacturaCliente item)
    {
        using var context = new AsoDbContext();
        var existente = context.FacturasCliente.Include(f => f.Lineas).First(f => f.Id == item.Id);

        context.RemoveRange(existente.Lineas);
        context.SaveChanges();

        context.Entry(existente).CurrentValues.SetValues(item);
        existente.Lineas = item.Lineas;
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var factura = context.FacturasCliente.Include(f => f.Lineas).FirstOrDefault(f => f.Id == id);
        if (factura != null)
        {
            context.FacturasCliente.Remove(factura);
            context.SaveChanges();
        }
    }
}
