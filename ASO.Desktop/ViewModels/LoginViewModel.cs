using ASO.Desktop.Configuration;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Lógica de presentación de la pantalla de inicio de sesión.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISesionActual _sesion;

    public LoginViewModel() : this(DataSourceFactory.CrearAuth(), SesionActual.Instancia) { }

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
        ResultadoAutenticacion? resultado;
        try
        {
            resultado = _authService.ValidarCredenciales(NombreUsuario, password);
        }
        catch (Exception ex)
        {
            // Sin base de datos no hay login posible; decirlo aquí evita que el fallo
            // aparezca más tarde, a mitad de una navegación, como si fuera otra cosa.
            MensajeError = $"No se pudo conectar con la base de datos. {ex.Message}";
            return false;
        }

        if (resultado is null)
        {
            MensajeError = "Usuario o contraseña incorrectos.";
            return false;
        }

        _sesion.IniciarSesion(resultado.Usuario, resultado.Ajustes);
        MensajeError = string.Empty;
        return true;
    }
}
