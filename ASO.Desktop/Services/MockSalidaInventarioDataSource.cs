using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Salidas de almacén de ejemplo mientras no existe base de datos. Los códigos de artículo y
/// los activos coinciden con <see cref="MockInventoryDataSource"/> y
/// <see cref="MockActivoFlotaDataSource"/>; hay una de cada estado para ver los filtros.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockSalidaInventarioDataSource : ISalidaInventarioDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<SalidaInventario> _salidas = new()
    {
        new()
        {
            Id = 1,
            Fecha = Hoy.AddDays(-6),
            ArticuloCodigo = "FIL-010", ArticuloNombre = "Filtro de aceite de motor", Unidad = "und",
            Cantidad = 2m, CostoUnitario = 18.75m,
            ActivoId = 6, ActivoEtiqueta = "COS-01 · Case IH A8000",
            MantenimientoId = 1,
            Motivo = "Cambio de filtros en servicio preventivo",
            Estado = EstadoSalida.Confirmada,
            FechaConfirmacion = Hoy.AddDays(-6),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-6)
        },
        new()
        {
            Id = 2,
            Fecha = Hoy.AddDays(-1),
            ArticuloCodigo = "LUB-030", ArticuloNombre = "Aceite hidráulico ISO 68", Unidad = "L",
            Cantidad = 20m,
            ActivoId = 1, ActivoEtiqueta = "CHU-01 · Mack Granite",
            Motivo = "Relleno de sistema hidráulico",
            Estado = EstadoSalida.Borrador,
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-1)
        },
        new()
        {
            Id = 3,
            Fecha = Hoy.AddDays(-10),
            ArticuloCodigo = "MAN-020", ArticuloNombre = "Manguera hidráulica 1/2\"", Unidad = "m",
            Cantidad = 4m, CostoUnitario = 12.30m,
            Motivo = "Solicitada por error, no se retiró del almacén",
            Estado = EstadoSalida.Anulada,
            MotivoAnulacion = "Cargada al activo equivocado.",
            FechaAnulacion = Hoy.AddDays(-10),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-10)
        },
    };

    private int _siguienteId = 4;

    public IEnumerable<SalidaInventario> GetAll() => _salidas;

    public SalidaInventario? GetById(int id) => _salidas.FirstOrDefault(s => s.Id == id);

    public IEnumerable<SalidaInventario> GetByMantenimiento(int mantenimientoId) =>
        _salidas.Where(s => s.MantenimientoId == mantenimientoId);

    public IEnumerable<SalidaInventario> GetByActivo(int activoId) =>
        _salidas.Where(s => s.ActivoId == activoId);

    public SalidaInventario Add(SalidaInventario item)
    {
        item.Id = _siguienteId++;
        _salidas.Add(item);
        return item;
    }

    public void Update(SalidaInventario item)
    {
        var indice = _salidas.FindIndex(s => s.Id == item.Id);
        if (indice >= 0)
            _salidas[indice] = item;
    }

    public void Delete(int id)
    {
        _salidas.RemoveAll(s => s.Id == id);
    }
}
