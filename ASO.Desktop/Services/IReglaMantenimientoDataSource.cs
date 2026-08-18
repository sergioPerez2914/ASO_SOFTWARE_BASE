using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>Reglas de revisión periódica por tipo de activo.</summary>
public interface IReglaMantenimientoDataSource : ICrudDataSource<ReglaMantenimiento, int>
{
    IEnumerable<ReglaMantenimiento> GetByTipo(TipoActivo tipo);
}
