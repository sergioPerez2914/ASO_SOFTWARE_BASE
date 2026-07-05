using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlEmpleadoDataSource : IEmpleadoDataSource
{
    // 1. OBTENER TODOS
    public IEnumerable<Empleado> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Empleados.ToList();
    }

    // 2. OBTENER POR ID (Esto resuelve el error CS0535)
    public Empleado GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Empleados.Find(id);
    }

    // 3. AGREGAR (Esto resuelve el error CS0738: ahora devuelve 'Empleado' en vez de 'void')
    public Empleado Add(Empleado entidad)
    {
        using var context = new AsoDbContext();
        context.Empleados.Add(entidad);
        context.SaveChanges(); // SQL genera el Id automático aquí

        return entidad; // Devolvemos el empleado con su nuevo Id asignado
    }

    // 4. ACTUALIZAR
    public void Update(Empleado entidad)
    {
        using var context = new AsoDbContext();
        context.Empleados.Update(entidad);
        context.SaveChanges();
    }

    // 5. ELIMINAR
    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var empleado = context.Empleados.Find(id);
        if (empleado != null)
        {
            context.Empleados.Remove(empleado);
            context.SaveChanges();
        }
    }
}