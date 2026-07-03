using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Lógica de presentación de la pantalla de inicio de sesión.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISesionActual _sesion;

    public LoginViewModel() : this(new MockAuthService(), SesionActual.Instancia) { }

    public LoginViewModel(IAuthService authService, ISesionActual sesion)
    {
        _authService = authService;
        _sesion = sesion;
    }

    private string _nombreUsuario = string.Empty;
    public string NombreUsuario
    {
        get => _nombreUsuario;
        set => SetProperty(ref _nombreUsuario, value);
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set => SetProperty(ref _mensajeError, value);
    }

    /// <returns><c>true</c> si las credenciales son válidas e inicia la sesión.</returns>
    public bool IntentarIniciarSesion(string password)
    {
        var usuario = _authService.ValidarCredenciales(NombreUsuario, password);
        if (usuario is null)
        {
            MensajeError = "Usuario o contraseña incorrectos.";
            return false;
        }

        _sesion.IniciarSesion(usuario);
        MensajeError = string.Empty;
        return true;
    }
}
