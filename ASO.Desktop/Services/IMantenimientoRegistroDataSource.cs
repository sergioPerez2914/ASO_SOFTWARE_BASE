using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>Constancias de mantenimiento realizado. Se registran vía <see cref="MantenimientoService"/>.</summary>
public interface IMantenimientoRegistroDataSource : ICrudDataSource<MantenimientoRegistro, int>
{
    IEnumerable<MantenimientoRegistro> GetByActivo(int activoId);
}
