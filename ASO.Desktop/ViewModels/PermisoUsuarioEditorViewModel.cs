using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Ajuste puntual de un permiso sobre un usuario, por encima de lo que ya da su rol.
/// Sirve para los dos sentidos: darle a un remesero algo que su rol no trae, o quitarle
/// a alguien algo que sí traía.
/// </summary>
public sealed class PermisoUsuarioEditorViewModel : CrudEditorViewModelBase<PermisoUsuario>
{
    private readonly PermisoUsuario _original;

    public PermisoUsuarioEditorViewModel(PermisoUsuario original, IReadOnlyList<Usuario> usuarios)
        : base(original)
    {
        _original = original;
        Usuarios = usuarios;

        // Todo el universo de permisos, ordenado: se elige de una lista en vez de escribirlo,
        // porque un permiso mal tecleado no falla, simplemente no aplica nunca.
        Permisos = [.. MatrizPermisos.Todos.OrderBy(p => p)];

        _usuario = usuarios.FirstOrDefault(u => u.Id == original.UsuarioId);
        _permiso = string.IsNullOrEmpty(original.Permiso) ? Permisos.FirstOrDefault() : original.Permiso;
        _concedido = original.Id == 0 || original.Concedido;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo ajuste de permiso" : "Ajuste de permiso";

    public IReadOnlyList<Usuario> Usuarios { get; }
    public IReadOnlyList<string> Permisos { get; }

    private Usuario? _usuario;
    public Usuario? Usuario
    {
        get => _usuario;
        set => SetProperty(ref _usuario, value);
    }

    private string? _permiso;
    public string? Permiso
    {
        get => _permiso;
        set => SetProperty(ref _permiso, value);
    }

    private bool _concedido;
    public bool Concedido
    {
        get => _concedido;
        set => SetProperty(ref _concedido, value);
    }

    protected override bool Validar(out string? error)
    {
        if (Usuario is null)
        {
            error = "Elija el usuario.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Permiso))
        {
            error = "Elija el permiso.";
            return false;
        }

        error = null;
        return true;
    }

    public override PermisoUsuario ObtenerResultado()
    {
        var resultado = _original.Clonar();
        resultado.UsuarioId = Usuario!.Id;
        resultado.UsuarioNombre = Usuario.NombreUsuario;
        resultado.Permiso = Permiso!;
        resultado.Concedido = Concedido;
        return resultado;
    }
}
