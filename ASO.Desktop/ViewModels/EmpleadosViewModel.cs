using System;
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
public sealed class EmpleadosViewModel : ViewModelBase
{
    public const string VistaAdministrativos = "Administrativos";
    public const string VistaCampo = "Campo";

    public event EventHandler? VolverSolicitado;

    public EmpleadosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private EmpleadosViewModel(Modulo modulo,
                               Submodulo submodulo,
                               IServicioDialogo dialogos,
                               ISesionActual sesion)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        Administrativos = new EmpleadosAdminViewModel(DataSourceFactory.CrearEmpleados(), dialogos, sesion);
        Campo = new PersonalCampoCrudViewModel(DataSourceFactory.CrearPersonalCampo(), dialogos, sesion);

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }
    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public EmpleadosAdminViewModel Administrativos { get; }
    public PersonalCampoCrudViewModel Campo { get; }

    private string _vistaActual = VistaAdministrativos;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            if (SetProperty(ref _vistaActual, value))
            {
                OnPropertyChanged(nameof(MostrarAdministrativos));
                OnPropertyChanged(nameof(MostrarCampo));
            }
        }
    }

    public bool MostrarAdministrativos => VistaActual == VistaAdministrativos;
    public bool MostrarCampo => VistaActual == VistaCampo;

    public ICommand VolverCommand { get; }
    public ICommand CambiarVistaCommand { get; }
}
