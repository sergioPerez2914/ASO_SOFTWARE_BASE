using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Que ViewModel abre cada submodulo. La vista la resuelve WPF por DataTemplate
    /// (ver <c>Styles/PantallaTemplates.xaml</c>), asi que aqui no se nombra ninguna.
    ///
    /// Antes esto era un switch de catorce casos que decian los tres mismos pasos con otros
    /// nombres. Una pantalla nueva se da de alta con una linea aqui y su plantilla alla.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Func<Modulo, Submodulo, IPantalla>> Pantallas =
        new Dictionary<string, Func<Modulo, Submodulo, IPantalla>>
        {
            ["Operaciones.Registro"] = (m, s) => new RegistroOperacionViewModel(m, s),
            ["Operaciones.Seguimiento"] = (m, s) => new SeguimientoViewModel(m, s),
            ["Operaciones.FincasNucleos"] = (m, s) => new FincasYNucleosViewModel(m, s),
            ["Flota.Gestion"] = (m, s) => new GestionFlotaViewModel(m, s),
            ["Flota.Mantenimiento"] = (m, s) => new MantenimientoViewModel(m, s),
            ["Inventario.Repuestos"] = (m, s) => new RepuestosViewModel(m, s),
            ["Inventario.Combustible"] = (m, s) => new CombustibleViewModel(m, s),
            ["Inventario.Producto"] = (m, s) => new ProductoViewModel(m, s),
            ["Nomina.Empleados"] = (m, s) => new EmpleadosViewModel(m, s),
            ["Nomina.Horarios"] = (m, s) => new HorariosViewModel(m, s),
            ["Nomina.Liquidaciones"] = (m, s) => new LiquidacionesViewModel(m, s),
            ["Finanzas.Tarifas"] = (m, s) => new TarifasViewModel(m, s),
            ["Finanzas.CuentasPorCobrar"] = (m, s) => new CuentasPorCobrarViewModel(m, s),
            ["Finanzas.CuentasPorPagar"] = (m, s) => new CuentasPorPagarViewModel(m, s),
        };

    private object CrearVistaModulo(Modulo modulo)
    {
        // Los modulos fijados (Peticiones, Usuarios) no tienen submodulos, asi que la guarda
        // de CrearVistaSubmodulo no los alcanza: se comprueban aqui.
        if (!SesionActual.Instancia.Ve(modulo))
            return CrearInicio();

        if (modulo.Clave == ModuloCatalogo.Administracion.Clave)
            return Conectar(new AdministracionViewModel(modulo), ModuloCatalogo.Inicio);

        if (modulo.Clave == ModuloCatalogo.Peticiones.Clave)
            return Conectar(new PeticionesViewModel(modulo), ModuloCatalogo.Inicio);

        if (modulo.Clave == ModuloCatalogo.Inicio.Clave)
            return CrearInicio();

        var dashboard = new ModuloDashboardViewModel(modulo);
        dashboard.SubmoduloSolicitado += (_, s) => Navegar(modulo, s);
        return dashboard;
    }

    private InicioViewModel CrearInicio()
    {
        var inicio = new InicioViewModel();
        inicio.ModuloSolicitado += (_, m) => Navegar(m, null);
        return inicio;
    }

    /// <summary>
    /// Los submodulos ya implementados se enrutan a su pantalla real; el resto cae en el
    /// marcador de posicion hasta que tengan la suya.
    /// </summary>
    private object CrearVistaSubmodulo(Modulo modulo, Submodulo submodulo)
    {
        // Ultima barrera: el menu ya oculta lo que el rol no ve, pero a una pantalla tambien
        // se llega desde el lanzador de Inicio y desde las tarjetas del dashboard. Comprobarlo
        // aqui, en el unico punto por el que pasan las tres, cierra los tres caminos a la vez.
        if (!SesionActual.Instancia.Ve(submodulo))
            return Conectar(SubmoduloViewModel.SinPermiso(modulo, submodulo), modulo);

        var pantalla = Pantallas.TryGetValue(submodulo.Clave, out var crear)
            ? crear(modulo, submodulo)
            : new SubmoduloViewModel(modulo, submodulo);

        return Conectar(pantalla, modulo);
    }

    /// <summary>Deja la pantalla lista para devolver el control cuando el usuario pida volver.</summary>
    private IPantalla Conectar(IPantalla pantalla, Modulo destinoAlVolver)
    {
        pantalla.VolverSolicitado += (_, _) => Navegar(destinoAlVolver, null);
        return pantalla;
    }
}
