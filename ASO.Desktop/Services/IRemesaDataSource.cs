using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos de Remesas de caña. La UI y los ViewModels solo conocen esta interfaz,
/// así que no saben nada de EF Core ni del filtro por organización.
/// </summary>
public interface IRemesaDataSource : ICrudDataSource<Remesa, int>
{
}
