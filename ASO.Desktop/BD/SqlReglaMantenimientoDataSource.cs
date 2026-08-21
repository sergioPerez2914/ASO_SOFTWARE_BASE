using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlReglaMantenimientoDataSource : SqlCrudDataSource<ReglaMantenimiento, int>, IReglaMantenimientoDataSource
{
    public IEnumerable<ReglaMantenimiento> GetByTipo(TipoActivo tipo)
        => Consultar(q => q.Where(e => e.Tipo == tipo));
}
