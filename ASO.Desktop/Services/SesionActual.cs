using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Implementación en memoria de <see cref="ISesionActual"/> para un solo centro / una sola sesión.
/// </summary>
public class SesionActual : ISesionActual
{
    public static SesionActual Instancia { get; } = new();

    public Usuario? UsuarioActual { get; private set; }
    public bool EstaAutenticado => UsuarioActual is not null;

    public void IniciarSesion(Usuario usuario) => UsuarioActual = usuario;
    public void CerrarSesion() => UsuarioActual = null;

    // TODO Fase 0: reemplazar por la matriz de roles/permisos RBAC (Módulo.Acción).
    public bool Puede(string permiso) => EstaAutenticado;
}
