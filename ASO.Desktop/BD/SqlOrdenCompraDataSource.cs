using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

/// <summary>La orden de compra se guarda entera con sus líneas.</summary>
public class SqlOrdenCompraDataSource : SqlAgregadoDataSource<OrdenCompra, int>, IOrdenCompraDataSource
{
    protected override IQueryable<OrdenCompra> Incluir(IQueryable<OrdenCompra> consulta)
        => consulta.Include(o => o.Lineas);

    protected override Expression<Func<OrdenCompra, bool>> PorId(int id) => o => o.Id == id;

    protected override IEnumerable<object> HijosDe(OrdenCompra raiz) => raiz.Lineas;

    protected override void CopiarHijos(OrdenCompra destino, OrdenCompra origen)
        => destino.Lineas = origen.Lineas;
}
