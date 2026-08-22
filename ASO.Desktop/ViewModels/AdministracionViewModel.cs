using System.ComponentModel;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Administración del sistema: los usuarios del núcleo con sus permisos, y la ficha del propio
/// núcleo.
///
/// Los permisos NO son una pestaña aparte. Eran un CRUD de ajustes sueltos, y obligaba a trabajar
/// al revés: para saber qué podía hacer alguien había que recordar qué trae su rol y cruzarlo con
/// filas dispersas. Ahora al seleccionar un usuario se ven todos sus permisos al lado
/// (<see cref="PermisosDeUsuarioViewModel"/>), que es como se piensa la pregunta.
/// </summary>
public sealed class AdministracionViewModel : PantallaViewModelBase
{
    public const string VistaNucleo = "Nucleo";
    public const string VistaUsuarios = "Usuarios";

    private readonly IServicioDialogo _dialogos;

    /// <summary>
    /// Para poder deshacer un cambio de selección: si el usuario declina descartar sus cambios,
    /// hay que devolver la grilla a donde estaba, y para entonces ya se movió.
    /// </summary>
    private Models.Usuario? _usuarioMostrado;
    private bool _restaurandoSeleccion;

    public AdministracionViewModel(Modulo modulo)
        : this(modulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private AdministracionViewModel(Modulo modulo, IServicioDialogo dialogos, ISesionActual sesion)
        : base(modulo)
    {
        _dialogos = dialogos;

        Nucleo = new DatosNucleoViewModel(
            DataSourceFactory.CrearOrganizaciones(), dialogos, sesion);
        Usuarios = new UsuariosCrudViewModel(DataSourceFactory.CrearUsuarios(), dialogos, sesion);
        PermisosDeUsuario = new PermisosDeUsuarioViewModel(
            DataSourceFactory.CrearPermisosUsuario(), dialogos, sesion);

        Usuarios.PropertyChanged += OnUsuarioSeleccionado;

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public DatosNucleoViewModel Nucleo { get; }
    public UsuariosCrudViewModel Usuarios { get; }
    public PermisosDeUsuarioViewModel PermisosDeUsuario { get; }

    /// <summary>
    /// El panel de permisos sigue a la selección del padrón. Si hay cambios sin guardar se
    /// pregunta antes de descartarlos; al declinar se devuelve la selección a su sitio.
    /// </summary>
    private void OnUsuarioSeleccionado(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Usuarios.SelectedItem) || _restaurandoSeleccion)
            return;

        var nuevo = Usuarios.SelectedItem;
        if (ReferenceEquals(nuevo, _usuarioMostrado))
            return;

        if (PermisosDeUsuario.HayCambios
            && !_dialogos.Confirmar(
                "Cambios sin guardar",
                $"Hay permisos modificados de {PermisosDeUsuario.Usuario?.NombreUsuario} que no se han guardado. ¿Descartarlos?"))
        {
            _restaurandoSeleccion = true;
            Usuarios.SelectedItem = _usuarioMostrado;
            _restaurandoSeleccion = false;
            return;
        }

        _usuarioMostrado = nuevo;
        PermisosDeUsuario.Cargar(nuevo);
    }

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

    public ICommand CambiarVistaCommand { get; }
}
