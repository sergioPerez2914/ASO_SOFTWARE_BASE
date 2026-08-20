using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Primera puesta en marcha: crea el núcleo inicial y el usuario Desarrollador que lo administra.
///
/// La contraseña se pide aquí en vez de sembrarse en una migración a propósito. Una clave por
/// defecto en el repositorio es una clave pública, y este es justo el usuario que puede entrar a
/// todos los núcleos.
/// </summary>
public sealed class PrimerArranqueViewModel : ViewModelBase
{
    private const int LargoMinimoPassword = 8;

    private readonly IOrganizacionDataSource _organizaciones;
    private readonly IUsuarioDataSource _usuarios;

    public PrimerArranqueViewModel()
        : this(DataSourceFactory.CrearOrganizaciones(), DataSourceFactory.CrearUsuarios())
    {
    }

    public PrimerArranqueViewModel(IOrganizacionDataSource organizaciones, IUsuarioDataSource usuarios)
    {
        _organizaciones = organizaciones;
        _usuarios = usuarios;

        // La migración Fase6 deja sembrado un núcleo con nombre provisional, dueño de todo lo que
        // ya estaba cargado. Se ADOPTA en vez de crear otro: si aquí naciera un núcleo nuevo, el
        // desarrollador entraría a uno vacío y no vería nada de lo que hay en la base.
        _existente = _organizaciones.GetAll().FirstOrDefault();

        if (_existente is { } organizacion)
        {
            _codigoNucleo = organizacion.Codigo;
            _nombreNucleo = organizacion.Nombre;
        }
    }

    private readonly Organizacion? _existente;

    public string Explicacion => _existente is null
        ? "La base de datos está vacía. Crea el primer núcleo y el usuario desarrollador que lo administrará."
        : "La base de datos no tiene usuarios todavía. Ponle nombre al núcleo que ya contiene los datos y crea el usuario desarrollador que lo administrará.";

    private string _codigoNucleo = string.Empty;
    public string CodigoNucleo
    {
        get => _codigoNucleo;
        set => SetProperty(ref _codigoNucleo, value);
    }

    private string _nombreNucleo = string.Empty;
    public string NombreNucleo
    {
        get => _nombreNucleo;
        set => SetProperty(ref _nombreNucleo, value);
    }

    private string _nombreUsuario = string.Empty;
    public string NombreUsuario
    {
        get => _nombreUsuario;
        set => SetProperty(ref _nombreUsuario, value);
    }

    private string _nombreCompleto = string.Empty;
    public string NombreCompleto
    {
        get => _nombreCompleto;
        set => SetProperty(ref _nombreCompleto, value);
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set => SetProperty(ref _mensajeError, value);
    }

    /// <summary>Crea núcleo y usuario. <c>true</c> si quedó todo listo para iniciar sesión.</summary>
    public bool Crear(string password)
    {
        if (!Validar(password))
            return false;

        try
        {
            Organizacion organizacion;

            if (_existente is { } actual)
            {
                organizacion = actual.Clonar();
                organizacion.Codigo = CodigoNucleo.Trim().ToUpperInvariant();
                organizacion.Nombre = NombreNucleo.Trim();
                organizacion.Activa = true;
                _organizaciones.Update(organizacion);
            }
            else
            {
                organizacion = _organizaciones.Add(new Organizacion
                {
                    Codigo = CodigoNucleo.Trim().ToUpperInvariant(),
                    Nombre = NombreNucleo.Trim(),
                    Activa = true
                });
            }

            var (hash, salt) = Passwords.Crear(password);

            // OrganizacionId explícito: el estampado automático de AsoDbContext.SaveChanges lee
            // el ámbito de la sesión, y todavía no hay ninguna.
            _usuarios.Add(new Usuario
            {
                OrganizacionId = organizacion.Id,
                NombreUsuario = NombreUsuario.Trim(),
                NombreCompleto = NombreCompleto.Trim(),
                Rol = Rol.Desarrollador,
                PasswordHash = hash,
                PasswordSalt = salt,
                Activo = true
            });
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo crear la configuración inicial. {ex.Message}";
            return false;
        }

        MensajeError = string.Empty;
        return true;
    }

    private bool Validar(string password)
    {
        if (string.IsNullOrWhiteSpace(CodigoNucleo))
        {
            MensajeError = "Indique el código del núcleo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(NombreNucleo))
        {
            MensajeError = "Indique el nombre del núcleo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(NombreUsuario) || NombreUsuario.Trim().Contains(' '))
        {
            MensajeError = "Indique un nombre de usuario sin espacios.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(NombreCompleto))
        {
            MensajeError = "Indique el nombre completo.";
            return false;
        }

        if (password.Length < LargoMinimoPassword)
        {
            MensajeError = $"La contraseña debe tener al menos {LargoMinimoPassword} caracteres.";
            return false;
        }

        return true;
    }
}
