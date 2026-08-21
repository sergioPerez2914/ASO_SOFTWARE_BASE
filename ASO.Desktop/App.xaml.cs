using System.Windows;
using ASO.Desktop.Configuration;
using ASO.Desktop.Views;

namespace ASO.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // La base de datos se toca ya en el arranque (para saber si hay usuarios), así que un
        // problema de conexión se ve aquí y no a mitad de una navegación, disfrazado de otra cosa.
        if (!HayUsuarios())
        {
            Shutdown();
            return;
        }

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

    /// <summary>
    /// ¿Hay con quién iniciar sesión? Si la base está vacía ofrece crear el primer núcleo y su
    /// usuario desarrollador; si no se puede consultar, lo dice y no sigue.
    /// </summary>
    private static bool HayUsuarios()
    {
        try
        {
            if (DataSourceFactory.CrearUsuarios().ExisteAlguno())
                return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo conectar con la base de datos.\n\n{ex.Message}\n\n" +
                "Revisa la cadena de conexión en appsettings.local.json.",
                "Software ASO", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return new PrimerArranqueView().ShowDialog() == true;
    }
}
