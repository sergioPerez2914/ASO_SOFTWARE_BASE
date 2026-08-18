using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Facturas al ingenio (Cuentas por Cobrar). Hoy la implementa un mock en memoria; mañana la
/// implementará un repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IFacturaClienteDataSource : ICrudDataSource<FacturaCliente, int>
{
}
