using System;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Administración del sistema: el padrón de núcleos, los usuarios de cada uno y los ajustes de
/// permisos que los apartan de lo que su rol trae por defecto.
///
/// Tres padrones en una vista conmutable, como <see cref="EmpleadosViewModel"/> y
/// <see cref="FincasYNucleosViewModel"/>. El de núcleos solo aparece para el Desarrollador:
/// un administrador manda en el suyo, no reparte núcleos.
/// </summary>
public sealed class AdministracionViewModel : ViewModelBase
{
    public const string VistaOrganizaciones = "Organizaciones";
    public const string VistaUsuarios = "Usuarios";
    public const string VistaPermisos = "Permisos";

    public event EventHandler? VolverSolicitado;

    public AdministracionViewModel(Modulo modulo)
        : this(modulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private AdministracionViewModel(Modulo modulo, IServicioDialogo dialogos, ISesionActual sesion)
    {
        Modulo = modulo;

        var usuarios = DataSourceFactory.CrearUsuarios();
        Organizaciones = new OrganizacionCrudViewModel(
            DataSourceFactory.CrearOrganizaciones(), dialogos, sesion);
        Usuarios = new UsuariosCrudViewModel(usuarios, dialogos, sesion);
        Permisos = new PermisosUsuarioCrudViewModel(
            DataSourceFactory.CrearPermisosUsuario(), usuarios, dialogos, sesion);

        PuedeVerOrganizaciones = sesion.Puede(Services.Permisos.Organizaciones.Crear);
        VistaActual = PuedeVerOrganizaciones ? VistaOrganizaciones : VistaUsuarios;

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public Modulo Modulo { get; }
    public string Ruta => Modulo.Nombre;

    public OrganizacionCrudViewModel Organizaciones { get; }
    public UsuariosCrudViewModel Usuarios { get; }
    public PermisosUsuarioCrudViewModel Permisos { get; }

    public bool PuedeVerOrganizaciones { get; }

    /// <summary>
    /// Los cambios de permisos se aplican al volver a entrar, no en caliente: el conjunto
    /// efectivo se calcula una sola vez al iniciar sesión. Decirlo en pantalla evita que
    /// alguien conceda un permiso y crea que no funcionó.
    /// </summary>
    public string NotaVigencia =>
        "Los cambios de rol y de permisos se aplican la próxima vez que el usuario inicie sesión.";

    private string _vistaActual = VistaUsuarios;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            if (SetProperty(ref _vistaActual, value))
            {
                OnPropertyChanged(nameof(MostrarOrganizaciones));
                OnPropertyChanged(nameof(MostrarUsuarios));
                OnPropertyChanged(nameof(MostrarPermisos));
            }
        }
    }

    public bool MostrarOrganizaciones => VistaActual == VistaOrganizaciones;
    public bool MostrarUsuarios => VistaActual == VistaUsuarios;
    public bool MostrarPermisos => VistaActual == VistaPermisos;

    public ICommand VolverCommand { get; }
    public ICommand CambiarVistaCommand { get; }
}
