using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Proveedores de ejemplo mientras no existe base de datos. El de combustible coincide con el
/// que citan las recargas de <see cref="MockRecargaCombustibleDataSource"/>.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockProveedorDataSource : IProveedorDataSource
{
    private readonly List<Proveedor> _proveedores = new()
    {
        new() { Id = 1, Nombre = "Estación de servicio Los Llanos", Rif = "J-30125478-9", Telefono = "0255-6543210", Notas = "Combustible de la cisterna principal.", Activo = true },
        new() { Id = 2, Nombre = "Repuestos Agrícolas Portuguesa",  Rif = "J-29874561-3", Telefono = "0257-2345678", Notas = "Filtros, correas y cuchillas.",         Activo = true },
        new() { Id = 3, Nombre = "Taller Hidráulico Acarigua",      Rif = "J-31456789-0", Telefono = "0255-7891234", Notas = "Reparación de mangueras y bombas.",     Activo = true },
        new() { Id = 4, Nombre = "Ferretería El Tornillo",          Rif = "J-28456123-7", Telefono = "0255-4567891", Activo = false },
    };

    private int _siguienteId = 5;

    public IEnumerable<Proveedor> GetAll() => _proveedores;

    public Proveedor? GetById(int id) => _proveedores.FirstOrDefault(p => p.Id == id);

    public Proveedor Add(Proveedor item)
    {
        item.Id = _siguienteId++;
        _proveedores.Add(item);
        return item;
    }

    public void Update(Proveedor item)
    {
        var indice = _proveedores.FindIndex(p => p.Id == item.Id);
        if (indice >= 0)
            _proveedores[indice] = item;
    }

    public void Delete(int id) => _proveedores.RemoveAll(p => p.Id == id);
}

/// <summary>
/// Facturas de compra de ejemplo mientras no existe base de datos. Hay una vencida a propósito,
/// para ver el filtro de vencidas y el indicador del módulo.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockFacturaProveedorDataSource : IFacturaProveedorDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<FacturaProveedor> _facturas = new()
    {
        new()
        {
            Id = 1, NumeroDocumento = "00012345",
            ProveedorId = 1, ProveedorNombre = "Estación de servicio Los Llanos",
            Descripcion = "8.000 L de gasoil para la cisterna principal",
            FechaEmision = Hoy.AddDays(-15), FechaVencimiento = Hoy.AddDays(15),
            Monto = 4_800m, Estado = EstadoFacturaProveedor.Pendiente,
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-15)
        },
        new()
        {
            Id = 2, NumeroDocumento = "A-7781",
            ProveedorId = 2, ProveedorNombre = "Repuestos Agrícolas Portuguesa",
            Descripcion = "Filtros y correas para cosechadoras",
            FechaEmision = Hoy.AddDays(-40), FechaVencimiento = Hoy.AddDays(-10),
            Monto = 1_260.50m, Estado = EstadoFacturaProveedor.Pendiente,
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-40)
        },
        new()
        {
            Id = 3, NumeroDocumento = "TH-0455",
            ProveedorId = 3, ProveedorNombre = "Taller Hidráulico Acarigua",
            Descripcion = "Reparación de bomba hidráulica de ALZ-01",
            FechaEmision = Hoy.AddDays(-30), FechaVencimiento = Hoy.AddDays(-5),
            Monto = 890m, Estado = EstadoFacturaProveedor.Pagada, FechaPago = Hoy.AddDays(-6),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-30)
        },
        new()
        {
            Id = 4, NumeroDocumento = "F-9002",
            ProveedorId = 4, ProveedorNombre = "Ferretería El Tornillo",
            Descripcion = "Material de ferretería facturado por duplicado",
            FechaEmision = Hoy.AddDays(-20), FechaVencimiento = Hoy.AddDays(10),
            Monto = 320m, Estado = EstadoFacturaProveedor.Anulada,
            MotivoAnulacion = "El proveedor emitió la misma factura dos veces.",
            FechaAnulacion = Hoy.AddDays(-18),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-20)
        },
    };

    private int _siguienteId = 5;

    public IEnumerable<FacturaProveedor> GetAll() => _facturas;

    public FacturaProveedor? GetById(int id) => _facturas.FirstOrDefault(f => f.Id == id);

    public IEnumerable<FacturaProveedor> GetByProveedor(int proveedorId) =>
        _facturas.Where(f => f.ProveedorId == proveedorId);

    public FacturaProveedor Add(FacturaProveedor item)
    {
        item.Id = _siguienteId++;
        _facturas.Add(item);
        return item;
    }

    public void Update(FacturaProveedor item)
    {
        var indice = _facturas.FindIndex(f => f.Id == item.Id);
        if (indice >= 0)
            _facturas[indice] = item;
    }

    public void Delete(int id) => _facturas.RemoveAll(f => f.Id == id);
}
