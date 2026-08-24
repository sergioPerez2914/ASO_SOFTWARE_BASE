using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

/// <summary>La requisición se guarda entera con sus líneas.</summary>
public class SqlRequisicionDataSource : SqlAgregadoDataSource<Requisicion, int>, IRequisicionDataSource
{
    protected override IQueryable<Requisicion> Incluir(IQueryable<Requisicion> consulta)
        => consulta.Include(r => r.Lineas);

    protected override Expression<Func<Requisicion, bool>> PorId(int id) => r => r.Id == id;

    protected override IEnumerable<object> HijosDe(Requisicion raiz) => raiz.Lineas;

    protected override void CopiarHijos(Requisicion destino, Requisicion origen)
        => destino.Lineas = origen.Lineas;
}
