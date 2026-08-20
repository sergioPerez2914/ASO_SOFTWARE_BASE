using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

public sealed class NavegacionEventArgs(Modulo modulo, Submodulo? submodulo) : EventArgs
{
    public Modulo Modulo { get; } = modulo;
    public Submodulo? Submodulo { get; } = submodulo;
}

public sealed class SubmoduloNavItem(Modulo modulo, Submodulo submodulo) : ViewModelBase
{
    public Modulo Modulo { get; } = modulo;
    public Submodulo Submodulo { get; } = submodulo;
    public string Nombre => Submodulo.Nombre;
    public string Icono => Submodulo.Icono;

    private bool _estaSeleccionado;
    public bool EstaSeleccionado
    {
        get => _estaSeleccionado;
        set => SetProperty(ref _estaSeleccionado, value);
    }
}

public sealed class ModuloNavItem : ViewModelBase
{
    public ModuloNavItem(Modulo modulo, ISesionActual sesion)
    {
        Modulo = modulo;
        Submodulos = [.. sesion.SubmodulosVisibles(modulo).Select(s => new SubmoduloNavItem(modulo, s))];
    }

    public Modulo Modulo { get; }
    public IReadOnlyList<SubmoduloNavItem> Submodulos { get; }
    public string Nombre => Modulo.Nombre;
    public string Icono => Modulo.Icono;
    public bool TieneSubmodulos => Submodulos.Count > 0;

    private bool _estaSeleccionado;
    public bool EstaSeleccionado
    {
        get => _estaSeleccionado;
        set => SetProperty(ref _estaSeleccionado, value);
    }

    /// <summary>
    /// El módulo es el que se está viendo, aunque el contenido sea uno de sus submódulos.
    /// Sirve para dejarle una marca sutil al padre sin robarle la barra al submódulo activo.
    /// </summary>
    private bool _estaActivo;
    public bool EstaActivo
    {
        get => _estaActivo;
        set => SetProperty(ref _estaActivo, value);
    }

    private bool _estaExpandido;
    public bool EstaExpandido
    {
        get => _estaExpandido;
        set
        {
            if (SetProperty(ref _estaExpandido, value))
            {
                OnPropertyChanged(nameof(GlifoChevron));
                OnPropertyChanged(nameof(TextoChevron));
            }
        }
    }

    /// <summary>Etiqueta del chevron para lectores de pantalla y tooltip.</summary>
    public string TextoChevron => EstaExpandido ? $"Contraer {Nombre}" : $"Expandir {Nombre}";

    public string GlifoChevron => EstaExpandido ? "" : "";
}

/// <summary>
/// Estado del menú lateral de dos niveles. Solo notifica la intención de navegar
/// (<see cref="NavegacionSolicitada"/>); quién muestra qué vista lo decide la ventana principal,
/// que a su vez confirma el estado visual llamando a <see cref="Sincronizar"/>.
/// </summary>
public sealed class SidebarViewModel : ViewModelBase
{
    public event EventHandler<NavegacionEventArgs>? NavegacionSolicitada;

    public IReadOnlyList<ModuloNavItem> Items { get; }

    public ICommand SeleccionarModuloCommand { get; }
    public ICommand SeleccionarSubmoduloCommand { get; }
    public ICommand AlternarExpansionCommand { get; }

    public SidebarViewModel() : this(SesionActual.Instancia) { }

    public SidebarViewModel(ISesionActual sesion)
    {
        // El menu se arma una vez por ventana y la ventana se reconstruye al entrar, al salir
        // y al cambiar de nucleo, asi que aqui basta con filtrar al construir.
        Items =
        [
            .. ModuloCatalogo.Fijados.Where(sesion.Ve).Select(m => new ModuloNavItem(m, sesion)),
            .. sesion.ModulosVisibles().Select(m => new ModuloNavItem(m, sesion))
        ];

        SeleccionarModuloCommand = new RelayCommand<ModuloNavItem>(item =>
            NavegacionSolicitada?.Invoke(this, new NavegacionEventArgs(item.Modulo, null)));

        SeleccionarSubmoduloCommand = new RelayCommand<SubmoduloNavItem>(item =>
            NavegacionSolicitada?.Invoke(this, new NavegacionEventArgs(item.Modulo, item.Submodulo)));

        // Plegar/desplegar es solo visual: no cambia de sección ni pide navegar.
        AlternarExpansionCommand = new RelayCommand<ModuloNavItem>(
            item => item.EstaExpandido = !item.EstaExpandido,
            item => item.TieneSubmodulos);
    }

    /// <summary>
    /// Refleja en el menú la sección que se está mostrando, venga del propio menú o de una
    /// tarjeta del dashboard. Al entrar a un módulo se despliega su lista de submódulos.
    /// </summary>
    public void Sincronizar(Modulo modulo, Submodulo? submodulo)
    {
        foreach (var item in Items)
        {
            var esModuloActivo = item.Modulo.Clave == modulo.Clave;

            item.EstaActivo = esModuloActivo;
            item.EstaSeleccionado = esModuloActivo && submodulo is null;
            item.EstaExpandido = esModuloActivo && item.TieneSubmodulos;

            foreach (var sub in item.Submodulos)
                sub.EstaSeleccionado = esModuloActivo && sub.Submodulo.Clave == submodulo?.Clave;
        }
    }
}
