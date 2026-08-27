using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Nómina · Empleados: el centro lleva dos padrones distintos y esta pantalla los aloja
/// en una sola vista conmutable, sin mezclarlos.
///
/// - Administrativos: empleados de nómina del centro (taller, almacén, oficina).
/// - Campo: personal que firma la remesa y cuyo núcleo (C.O.D) determina el pago por destajo.
///
/// PROVISIONAL: pendiente de que el socio defina si una misma persona puede estar en ambos
/// padrones; hasta entonces se administran por separado y no se cruzan cédulas entre ellos.
/// </summary>
public sealed class EmpleadosViewModel : PantallaViewModelBase
{
    public const string VistaAdministrativos = "Administrativos";
    public const string VistaCampo = "Campo";

    public EmpleadosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private EmpleadosViewModel(Modulo modulo,
                               Submodulo submodulo,
                               IServicioDialogo dialogos,
                               ISesionActual sesion)
        : base(modulo, submodulo)
    {
        // Un solo HorarioService para los dos padrones: es de donde sale el historial de trabajo
        // de cualquiera de ellos, y la jornada ya sabe a qué padrón pertenece cada persona.
        var horarios = new HorarioService(DataSourceFactory.CrearJornadas(),
                                          DataSourceFactory.CrearEventosOperacion(),
                                          DataSourceFactory.CrearRemesas());

        Administrativos = new EmpleadosAdminViewModel(DataSourceFactory.CrearEmpleados(), dialogos, sesion, horarios);
        Campo = new PersonalCampoCrudViewModel(DataSourceFactory.CrearPersonalCampo(), dialogos, sesion, horarios);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public EmpleadosAdminViewModel Administrativos { get; }
    public PersonalCampoCrudViewModel Campo { get; }

    /// <summary>Los dos padrones de la pantalla; cada uno relee el suyo.</summary>
    public override void Recargar()
    {
        Administrativos.Recargar();
        Campo.Recargar();
    }

    private string _vistaActual = VistaAdministrativos;
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

    public bool MostrarAdministrativos => VistaActual == VistaAdministrativos;
    public bool MostrarCampo => VistaActual == VistaCampo;

    /// <summary>
    /// Las dos pestañas, enlazadas en DOS VÍAS al <c>IsChecked</c> de su botón, como en
    /// <see cref="AdministracionViewModel"/>. Antes la selección viajaba solo de la vista al
    /// ViewModel por <c>Command</c>, con <c>IsChecked="True"</c> a fuego en la primera: si algo
    /// cambiaba <see cref="VistaActual"/> desde el código, los botones no se enteraban.
    ///
    /// El setter solo actúa al marcar: al desmarcar ya hay otro botón del grupo encendiéndose.
    /// </summary>
    public bool EsAdministrativos
    {
        get => MostrarAdministrativos;
        set { if (value) VistaActual = VistaAdministrativos; }
    }

    public bool EsCampo
    {
        get => MostrarCampo;
        set { if (value) VistaActual = VistaCampo; }
    }

    public ICommand CambiarVistaCommand { get; }
}
