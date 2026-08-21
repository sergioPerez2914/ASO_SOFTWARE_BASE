using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlFacturaProveedorDataSource : SqlCrudDataSource<FacturaProveedor, int>, IFacturaProveedorDataSource
{
    public IEnumerable<FacturaProveedor> GetByProveedor(int proveedorId)
        => Consultar(q => q.Where(e => e.ProveedorId == proveedorId));
}
