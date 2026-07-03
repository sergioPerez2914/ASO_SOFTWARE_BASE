using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Valida credenciales de acceso. Hoy lo implementa un mock en memoria;
/// mañana lo implementará un repositorio EF Core (hash real) sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IAuthService
{
    /// <returns>El usuario si las credenciales son válidas; <c>null</c> en caso contrario.</returns>
    Usuario? ValidarCredenciales(string nombreUsuario, string password);
}
