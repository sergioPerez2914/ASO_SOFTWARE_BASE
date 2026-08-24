using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlRecargaCombustibleDataSource : SqlCrudDataSource<RecargaCombustible, int>, IRecargaCombustibleDataSource
{
    public IEnumerable<RecargaCombustible> GetByStockCombustible(int stockCombustibleId)
        => Consultar(q => q.Where(e => e.StockCombustibleId == stockCombustibleId));
}
