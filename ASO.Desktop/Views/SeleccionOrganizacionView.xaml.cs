using System.Windows;
using ASO.Desktop.ViewModels;

namespace ASO.Desktop.Views;

public partial class SeleccionOrganizacionView : Window
{
    public SeleccionOrganizacionViewModel ViewModel { get; } = new();

    public SeleccionOrganizacionView()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void OnAceptar(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Aplicar())
            DialogResult = true;
    }

    private void OnCancelar(object sender, RoutedEventArgs e) => DialogResult = false;
}
