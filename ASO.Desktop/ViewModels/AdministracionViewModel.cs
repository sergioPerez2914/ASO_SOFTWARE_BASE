using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Administración del sistema: los usuarios del núcleo y los ajustes de permisos que los
/// apartan de lo que su rol trae por defecto.
///
/// Dos padrones y la ficha del núcleo, en una vista conmutable como <see cref="EmpleadosViewModel"/>.
/// No hay padrón de núcleos: una instalación atiende a uno solo, que nace en el primer arranque;
/// lo que sí hace falta es poder corregir sus datos, sobre todo el C.O.D.
/// </summary>
public sealed class AdministracionViewModel : PantallaViewModelBase
{
    public const string VistaNucleo = "Nucleo";
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
        Nucleo = new DatosNucleoViewModel(
            DataSourceFactory.CrearOrganizaciones(), dialogos, sesion);
        Usuarios = new UsuariosCrudViewModel(usuarios, dialogos, sesion);
        Permisos = new PermisosUsuarioCrudViewModel(
            DataSourceFactory.CrearPermisosUsuario(), usuarios, dialogos, sesion);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public DatosNucleoViewModel Nucleo { get; }
    public UsuariosCrudViewModel Usuarios { get; }
    public PermisosUsuarioCrudViewModel Permisos { get; }

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

    public bool MostrarNucleo => VistaActual == VistaNucleo;
    public bool MostrarUsuarios => VistaActual == VistaUsuarios;
    public bool MostrarPermisos => VistaActual == VistaPermisos;

    public ICommand CambiarVistaCommand { get; }
}
