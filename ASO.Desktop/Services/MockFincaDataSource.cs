using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fincas de ejemplo mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockFincaDataSource : IFincaDataSource
{
    private readonly List<Finca> _fincas = new()
    {
        new()
        {
            Id = 1, CodigoCam = "F-0112", Nombre = "La Esperanza",
            Lotes =
            [
                new Lote { Id = 1, Nombre = "Lote 1", Tablones = [new() { Id = 1, Nombre = "Tablón A" }, new() { Id = 2, Nombre = "Tablón B" }] },
                new Lote { Id = 2, Nombre = "Lote 2", Tablones = [new() { Id = 3, Nombre = "Tablón A" }, new() { Id = 4, Nombre = "Tablón C" }] }
            ]
        },
        new()
        {
            Id = 2, CodigoCam = "F-0245", Nombre = "Santa Rita",
            Lotes =
            [
                new Lote { Id = 3, Nombre = "Lote Norte", Tablones = [new() { Id = 5, Nombre = "Tablón 1" }, new() { Id = 6, Nombre = "Tablón 2" }, new() { Id = 7, Nombre = "Tablón 3" }] },
                new Lote { Id = 4, Nombre = "Lote Sur",   Tablones = [new() { Id = 8, Nombre = "Tablón 1" }, new() { Id = 9, Nombre = "Tablón 2" }] }
            ]
        },
        new()
        {
            Id = 3, CodigoCam = "F-0301", Nombre = "Agropecuaria Turén",
            Lotes =
            [
                new Lote { Id = 5, Nombre = "Lote El Samán", Tablones = [new() { Id = 10, Nombre = "Tablón A" }, new() { Id = 11, Nombre = "Tablón B" }] },
                new Lote { Id = 6, Nombre = "Lote La Ceiba", Tablones = [new() { Id = 12, Nombre = "Tablón Único" }] }
            ]
        },
    };

    private int _siguienteId = 4;

    public IEnumerable<Finca> GetAll() => _fincas;

    public Finca? GetById(int id) => _fincas.FirstOrDefault(f => f.Id == id);

    public Finca Add(Finca item)
    {
        item.Id = _siguienteId++;
        _fincas.Add(item);
        return item;
    }

    public void Update(Finca item)
    {
        var indice = _fincas.FindIndex(f => f.Id == item.Id);
        if (indice >= 0)
            _fincas[indice] = item;
    }

    public void Delete(int id)
    {
        _fincas.RemoveAll(f => f.Id == id);
    }
}
