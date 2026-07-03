using System;
using System.Windows;
using System.Windows.Controls;

namespace ASO.Desktop.Controls;

public partial class Sidebar : UserControl
{
    /// <summary>
    /// Se dispara cuando el usuario selecciona un módulo. El argumento es el
    /// identificador de sección tomado del <c>Tag</c> del ítem de navegación.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    public Sidebar()
    {
        InitializeComponent();
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string section })
            NavigationRequested?.Invoke(this, section);
    }
}
