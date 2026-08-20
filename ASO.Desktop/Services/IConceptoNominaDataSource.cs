using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos de Conceptos de nómina. La implementa
/// una fuente EF Core; la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IConceptoNominaDataSource : ICrudDataSource<ConceptoNomina, int>
{
}
