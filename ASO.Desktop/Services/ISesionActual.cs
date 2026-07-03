using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Usuario con sesión activa en la aplicación. Sirve de base para las reglas de
/// autorización por rol descritas en el plan (RBAC, segregación de funciones).
/// </summary>
public interface ISesionActual
{
    Usuario? UsuarioActual { get; }
    bool EstaAutenticado { get; }

    void IniciarSesion(Usuario usuario);
    void CerrarSesion();

    /// <summary>
    /// ¿El usuario actual puede realizar <paramref name="permiso"/> (formato "Módulo.Acción")?
    /// Hoy solo exige sesión iniciada; se conectará a la matriz de roles RBAC de la Fase 0
    /// sin cambiar las ViewModels que ya llaman a este método.
    /// </summary>
    bool Puede(string permiso);
}
