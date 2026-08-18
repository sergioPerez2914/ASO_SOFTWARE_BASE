using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Facturas de ejemplo mientras no existe base de datos. Arranca vacía a propósito: las
/// remesas semilla quedan todas facturables, y así el flujo completo (generar, emitir, cobrar)
/// se prueba desde la pantalla sin que el mock de facturas y el de remesas se contradigan
/// sobre quién está facturado.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockFacturaClienteDataSource : IFacturaClienteDataSource
{
    private readonly List<FacturaCliente> _facturas = new();

    private int _siguienteId = 1;

    public IEnumerable<FacturaCliente> GetAll() => _facturas;

    public FacturaCliente? GetById(int id) => _facturas.FirstOrDefault(f => f.Id == id);

    public FacturaCliente Add(FacturaCliente item)
    {
        item.Id = _siguienteId++;
        _facturas.Add(item);
        return item;
    }

    public void Update(FacturaCliente item)
    {
        var indice = _facturas.FindIndex(f => f.Id == item.Id);
        if (indice >= 0)
            _facturas[indice] = item;
    }

    public void Delete(int id) => _facturas.RemoveAll(f => f.Id == id);
}
