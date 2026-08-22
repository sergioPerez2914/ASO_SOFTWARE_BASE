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

        AplicarEscala();
        Ajustes.Cambiaron += AplicarEscala;
        Closed += OnCerrada;

        Navegar(SeccionDeArranque(), null);

        MostrarCabecera();
    }

    /// <summary>
    /// Al cerrar se anota dónde se quedó, y solo aquí: <see cref="Navegar"/> lo lleva en
    /// memoria porque escribir el archivo en cada clic del menú sería un archivo por
    /// navegación. Cerrar sesión también pasa por aquí — la ventana se cierra y se abre otra.
    ///
    /// La baja del evento va en el mismo sitio: <c>Ajustes.Cambiaron</c> es estático y vive más
    /// que la ventana, así que una ventana cerrada que siguiera suscrita se quedaría colgando
    /// de él intentando escalar un árbol que ya no existe.
    /// </summary>
    private void OnCerrada(object? sender, EventArgs e)
    {
        Ajustes.Cambiaron -= AplicarEscala;

        if (Ajustes.Actual.AbrirEnUltimaSeccion)
            Ajustes.Guardar();
    }

    /// <summary>
    /// Escala de la interfaz. Se reaplica cada vez que cambian los ajustes, no solo al abrir,
    /// para que el selector de Configuración se vea al instante.
    /// </summary>
    private void AplicarEscala()
    {
        var escala = Ajustes.Actual.EscalaInterfaz;
        EscalaInterfaz.ScaleX = EscalaInterfaz.ScaleY = escala is >= 0.5 and <= 3 ? escala : 1;
    }

    /// <summary>
    /// Dónde abrir. Por defecto Inicio; con la preferencia activada, donde se quedó la última
    /// vez. Si esa sección ya no existe o el rol dejó de verla, se cae a Inicio en vez de
    /// dejar la ventana en una pantalla vacía — de eso se encarga <see cref="CrearVistaModulo"/>.
    /// </summary>
    private static Modulo SeccionDeArranque()
    {
        if (!Ajustes.Actual.AbrirEnUltimaSeccion)
            return ModuloCatalogo.Inicio;

        return ModuloCatalogo.BuscarModulo(Ajustes.Actual.UltimaSeccion) ?? ModuloCatalogo.Inicio;
    }

    /// <summary>
    /// Quién está dentro y sobre qué núcleo. El núcleo se muestra siempre porque saber
    /// dónde se está escribiendo importa antes de guardar nada.
    /// </summary>
    private void MostrarCabecera()
    {
        var sesion = SesionActual.Instancia;

        if (sesion.UsuarioActual is { } usuario)
            UsuarioActualLabel.Text = $"{usuario.NombreCompleto} · {usuario.RolTexto}";

        if (Ambito.Actual is { } nucleo)
        {
            NucleoActualLabel.Text = nucleo.Etiqueta;
            NucleoChip.Visibility = Visibility.Visible;
        }
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

        // Solo en memoria: el archivo se escribe al guardar desde Configuración, no una vez
        // por clic del menú.
        Ajustes.Actual.UltimaSeccion = modulo.Clave;

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
            ["Operaciones.Fincas"] = (m, s) => new FincasViewModel(m, s),
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

        if (modulo.Clave == ModuloCatalogo.Configuracion.Clave)
            return Conectar(new ConfiguracionViewModel(modulo), ModuloCatalogo.Inicio);

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
