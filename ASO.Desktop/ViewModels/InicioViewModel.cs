using System;
using System.Collections.Generic;
using System.Windows.Input;
using ASO.Desktop.Navigation;

namespace ASO.Desktop.ViewModels;

/// <summary>Lanzador: tarjeta por módulo con su lista de submódulos.</summary>
public sealed class InicioViewModel : ViewModelBase
{
    public event EventHandler<Modulo>? ModuloSolicitado;

    public IReadOnlyList<Modulo> Modulos => ModuloCatalogo.Modulos;

    public ICommand AbrirModuloCommand { get; }

    public InicioViewModel()
    {
        AbrirModuloCommand = new RelayCommand<Modulo>(m => ModuloSolicitado?.Invoke(this, m));
    }
}
