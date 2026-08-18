using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Núcleos de ejemplo mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockNucleoDataSource : INucleoDataSource
{
    private readonly List<Nucleo> _nucleos = new()
    {
        new() { Id = 1, Codigo = "N-05", Nombre = "Núcleo La Colonia" },
        new() { Id = 2, Codigo = "N-14", Nombre = "Núcleo Payara" },
        new() { Id = 3, Codigo = "N-21", Nombre = "Núcleo Sarare" },
        new() { Id = 4, Codigo = "N-33", Nombre = "Núcleo Turén" },
    };

    private int _siguienteId = 5;

    public IEnumerable<Nucleo> GetAll() => _nucleos;

    public Nucleo? GetById(int id) => _nucleos.FirstOrDefault(n => n.Id == id);

    public Nucleo Add(Nucleo item)
    {
        item.Id = _siguienteId++;
        _nucleos.Add(item);
        return item;
    }

    public void Update(Nucleo item)
    {
        var indice = _nucleos.FindIndex(n => n.Id == item.Id);
        if (indice >= 0)
            _nucleos[indice] = item;
    }

    public void Delete(int id)
    {
        _nucleos.RemoveAll(n => n.Id == id);
    }
}
