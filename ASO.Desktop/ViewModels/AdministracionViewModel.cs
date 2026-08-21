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
public sealed class AdministracionViewModel : PantallaViewModelBase
{
    public const string VistaOrganizaciones = "Organizaciones";
    public const string VistaUsuarios = "Usuarios";
    public const string VistaPermisos = "Permisos";

    public AdministracionViewModel(Modulo modulo)
        : this(modulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private AdministracionViewModel(Modulo modulo, IServicioDialogo dialogos, ISesionActual sesion)
        : base(modulo)
    {
        var usuarios = DataSourceFactory.CrearUsuarios();
        Organizaciones = new OrganizacionCrudViewModel(
            DataSourceFactory.CrearOrganizaciones(), dialogos, sesion);
        Usuarios = new UsuariosCrudViewModel(usuarios, dialogos, sesion);
        Permisos = new PermisosUsuarioCrudViewModel(
            DataSourceFactory.CrearPermisosUsuario(), usuarios, dialogos, sesion);

        PuedeVerOrganizaciones = sesion.Puede(Services.Permisos.Organizaciones.Crear);
        VistaActual = PuedeVerOrganizaciones ? VistaOrganizaciones : VistaUsuarios;

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

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
            // Notifica todas: enumerar aqui un OnPropertyChanged por cada Mostrar… es
            // la lista que se queda corta el dia que se agrega un padron mas.
            if (SetProperty(ref _vistaActual, value))
                OnTodasLasPropiedadesCambiaron();
        }
    }

    public bool MostrarOrganizaciones => VistaActual == VistaOrganizaciones;
    public bool MostrarUsuarios => VistaActual == VistaUsuarios;
    public bool MostrarPermisos => VistaActual == VistaPermisos;

    public ICommand CambiarVistaCommand { get; }
}
