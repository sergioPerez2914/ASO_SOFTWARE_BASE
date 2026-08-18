using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Datos de ejemplo de Empleados mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockEmpleadoDataSource : IEmpleadoDataSource
{
    private readonly List<Empleado> _empleados = new()
    {
        new() { Id = 1, Nombre = "Juan Pérez",     Cedula = "12345678", Cargo = "Operador de cosechadora", Activo = true },
        new() { Id = 2, Nombre = "María Gómez",    Cedula = "23456789", Cargo = "Chofer de transporte",    Activo = true },
        new() { Id = 3, Nombre = "Carlos Rodríguez", Cedula = "34567890", Cargo = "Mecánico de taller",    Activo = true },
        new() { Id = 4, Nombre = "Ana Torres",     Cedula = "45678901", Cargo = "Encargada de inventario", Activo = false },
        new() { Id = 5, Nombre = "Luis Bastidas",  Cedula = "56789012", Cargo = "Almacenista",             Activo = true },
        new() { Id = 6, Nombre = "Yaneth Salas",   Cedula = "67890123", Cargo = "Administradora",          Activo = true },
        new() { Id = 7, Nombre = "Ramón Piñero",   Cedula = "78901234", Cargo = "Mecánico de taller",      Activo = true },
    };

    private int _siguienteId = 8;

    public IEnumerable<Empleado> GetAll() => _empleados;

    public Empleado? GetById(int id) => _empleados.FirstOrDefault(e => e.Id == id);

    public Empleado Add(Empleado item)
    {
        item.Id = _siguienteId++;
        _empleados.Add(item);
        return item;
    }

    public void Update(Empleado item)
    {
        var indice = _empleados.FindIndex(e => e.Id == item.Id);
        if (indice >= 0)
            _empleados[indice] = item;
    }

    public void Delete(int id)
    {
        _empleados.RemoveAll(e => e.Id == id);
    }
}
