using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlUsuarioDataSource : IUsuarioDataSource
{
    public IEnumerable<Usuario> GetAll()
    {
        using var context = new AsoDbContext();
        return context.Usuarios.OrderBy(u => u.NombreUsuario).ToList();
    }

    public Usuario? GetById(int id)
    {
        using var context = new AsoDbContext();
        return context.Usuarios.Find(id);
    }

    // IgnoreQueryFilters: al autenticar no hay ambito todavia, asi que el filtro global
    // taparia al propio usuario que intenta entrar. Es uno de los dos sitios donde se salta
    // a proposito (el otro es el selector de organizaciones del Desarrollador).
    public Usuario? BuscarPorNombreSinAmbito(string nombreUsuario)
    {
        using var context = new AsoDbContext();
        return context.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefault(u => u.NombreUsuario == nombreUsuario);
    }

    public IReadOnlyList<PermisoUsuario> AjustesDe(int usuarioId)
    {
        using var context = new AsoDbContext();
        return context.PermisosUsuario
            .IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .ToList();
    }

    public bool ExisteAlguno()
    {
        using var context = new AsoDbContext();
        return context.Usuarios.IgnoreQueryFilters().Any();
    }

    public Usuario Add(Usuario item)
    {
        using var context = new AsoDbContext();
        context.Usuarios.Add(item);
        context.SaveChanges();
        return item;
    }

    public void Update(Usuario item)
    {
        using var context = new AsoDbContext();
        context.Usuarios.Update(item);
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        using var context = new AsoDbContext();
        var usuario = context.Usuarios.Find(id);
        if (usuario != null)
        {
            context.Usuarios.Remove(usuario);
            context.SaveChanges();
        }
    }
}
