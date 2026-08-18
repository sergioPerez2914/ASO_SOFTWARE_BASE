using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos de Remesas de caña. La UI y los ViewModels solo conocen esta interfaz,
/// así que pasar de mock a EF Core no toca ni la vista ni el ViewModel.
/// </summary>
public interface IRemesaDataSource : ICrudDataSource<Remesa, int>
{
}
