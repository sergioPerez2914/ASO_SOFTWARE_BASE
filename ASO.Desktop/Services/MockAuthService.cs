using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Usuarios de ejemplo (uno por rol) mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) con hash de contraseña
/// en la capa de infraestructura.
/// </summary>
public class MockAuthService : IAuthService
{
    private record Credencial(Usuario Usuario, string Password);

    private readonly List<Credencial> _credenciales = new()
    {
        new(new Usuario { Id = 1, NombreUsuario = "admin",      NombreCompleto = "Administrador",     Rol = Rol.Admin },       "admin123"),
        new(new Usuario { Id = 2, NombreUsuario = "operaciones", NombreCompleto = "Carlos Operaciones", Rol = Rol.Operaciones }, "1234"),
        new(new Usuario { Id = 3, NombreUsuario = "taller",      NombreCompleto = "Marta Taller",       Rol = Rol.Taller },      "1234"),
        new(new Usuario { Id = 4, NombreUsuario = "finanzas",    NombreCompleto = "Luis Finanzas",      Rol = Rol.Finanzas },    "1234"),
        new(new Usuario { Id = 5, NombreUsuario = "rrhh",        NombreCompleto = "Ana RRHH",           Rol = Rol.RRHH },        "1234"),
        new(new Usuario { Id = 6, NombreUsuario = "consulta",    NombreCompleto = "Usuario de Consulta", Rol = Rol.Consulta },   "1234"),
    };

    public Usuario? ValidarCredenciales(string nombreUsuario, string password)
    {
        var credencial = _credenciales.FirstOrDefault(c =>
            string.Equals(c.Usuario.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)
            && c.Password == password);

        return credencial?.Usuario;
    }
}
