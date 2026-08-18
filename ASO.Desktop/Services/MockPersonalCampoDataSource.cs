using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Personal de campo de ejemplo mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockPersonalCampoDataSource : IPersonalCampoDataSource
{
    private readonly List<PersonalCampo> _personal = new()
    {
        new() { Id = 1,  Nombre = "Juan Pérez",         Cedula = "12345678", Rol = RolCampo.Operador,    NucleoCodigo = "N-05" },
        new() { Id = 2,  Nombre = "Rafael Colmenares",  Cedula = "13456789", Rol = RolCampo.Operador,    NucleoCodigo = "N-14" },
        new() { Id = 3,  Nombre = "Luis Mendoza",       Cedula = "14567890", Rol = RolCampo.Operador,    NucleoCodigo = "N-21" },

        new() { Id = 4,  Nombre = "Pedro Escalona",     Cedula = "15678901", Rol = RolCampo.Tractorista, NucleoCodigo = "N-05" },
        new() { Id = 5,  Nombre = "José Graterol",      Cedula = "16789012", Rol = RolCampo.Tractorista, NucleoCodigo = "N-14" },
        new() { Id = 6,  Nombre = "Wilmer Rodríguez",   Cedula = "17890123", Rol = RolCampo.Tractorista, NucleoCodigo = "N-33" },

        new() { Id = 7,  Nombre = "María Gómez",        Cedula = "23456789", Rol = RolCampo.Chofer,      NucleoCodigo = "N-21" },
        new() { Id = 8,  Nombre = "Douglas Piña",       Cedula = "24567890", Rol = RolCampo.Chofer,      NucleoCodigo = "N-05" },
        new() { Id = 9,  Nombre = "Alexis Camacho",     Cedula = "25678901", Rol = RolCampo.Chofer,      NucleoCodigo = "N-14" },

        new() { Id = 10, Nombre = "Ana Torres",         Cedula = "34567890", Rol = RolCampo.Remesero,    NucleoCodigo = "N-05" },
        new() { Id = 11, Nombre = "Yelitza Ramírez",    Cedula = "35678901", Rol = RolCampo.Remesero,    NucleoCodigo = "N-14" },
        new() { Id = 12, Nombre = "Carlos Aguilar",     Cedula = "36789012", Rol = RolCampo.Remesero,    NucleoCodigo = "N-21" },
    };

    private int _siguienteId = 13;

    public IEnumerable<PersonalCampo> GetAll() => _personal;

    public PersonalCampo? GetById(int id) => _personal.FirstOrDefault(p => p.Id == id);

    public PersonalCampo Add(PersonalCampo item)
    {
        item.Id = _siguienteId++;
        _personal.Add(item);
        return item;
    }

    public void Update(PersonalCampo item)
    {
        var indice = _personal.FindIndex(p => p.Id == item.Id);
        if (indice >= 0)
            _personal[indice] = item;
    }

    public void Delete(int id)
    {
        _personal.RemoveAll(p => p.Id == id);
    }
}
