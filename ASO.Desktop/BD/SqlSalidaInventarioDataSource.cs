using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlSalidaInventarioDataSource : SqlCrudDataSource<SalidaInventario, int>, ISalidaInventarioDataSource
{
    public IEnumerable<SalidaInventario> GetByMantenimiento(int mantenimientoId)
        => Consultar(q => q.Where(s => s.MantenimientoId == mantenimientoId));

    public IEnumerable<SalidaInventario> GetByActivo(int activoId)
        => Consultar(q => q.Where(s => s.ActivoId == activoId));
}
