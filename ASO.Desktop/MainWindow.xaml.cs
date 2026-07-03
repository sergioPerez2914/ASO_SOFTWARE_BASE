using System.Windows;
using ASO.Desktop.Services;
using ASO.Desktop.Views;

namespace ASO.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainSidebar.NavigationRequested += OnNavigationRequested;
        ContentArea.Content = new DashboardView(); // sección inicial: Inicio

        var usuario = SesionActual.Instancia.UsuarioActual;
        if (usuario is not null)
            UsuarioActualLabel.Text = $"{usuario.NombreCompleto} · {usuario.Rol}";
    }

    private void OnCerrarSesion(object sender, RoutedEventArgs e)
    {
        SesionActual.Instancia.CerrarSesion();

        var login = new LoginView();
        if (login.ShowDialog() == true)
        {
            var nuevaVentana = new MainWindow();
            Application.Current.MainWindow = nuevaVentana;
            nuevaVentana.Show();
        }
        else
        {
            Application.Current.Shutdown();
            return;
        }

        Close();
    }

    private void OnNavigationRequested(object? sender, string section)
    {
        ContentArea.Content = section switch
        {
            "Inicio" => new DashboardView(),
            "Inventario" => new InventoryView(),
            "Operaciones" => new PlaceholderView(
                "Operaciones · Cosecha",
                "Registro diario, tickets de pesaje, frentes de corte y tiempos muertos.",
                "Fase 1", ""),
            "Flota" => new PlaceholderView(
                "Flota y activos",
                "Hoja de vida, horómetros/odómetros y disponibilidad de la flota.",
                "Fase 1", ""),
            "Combustible" => new PlaceholderView(
                "Combustible",
                "Despacho por cisterna, rendimiento L/ton y alertas de consumo.",
                "Fase 1", ""),
            "Mantenimiento" => new PlaceholderView(
                "Mantenimiento",
                "Órdenes de trabajo y mantenimiento preventivo por horas de uso.",
                "Fase 2", ""),
            "Finanzas" => new PlaceholderView(
                "Finanzas",
                "Cuentas por cobrar y por pagar, bancos y flujo de caja.",
                "Fase 3", ""),
            "Nomina" => new PlaceholderView(
                "Nómina y destajo",
                "Tarifas de destajo, turnos de zafra y liquidaciones.",
                "Fase 4", ""),
            "Reportes" => new PlaceholderView(
                "Reportes",
                "Producción diaria, consumo de combustible y estados financieros.",
                "Fase 5", ""),
            "Maestros" => new EmpleadosView(),
            "Configuracion" => new PlaceholderView(
                "Configuración",
                "Usuarios, roles, parámetros del sistema y auditoría.",
                "", ""),
            _ => new PlaceholderView(section, "Sección no reconocida.", "")
        };
    }
}
