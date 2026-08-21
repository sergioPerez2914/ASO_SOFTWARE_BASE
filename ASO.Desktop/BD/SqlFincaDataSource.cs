using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

/// <summary>La finca se guarda entera con sus lotes y los tablones de cada lote.</summary>
public class SqlFincaDataSource : SqlAgregadoDataSource<Finca, int>, IFincaDataSource
{
    protected override IQueryable<Finca> Incluir(IQueryable<Finca> consulta)
        => consulta.Include(f => f.Lotes).ThenInclude(l => l.Tablones);

    protected override Expression<Func<Finca, bool>> PorId(int id) => f => f.Id == id;

    protected override IEnumerable<object> HijosDe(Finca raiz) => raiz.Lotes;

    protected override void CopiarHijos(Finca destino, Finca origen) => destino.Lotes = origen.Lotes;
}
