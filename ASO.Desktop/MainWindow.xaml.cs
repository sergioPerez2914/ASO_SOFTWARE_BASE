using System.Windows;
using ASO.Desktop.Configuration;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;
using ASO.Desktop.ViewModels;
using ASO.Desktop.Views;

namespace ASO.Desktop;

/// <summary>
/// Shell de la aplicación: menú de módulos/submódulos a la izquierda y, a la derecha,
/// el resumen del módulo o el submódulo abierto.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainSidebar.NavegacionSolicitada += (_, e) => Navegar(e.Modulo, e.Submodulo);
        Navegar(ModuloCatalogo.Inicio, null);

        MostrarCabecera();
    }

    /// <summary>
    /// Quién está dentro y sobre qué núcleo. El núcleo se muestra siempre — no solo al
    /// Desarrollador — porque saber en qué organización se está escribiendo importa antes
    /// de guardar nada.
    /// </summary>
    private void MostrarCabecera()
    {
        var sesion = SesionActual.Instancia;

        if (sesion.UsuarioActual is { } usuario)
            UsuarioActualLabel.Text = $"{usuario.NombreCompleto} · {usuario.RolTexto}";

        if (Ambito.OrganizacionId is { } organizacionId)
        {
            var organizacion = DataSourceFactory.CrearOrganizaciones().GetById(organizacionId);
            NucleoActualLabel.Text = organizacion?.Etiqueta ?? $"Núcleo {organizacionId}";
            NucleoChip.Visibility = Visibility.Visible;
        }

        CambiarNucleoBoton.Visibility = sesion.Puede(Permisos.Organizaciones.Cambiar)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Cambiar de núcleo reconstruye la ventana entera, igual que al iniciar sesión: el menú,
    /// los dashboards y las listas se arman en sus constructores leyendo el ámbito, así que
    /// refrescarlos en sitio sería reimplementar ese arranque en otro camino.
    /// </summary>
    private void OnCambiarNucleo(object sender, RoutedEventArgs e)
    {
        if (!SesionActual.Instancia.Puede(Permisos.Organizaciones.Cambiar))
            return;

        var selector = new SeleccionOrganizacionView { Owner = this };
        if (selector.ShowDialog() != true)
            return;

        var nuevaVentana = new MainWindow();
        Application.Current.MainWindow = nuevaVentana;
        nuevaVentana.Show();
        Close();
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

    /// <summary>
    /// Único punto de cambio de sección: sin submódulo muestra el resumen del módulo,
    /// con submódulo abre su pantalla. El menú lateral se sincroniza con lo que se muestra.
    /// </summary>
    private void Navegar(Modulo modulo, Submodulo? submodulo)
    {
        MainSidebar.Sincronizar(modulo, submodulo);

        ContentArea.Content = submodulo is not null
            ? CrearVistaSubmodulo(modulo, submodulo)
            : CrearVistaModulo(modulo);
    }

    private object CrearVistaModulo(Modulo modulo)
    {
        // Los módulos fijados (Peticiones, Usuarios) no tienen submódulos, así que la guarda
        // de CrearVistaSubmodulo no los alcanza: se comprueban aquí.
        if (!SesionActual.Instancia.Ve(modulo))
            return new InicioView { DataContext = CrearInicio() };

        if (modulo.Clave == ModuloCatalogo.Administracion.Clave)
        {
            var administracion = new AdministracionViewModel(modulo);
            administracion.VolverSolicitado += (_, _) => Navegar(ModuloCatalogo.Inicio, null);
            return new AdministracionView { DataContext = administracion };
        }

        if (modulo.Clave == ModuloCatalogo.Peticiones.Clave)
        {
            var peticiones = new PeticionesViewModel(modulo);
            peticiones.VolverSolicitado += (_, _) => Navegar(ModuloCatalogo.Inicio, null);
            return new PeticionesView { DataContext = peticiones };
        }

        if (modulo.Clave == ModuloCatalogo.Inicio.Clave)
            return new InicioView { DataContext = CrearInicio() };

        var dashboard = new ModuloDashboardViewModel(modulo);
        dashboard.SubmoduloSolicitado += (_, s) => Navegar(modulo, s);
        return new ModuloDashboardView { DataContext = dashboard };
    }

    private InicioViewModel CrearInicio()
    {
        var inicio = new InicioViewModel();
        inicio.ModuloSolicitado += (_, m) => Navegar(m, null);
        return inicio;
    }

    /// <summary>
    /// Los submódulos ya implementados se enrutan a su pantalla real; el resto cae en el
    /// marcador de posición hasta que tengan la suya.
    /// </summary>
    private object CrearVistaSubmodulo(Modulo modulo, Submodulo submodulo)
    {
        // Última barrera: el menú ya oculta lo que el rol no ve, pero a una pantalla también
        // se llega desde el lanzador de Inicio y desde las tarjetas del dashboard. Comprobarlo
        // aquí, en el único punto por el que pasan las tres, cierra los tres caminos a la vez.
        if (!SesionActual.Instancia.Ve(submodulo))
        {
            var denegado = SubmoduloViewModel.SinPermiso(modulo, submodulo);
            denegado.VolverSolicitado += (_, _) => Navegar(modulo, null);
            return new SubmoduloView { DataContext = denegado };
        }

        switch (submodulo.Clave)
        {
            case "Operaciones.Registro":
                var registro = new RegistroOperacionViewModel(modulo, submodulo);
                registro.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new RegistroOperacionView { DataContext = registro };

            case "Operaciones.Seguimiento":
                var seguimiento = new SeguimientoViewModel(modulo, submodulo);
                seguimiento.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new SeguimientoView { DataContext = seguimiento };

            case "Operaciones.FincasNucleos":
                var fincasNucleos = new FincasYNucleosViewModel(modulo, submodulo);
                fincasNucleos.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new FincasYNucleosView { DataContext = fincasNucleos };

            case "Flota.Gestion":
                var gestion = new GestionFlotaViewModel(modulo, submodulo);
                gestion.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new GestionFlotaView { DataContext = gestion };

            case "Flota.Mantenimiento":
                var mantenimiento = new MantenimientoViewModel(modulo, submodulo);
                mantenimiento.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new MantenimientoView { DataContext = mantenimiento };

            case "Nomina.Empleados":
                var empleados = new EmpleadosViewModel(modulo, submodulo);
                empleados.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new EmpleadosView { DataContext = empleados };

            case "Finanzas.Tarifas":
                var tarifas = new TarifasViewModel(modulo, submodulo);
                tarifas.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new TarifasView { DataContext = tarifas };

            case "Inventario.Repuestos":
                var repuestos = new RepuestosViewModel(modulo, submodulo);
                repuestos.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new RepuestosView { DataContext = repuestos };

            case "Inventario.Combustible":
                var combustible = new CombustibleViewModel(modulo, submodulo);
                combustible.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new CombustibleView { DataContext = combustible };

            case "Nomina.Horarios":
                var horarios = new HorariosViewModel(modulo, submodulo);
                horarios.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new HorariosView { DataContext = horarios };

            case "Nomina.Liquidaciones":
                var liquidaciones = new LiquidacionesViewModel(modulo, submodulo);
                liquidaciones.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new LiquidacionesView { DataContext = liquidaciones };

            case "Finanzas.CuentasPorCobrar":
                var cxc = new CuentasPorCobrarViewModel(modulo, submodulo);
                cxc.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new CuentasPorCobrarView { DataContext = cxc };

            case "Finanzas.CuentasPorPagar":
                var cxp = new CuentasPorPagarViewModel(modulo, submodulo);
                cxp.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new CuentasPorPagarView { DataContext = cxp };

            case "Inventario.Producto":
                var producto = new ProductoViewModel(modulo, submodulo);
                producto.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new ProductoView { DataContext = producto };

            default:
                var vm = new SubmoduloViewModel(modulo, submodulo);
                vm.VolverSolicitado += (_, _) => Navegar(modulo, null);
                return new SubmoduloView { DataContext = vm };
        }
    }
}
