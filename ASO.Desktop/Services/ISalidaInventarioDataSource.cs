using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Salidas de almacén. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface ISalidaInventarioDataSource : ICrudDataSource<SalidaInventario, int>
{
    /// <summary>Salidas imputadas a un mantenimiento: con ellas se valoriza su costo de repuestos.</summary>
    IEnumerable<SalidaInventario> GetByMantenimiento(int mantenimientoId);

    /// <summary>Salidas cargadas a un activo: alimentan su hoja de vida.</summary>
    IEnumerable<SalidaInventario> GetByActivo(int activoId);
}
