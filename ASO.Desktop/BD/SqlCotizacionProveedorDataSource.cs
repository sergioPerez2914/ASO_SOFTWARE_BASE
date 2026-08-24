using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlCotizacionProveedorDataSource : SqlCrudDataSource<CotizacionProveedor, int>, ICotizacionProveedorDataSource
{
    public IEnumerable<CotizacionProveedor> GetByRequisicion(int requisicionId)
        => Consultar(q => q.Where(c => c.RequisicionId == requisicionId));
}
