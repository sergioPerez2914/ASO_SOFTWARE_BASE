using System;
using System.Windows.Input;
using ASO.Desktop.Navigation;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Contenedor de un submódulo todavía sin implementar: encabezado con la ruta
/// módulo · submódulo y vuelta al resumen del módulo.
/// </summary>
public sealed class SubmoduloViewModel : ViewModelBase
{
    public event EventHandler? VolverSolicitado;

    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }

    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }

    public SubmoduloViewModel(Modulo modulo, Submodulo submodulo)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
    }
}
