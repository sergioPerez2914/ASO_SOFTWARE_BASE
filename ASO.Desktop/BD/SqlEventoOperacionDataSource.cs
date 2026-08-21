using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlEventoOperacionDataSource : SqlCrudDataSource<EventoOperacion, int>, IEventoOperacionDataSource
{
    public IEnumerable<EventoOperacion> GetByRemesa(int remesaId)
        => Consultar(q => q.Where(e => e.RemesaId == remesaId));
}
