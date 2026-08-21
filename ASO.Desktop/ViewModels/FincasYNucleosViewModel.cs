using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Operaciones · Fincas y Núcleos: los dos catálogos que alimentan los combos en cascada del
/// Registro de Operación (finca → lote → tablón, y los tres núcleos que determinan el pago),
/// alojados en una sola vista conmutable, igual que <see cref="EmpleadosViewModel"/> hace con
/// sus dos padrones.
/// </summary>
public sealed class FincasYNucleosViewModel : PantallaViewModelBase
{
    public const string VistaFincas = "Fincas";
    public const string VistaNucleos = "Nucleos";

    public FincasYNucleosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private FincasYNucleosViewModel(Modulo modulo,
                                    Submodulo submodulo,
                                    IServicioDialogo dialogos,
                                    ISesionActual sesion)
        : base(modulo, submodulo)
    {
        Fincas = new FincaCrudViewModel(DataSourceFactory.CrearFincas(), dialogos, sesion);
        Nucleos = new NucleoCrudViewModel(DataSourceFactory.CrearNucleos(), dialogos, sesion);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public FincaCrudViewModel Fincas { get; }
    public NucleoCrudViewModel Nucleos { get; }

    private string _vistaActual = VistaFincas;
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

    public bool MostrarFincas => VistaActual == VistaFincas;
    public bool MostrarNucleos => VistaActual == VistaNucleos;

    public ICommand CambiarVistaCommand { get; }
}
