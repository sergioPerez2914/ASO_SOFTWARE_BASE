using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Facturas de compra (Cuentas por Pagar). Hoy la implementa un mock en memoria; mañana la
/// implementará un repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IFacturaProveedorDataSource : ICrudDataSource<FacturaProveedor, int>
{
    IEnumerable<FacturaProveedor> GetByProveedor(int proveedorId);
}
