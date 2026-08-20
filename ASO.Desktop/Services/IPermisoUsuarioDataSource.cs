using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>Ajustes de permisos por usuario, administrados dentro de la organizacion activa.</summary>
public interface IPermisoUsuarioDataSource : ICrudDataSource<PermisoUsuario, int>
{
}
