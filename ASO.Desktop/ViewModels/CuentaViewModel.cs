using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// La cuenta de quien está dentro: quién es, con qué rol y sobre qué núcleo, y el único sitio
/// de la aplicación donde puede cambiar su propia contraseña.
///
/// Hasta ahora solo un administrador podía cambiarla, desde Administración · Usuarios, así que
/// rotar la propia clave obligaba a pedírselo a otro — y de paso a decirle la nueva en voz alta.
///
/// La contraseña actual se exige aunque la sesión ya esté iniciada: un equipo desatendido es
/// justo el escenario en el que alguien cambiaría la clave ajena.
/// </summary>
public sealed class CuentaViewModel : ViewModelBase
{
    /// <summary>El mismo mínimo que exige el alta de usuarios; una sola regla para las dos vías.</summary>
    public const int LargoMinimoPassword = 8;

    private readonly IUsuarioDataSource _usuarios;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    public CuentaViewModel(IUsuarioDataSource usuarios, IServicioDialogo dialogos, ISesionActual sesion)
    {
        _usuarios = usuarios;
        _dialogos = dialogos;
        _sesion = sesion;

        _recordarUsuario = Ajustes.Actual.RecordarUltimoUsuario;
    }

    public string NombreUsuario => _sesion.UsuarioActual?.NombreUsuario ?? "—";
    public string NombreCompleto => _sesion.UsuarioActual?.NombreCompleto ?? "—";
    public string RolTexto => _sesion.UsuarioActual?.RolTexto ?? "—";

    public string NucleoTexto => Ambito.Actual is { } nucleo
        ? $"{nucleo.Nombre} · C.O.D {nucleo.CodigoCam}"
        : "—";

    public string AyudaPassword =>
        $"Al menos {LargoMinimoPassword} caracteres. Se aplica de inmediato: la próxima vez que entres usa la nueva.";

    private bool _recordarUsuario;
    public bool RecordarUsuario
    {
        get => _recordarUsuario;
        set
        {
            if (!SetProperty(ref _recordarUsuario, value))
                return;

            Ajustes.Actual.RecordarUltimoUsuario = value;

            // Al desactivarlo se borra el que ya estaba guardado: dejarlo escrito en el archivo
            // haria que la casilla dijera una cosa y el disco otra.
            if (!value)
                Ajustes.Actual.UltimoUsuario = string.Empty;

            Ajustes.Guardar();
        }
    }

    /// <summary>
    /// Cambia la contraseña del usuario en sesión.
    ///
    /// Recibe las tres por parámetro y no por binding porque el <c>PasswordBox</c> de WPF no
    /// expone su contenido como propiedad enlazable — a propósito, para que no quede colgado
    /// en el árbol de bindings. Es la misma vía que usa <c>LoginView</c>.
    /// </summary>
    /// <returns><c>true</c> si se cambió, para que la vista sepa si limpiar los campos.</returns>
    public bool CambiarPassword(string actual, string nueva, string confirmacion)
    {
        if (_sesion.UsuarioActual is not { } usuario)
            return false;

        if (!Passwords.Verificar(actual, usuario.PasswordHash, usuario.PasswordSalt))
        {
            _dialogos.Informar("Contraseña incorrecta",
                "La contraseña actual no coincide. No se cambió nada.");
            return false;
        }

        if (nueva.Length < LargoMinimoPassword)
        {
            _dialogos.Informar("Contraseña demasiado corta",
                $"La contraseña nueva debe tener al menos {LargoMinimoPassword} caracteres.");
            return false;
        }

        if (nueva != confirmacion)
        {
            _dialogos.Informar("No coinciden",
                "La contraseña nueva y su confirmación no son iguales.");
            return false;
        }

        if (nueva == actual)
        {
            _dialogos.Informar("Es la misma",
                "La contraseña nueva es igual a la actual. Elige otra.");
            return false;
        }

        var (hash, salt) = Passwords.Crear(nueva);

        var copia = usuario.Clonar();
        copia.PasswordHash = hash;
        copia.PasswordSalt = salt;

        try
        {
            _usuarios.Update(copia);
        }
        catch (Exception ex)
        {
            _dialogos.Informar("No se pudo cambiar", ex.Message);
            return false;
        }

        // La sesión guarda su propia copia del usuario y de ella sale la verificación de la
        // próxima vez: sin esto, cambiar dos veces seguidas fallaría en la segunda.
        usuario.PasswordHash = hash;
        usuario.PasswordSalt = salt;

        _dialogos.Informar("Contraseña cambiada",
            "Ya está activa. La próxima vez que inicies sesión usa la nueva.");
        return true;
    }
}
