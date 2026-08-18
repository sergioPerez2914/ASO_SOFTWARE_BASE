using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Salidas de almacén. Hoy la implementa un mock en memoria; mañana la implementará un
/// repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface ISalidaInventarioDataSource : ICrudDataSource<SalidaInventario, int>
{
    /// <summary>Salidas imputadas a un mantenimiento: con ellas se valoriza su costo de repuestos.</summary>
    IEnumerable<SalidaInventario> GetByMantenimiento(int mantenimientoId);

    /// <summary>Salidas cargadas a un activo: alimentan su hoja de vida.</summary>
    IEnumerable<SalidaInventario> GetByActivo(int activoId);
}
