using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Facturas al ingenio (Cuentas por Cobrar). La implementa
/// una fuente EF Core; la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IFacturaClienteDataSource : ICrudDataSource<FacturaCliente, int>
{
}
