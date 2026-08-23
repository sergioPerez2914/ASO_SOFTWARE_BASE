using System.Windows;
using ASO.Desktop.Services;
using ASO.Desktop.ViewModels;

namespace ASO.Desktop.Views;

public partial class PrimerArranqueView : Window
{
    public PrimerArranqueViewModel ViewModel { get; } = new();

    public PrimerArranqueView()
    {
        InitializeComponent();
        DataContext = ViewModel;
        EscalaVentana.Aplicar(this);
        UsuarioBox.Focus();
    }

    private void OnCrear(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Crear(PasswordBox.Password))
            DialogResult = true;
    }
}
