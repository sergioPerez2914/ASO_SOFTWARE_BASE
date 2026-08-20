using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Ajustes de permisos por usuario: lo que se le concede o se le quita por encima de su rol.
/// Comparte el prefijo de permiso "Usuarios" porque administrar quién puede qué es la misma
/// atribución que administrar el padrón.
/// </summary>
public sealed class PermisosUsuarioCrudViewModel : CrudViewModelBase<PermisoUsuario, int>
{
    private readonly IUsuarioDataSource _usuarios;

    public PermisosUsuarioCrudViewModel(IPermisoUsuarioDataSource permisos,
                                        IUsuarioDataSource usuarios,
                                        IServicioDialogo dialogos,
                                        ISesionActual sesion)
        : base(permisos, dialogos, sesion)
    {
        _usuarios = usuarios;
    }

    protected override string ModuloPermiso => "Usuarios";

    protected override bool CoincideBusqueda(PermisoUsuario item, string texto) =>
        item.Permiso.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.UsuarioNombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override PermisoUsuario CrearNuevo() => new();

    protected override CrudEditorViewModelBase<PermisoUsuario> CrearEditor(PermisoUsuario item) =>
        new PermisoUsuarioEditorViewModel(item, [.. _usuarios.GetAll()]);
}
