using System;
using System.Windows.Input;
using ASO.Desktop.Navigation;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Pantalla de relleno con la ruta módulo · submódulo y vuelta al resumen. Cubre los dos
/// motivos por los que un submódulo puede no mostrarse: todavía no está construido, o el
/// usuario no tiene permiso para verlo. El mensaje lo decide quien la crea.
/// </summary>
public sealed class SubmoduloViewModel : ViewModelBase
{
    public event EventHandler? VolverSolicitado;

    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }

    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public string Titulo { get; private init; } = "Submódulo en construcción";
    public string Detalle { get; private init; } = "Todavía no tiene pantallas ni fuente de datos conectada.";

    public ICommand VolverCommand { get; }

    public SubmoduloViewModel(Modulo modulo, Submodulo submodulo)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// El submódulo existe pero este usuario no llega a él. Se muestra en vez de la pantalla
    /// real: es la última barrera, por si algún camino de navegación se saltara el filtro
    /// del menú.
    /// </summary>
    public static SubmoduloViewModel SinPermiso(Modulo modulo, Submodulo submodulo) =>
        new(modulo, submodulo)
        {
            Titulo = "Sin permiso para esta sección",
            Detalle = "Tu rol no tiene acceso a este submódulo. " +
                      "Si lo necesitas, pídeselo al administrador de tu núcleo."
        };
}
