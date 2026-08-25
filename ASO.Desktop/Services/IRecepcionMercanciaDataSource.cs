using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

public interface IRecepcionMercanciaDataSource : ICrudDataSource<RecepcionMercancia, int>
{
    /// <summary>Recepciones de una orden de compra (incluye anuladas, para el historial).</summary>
    IEnumerable<RecepcionMercancia> GetByOrdenCompra(int ordenCompraId);
}
