using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

/// <summary>La recepción de mercancía se guarda entera con sus líneas.</summary>
public class SqlRecepcionMercanciaDataSource : SqlAgregadoDataSource<RecepcionMercancia, int>, IRecepcionMercanciaDataSource
{
    protected override IQueryable<RecepcionMercancia> Incluir(IQueryable<RecepcionMercancia> consulta)
        => consulta.Include(r => r.Lineas);

    protected override Expression<Func<RecepcionMercancia, bool>> PorId(int id) => r => r.Id == id;

    protected override IEnumerable<object> HijosDe(RecepcionMercancia raiz) => raiz.Lineas;

    protected override void CopiarHijos(RecepcionMercancia destino, RecepcionMercancia origen)
        => destino.Lineas = origen.Lineas;

    public IEnumerable<RecepcionMercancia> GetByOrdenCompra(int ordenCompraId)
        => Consultar(q => q.Where(r => r.OrdenCompraId == ordenCompraId));
}
