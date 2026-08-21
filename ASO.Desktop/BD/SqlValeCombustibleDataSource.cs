using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlValeCombustibleDataSource : SqlCrudDataSource<ValeCombustible, int>, IValeCombustibleDataSource
{
    public IEnumerable<ValeCombustible> GetByActivo(int activoId)
        => Consultar(q => q.Where(e => e.ActivoId == activoId));
}
