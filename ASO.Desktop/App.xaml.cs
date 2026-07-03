using System.Configuration;
using System.Data;
using System.Windows;
using ASO.Desktop.Views;

namespace ASO.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var login = new LoginView();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var main = new MainWindow();
        MainWindow = main;
        main.Show();

        // A partir de aquí sí queremos que cerrar la última ventana cierre la app.
        ShutdownMode = ShutdownMode.OnLastWindowClose;
    }
}

