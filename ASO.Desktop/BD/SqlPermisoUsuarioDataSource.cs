using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlPermisoUsuarioDataSource : IPermisoUsuarioDataSource
{
    public IEnumerable<PermisoUsuario> GetAll()
    {
        using var context = new AsoDbContext();
        return context.PermisosUsuario.ToList();
    }

    public PermisoUsuario? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.PermisosUsuario.Find(id);
    }

    public PermisoUsuario Add(PermisoUsuario item)
    {
        using var context = new AsoDbContext();
        context.PermisosUsuario.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(PermisoUsuario item)
    {
        using var context = new AsoDbContext();
        context.PermisosUsuario.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var permiso = context.PermisosUsuario.Find(id);
        if (permiso != null)
        {
            context.PermisosUsuario.Remove(permiso);
            context.SaveChanges();
        }
    }
}
