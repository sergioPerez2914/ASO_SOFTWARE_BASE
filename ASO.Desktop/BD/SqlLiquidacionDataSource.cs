using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

/// <summary>La liquidacion se guarda entera con sus lineas de concepto.</summary>
public class SqlLiquidacionDataSource : SqlAgregadoDataSource<Liquidacion, int>, ILiquidacionDataSource
{
    protected override IQueryable<Liquidacion> Incluir(IQueryable<Liquidacion> consulta)
        => consulta.Include(l => l.Lineas);

    protected override Expression<Func<Liquidacion, bool>> PorId(int id) => l => l.Id == id;

    protected override IEnumerable<object> HijosDe(Liquidacion raiz) => raiz.Lineas;

    /// <summary>
    /// Ademas de las lineas hay que copiar RemesaIdsIncluidas: SetValues no la trae porque
    /// no es una columna escalar cualquiera, y sin ella la liquidacion perderia su trazabilidad.
    /// </summary>
    protected override void CopiarHijos(Liquidacion destino, Liquidacion origen)
    {
        destino.Lineas = origen.Lineas;
        destino.RemesaIdsIncluidas = origen.RemesaIdsIncluidas;
    }
}
