using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Los datos del núcleo donde está instalado el sistema. No es un padrón: una instalación
/// atiende a un solo núcleo, que nace en el primer arranque. Aquí solo se corrigen sus datos,
/// y el que importa es el <b>C.O.D</b>: es lo que estampan las remesas, las liquidaciones y las
/// facturas, así que si está mal, sale mal en todos los papeles que se emitan de aquí en más.
///
/// Al guardar se refresca <see cref="Ambito"/>: los documentos siguientes deben llevar el
/// código nuevo sin esperar a que el usuario vuelva a entrar.
/// </summary>
public sealed class DatosNucleoViewModel : ViewModelBase
{
    private readonly IOrganizacionDataSource _organizaciones;
    private readonly IServicioDialogo _dialogos;

    public DatosNucleoViewModel(IOrganizacionDataSource organizaciones,
                                IServicioDialogo dialogos,
                                ISesionActual sesion)
    {
        _organizaciones = organizaciones;
        _dialogos = dialogos;

        PuedeEditar = sesion.Puede(Permisos.Nucleo.Editar);

        if (Ambito.Actual is { } nucleo)
        {
            _codigo = nucleo.Codigo;
            _codigoCam = nucleo.CodigoCam;
            _nombre = nucleo.Nombre;
        }

        GuardarCommand = new RelayCommand(Guardar, () => PuedeEditar);
    }

    public bool PuedeEditar { get; }

    public ICommand GuardarCommand { get; }

    private string _codigo = string.Empty;
    public string Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    private string _codigoCam = string.Empty;
    public string CodigoCam
    {
        get => _codigoCam;
        set => SetProperty(ref _codigoCam, value);
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private void Guardar()
    {
        if (Ambito.Actual is not { } actual)
            return;

        if (string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(CodigoCam)
            || string.IsNullOrWhiteSpace(Nombre))
        {
            _dialogos.Informar("Datos incompletos",
                "El código interno, el C.O.D y el nombre del núcleo son obligatorios.");
            return;
        }

        var actualizado = actual.Clonar();
        actualizado.Codigo = Codigo.Trim().ToUpperInvariant();
        actualizado.CodigoCam = CodigoCam.Trim().ToUpperInvariant();
        actualizado.Nombre = Nombre.Trim();

        try
        {
            _organizaciones.Update(actualizado);
            Ambito.Actualizar(actualizado);

            Codigo = actualizado.Codigo;
            CodigoCam = actualizado.CodigoCam;
            Nombre = actualizado.Nombre;

            _dialogos.Informar("Núcleo actualizado",
                "Los documentos que se emitan a partir de ahora llevan el C.O.D nuevo. " +
                "Los ya emitidos conservan el que tenían.");
        }
        catch (Exception ex)
        {
            _dialogos.Informar("No se pudo guardar", ex.Message);
        }
    }
}
