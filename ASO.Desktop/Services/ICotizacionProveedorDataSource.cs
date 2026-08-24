using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

public interface ICotizacionProveedorDataSource : ICrudDataSource<CotizacionProveedor, int>
{
    IEnumerable<CotizacionProveedor> GetByRequisicion(int requisicionId);
}
