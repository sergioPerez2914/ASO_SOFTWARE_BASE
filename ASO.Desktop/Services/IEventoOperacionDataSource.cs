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

    /// <summary>
    /// Borra los eventos de una remesa. No hay clave foránea ni borrado en cascada (las tablas
    /// son planas a propósito), así que al eliminar una remesa en borrador hay que barrer sus
    /// eventos aquí o quedan apuntando a un Id que ya no existe.
    /// </summary>
    void EliminarDeRemesa(int remesaId);
}
