using System.Windows;
using System.Windows.Input;
using ASO.Desktop.ViewModels;

namespace ASO.Desktop.Views;

public partial class LoginView : Window
{
    public LoginViewModel ViewModel { get; } = new();

    public LoginView()
    {
        InitializeComponent();
        DataContext = ViewModel;
        UsuarioBox.Focus();
    }

    private void OnIniciarSesion(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IntentarIniciarSesion(PasswordBox.Password))
        {
            DialogResult = true;
            return;
        }

        PasswordBox.Clear();
        PasswordBox.Focus();
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnIniciarSesion(sender, e);
    }
}
