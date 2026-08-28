using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Catálogo de temporadas de cosecha. La implementa una fuente EF Core; la interfaz mantiene la
/// UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IZafraDataSource : ICrudDataSource<Zafra, int>
{
}
