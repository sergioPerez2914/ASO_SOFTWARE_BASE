using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlMantenimientoRegistroDataSource : SqlCrudDataSource<MantenimientoRegistro, int>, IMantenimientoRegistroDataSource
{
    public IEnumerable<MantenimientoRegistro> GetByActivo(int activoId)
        => Consultar(q => q.Where(e => e.ActivoId == activoId));
}
