using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de revisión de ejemplo mientras no existe base de datos. Catálogo estático por ahora;
/// detrás de la interfaz CRUD queda listo para hacerse editable después.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockReglaMantenimientoDataSource : IReglaMantenimientoDataSource
{
    private readonly List<ReglaMantenimiento> _reglas = new()
    {
        new() { Id = 1, Tipo = TipoActivo.Cosechadora, Revision = "Revisión hidráulica y engrase general", IntervaloHoras = 250m },
        new() { Id = 2, Tipo = TipoActivo.Cosechadora, Revision = "Cambio de cuchillas y correa de la picadora", IntervaloHoras = 500m },
        new() { Id = 3, Tipo = TipoActivo.Tractor, Revision = "Cambio de aceite y filtros de motor", IntervaloHoras = 300m },
        new() { Id = 4, Tipo = TipoActivo.Tractor, Revision = "Revisión de transmisión y sistema hidráulico", IntervaloHoras = 600m },
        new() { Id = 5, Tipo = TipoActivo.Alzadora, Revision = "Engrase de pluma y revisión hidráulica", IntervaloHoras = 250m },
        new() { Id = 6, Tipo = TipoActivo.Camion, Revision = "Cambio de aceite y filtros", IntervaloDias = 90 },
        new() { Id = 7, Tipo = TipoActivo.Camion, Revision = "Revisión de frenos y suspensión", IntervaloDias = 180 },
        new() { Id = 8, Tipo = TipoActivo.Chuto, Revision = "Cambio de aceite y filtros", IntervaloDias = 90 },
        new() { Id = 9, Tipo = TipoActivo.Chuto, Revision = "Revisión de frenos, quinta rueda y batea", IntervaloDias = 120 },
    };

    private int _siguienteId = 10;

    public IEnumerable<ReglaMantenimiento> GetAll() => _reglas;

    public IEnumerable<ReglaMantenimiento> GetByTipo(TipoActivo tipo)
        => _reglas.Where(r => r.Tipo == tipo);

    public ReglaMantenimiento? GetById(int id) => _reglas.FirstOrDefault(r => r.Id == id);

    public ReglaMantenimiento Add(ReglaMantenimiento item)
    {
        item.Id = _siguienteId++;
        _reglas.Add(item);
        return item;
    }

    public void Update(ReglaMantenimiento item)
    {
        var indice = _reglas.FindIndex(r => r.Id == item.Id);
        if (indice >= 0)
            _reglas[indice] = item;
    }

    public void Delete(int id)
    {
        _reglas.RemoveAll(r => r.Id == id);
    }
}
