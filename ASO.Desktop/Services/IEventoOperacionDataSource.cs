using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Eventos de seguimiento que NO viven en la remesa: cambios de turno, mantenimientos y notas.
/// Los del ciclo de vida del documento los deriva <see cref="SeguimientoService"/> y no pasan por aquí.
/// </summary>
public interface IEventoOperacionDataSource : ICrudDataSource<EventoOperacion, int>
{
    IEnumerable<EventoOperacion> GetByRemesa(int remesaId);
}
