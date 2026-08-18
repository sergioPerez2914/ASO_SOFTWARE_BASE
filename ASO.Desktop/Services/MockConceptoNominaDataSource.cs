using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Datos de ejemplo de Conceptos de nómina mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockConceptoNominaDataSource : IConceptoNominaDataSource
{
    private readonly List<ConceptoNomina> _conceptos = new()
    {
        new() { Id = 1, Nombre = "Bono de asistencia",    Tipo = TipoConcepto.Devengo,   Activo = true },
        new() { Id = 2, Nombre = "Bono de productividad", Tipo = TipoConcepto.Devengo,   Activo = true },
        new() { Id = 3, Nombre = "Anticipo",              Tipo = TipoConcepto.Deduccion, Activo = true },
        new() { Id = 4, Nombre = "Préstamo",              Tipo = TipoConcepto.Deduccion, Activo = true },
    };

    private int _siguienteId = 5;

    public IEnumerable<ConceptoNomina> GetAll() => _conceptos;

    public ConceptoNomina? GetById(int id) => _conceptos.FirstOrDefault(c => c.Id == id);

    public ConceptoNomina Add(ConceptoNomina item)
    {
        item.Id = _siguienteId++;
        _conceptos.Add(item);
        return item;
    }

    public void Update(ConceptoNomina item)
    {
        var indice = _conceptos.FindIndex(c => c.Id == item.Id);
        if (indice >= 0)
            _conceptos[indice] = item;
    }

    public void Delete(int id)
    {
        _conceptos.RemoveAll(c => c.Id == id);
    }
}
