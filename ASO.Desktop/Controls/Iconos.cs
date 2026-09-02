namespace ASO.Desktop.Controls;

/// <summary>
/// Catalogo de glifos de la interfaz. Un solo sitio donde vive el punto de codigo de cada
/// icono, con un nombre que dice de que es: antes estaban sueltos por el XAML y por los
/// modelos como literales sin nombre, ilegibles sin abrir el visor de fuentes.
///
/// Los valores son de la fuente <b>Phosphor</b> (regular), empotrada en
/// <c>Assets/Fonts/Phosphor.ttf</c> y expuesta como el token <c>IconFont</c> de Tokens.xaml.
/// Sustituye a Segoe MDL2 Assets, que solo existe en Windows 10/11 y no viaja con el proyecto.
///
/// Para anadir uno: busca el icono en phosphoricons.com, toma su punto de codigo de
/// <c>@phosphor-icons/web/src/regular/style.css</c> y declaralo aqui con el nombre del
/// concepto -no el de la forma-, para que cambiar de dibujo no obligue a renombrar.
///
/// En XAML: <c>{x:Static controls:Iconos.Loque}</c>. En C#, directamente.
/// </summary>
public static class Iconos
{
    // =============================== Navegacion ===============================
    // Un glifo por modulo y submodulo del catalogo. Dos entradas del menu no comparten
    // glifo a proposito: en una lista vertical el icono es lo primero que se lee.

    public const string Inicio =           "\ue2c2";     // house
    public const string Peticiones =       "\ue4aa";     // tray

    public const string Operaciones =      "\ue198";     // clipboard-text
    public const string Registro =         "\ue23a";     // file-text
    public const string Seguimiento =      "\ue39c";     // path
    public const string Fincas =           "\uec70";     // farm

    public const string Flota =            "\ue4b4";     // truck
    public const string GestionFlota =     "\uecd6";     // garage
    public const string Mantenimiento =    "\ue5d4";     // wrench
    public const string Telemetria =       "\uee74";     // speedometer

    public const string Inventario =       "\ue390";     // package
    public const string Repuestos =        "\ue38c";     // nut
    public const string Combustible =      "\ue8ce";     // gas-can
    public const string Producto =         "\uebae";     // plant
    public const string Compras =          "\ue41e";     // shopping-cart

    public const string Nomina =           "\ue68e";     // users-three
    public const string Liquidaciones =    "\ue588";     // money
    public const string Empleados =        "\ue4d6";     // users
    public const string Horarios =         "\ue10a";     // calendar-blank

    public const string Finanzas =         "\ue54c";     // currency-circle-dollar
    public const string CuentasPorCobrar = "\uea8c";     // hand-coins
    public const string CuentasPorPagar =  "\uee42";     // invoice
    public const string Tarifas =          "\ue478";     // tag
    public const string Banco =            "\ue0b4";     // bank

    public const string Administracion =   "\ue40c";     // shield-check
    public const string Configuracion =    "\ue270";     // gear

    // ================================= Flota =================================
    // Tipos de activo. Phosphor no trae cosechadora ni alzadora de cana; se toma lo mas
    // cercano de su set agricola y de obra, que en este contexto se leen sin ambiguedad.

    public const string Cosechadora =      "\uec68";     // grains
    public const string Tractor =          "\uec6e";     // tractor
    public const string Alzadora =         "\ued48";     // crane
    public const string Camion =           Flota;        // truck
    public const string Vehiculo =         "\ue826";     // van

    // =============================== Operacion ===============================
    // Hitos de la linea de tiempo de una remesa.

    public const string CargaInicio =      "\ue066";     // arrow-line-up
    public const string CargaFin =         Inventario;   // package
    public const string CambioTurno =      "\ue756";     // user-switch
    public const string Ubicacion =        "\ue316";     // map-pin
    public const string Pesaje =           "\ue750";     // scales
    public const string Factura =          "\ue3ec";     // receipt
    public const string Cobro =            CuentasPorCobrar;// hand-coins

    // =========================== Acciones y estados ===========================

    public const string Nota =             "\ue348";     // note
    public const string Confirmacion =     "\ue184";     // check-circle
    public const string Anulacion =        "\ue4f8";     // x-circle
    public const string Edicion =          "\ue3b4";     // pencil-simple
    public const string Buscar =           "\ue30c";     // magnifying-glass
    public const string Limpiar =          "\ue4f6";     // x

    // ========================= Documentos y terceros =========================

    public const string Requisicion =      "\ueadc";     // list-checks
    public const string OrdenCompra =      Compras;      // shopping-cart
    public const string Recepcion =        Inventario;   // package
    public const string Lubricante =       "\ue210";     // drop
    public const string Proveedor =        "\ue582";     // handshake
    public const string Cuenta =           "\ue68a";     // wallet
    public const string Usuarios =         Empleados;    // users
    public const string Zafra =            "\ue7b4";     // calendar-dots
    public const string Calendario =       Horarios;     // calendar-blank

    // =============================== Chevrones ===============================
    // Plegado del menu, separador de migas y navegacion del calendario.

    public const string ChevronArriba =    "\ue13c";     // caret-up
    public const string ChevronAbajo =     "\ue136";     // caret-down
    public const string ChevronIzquierda = "\ue138";     // caret-left
    public const string ChevronDerecha =   "\ue13a";     // caret-right
}
