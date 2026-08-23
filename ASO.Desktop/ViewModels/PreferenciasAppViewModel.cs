using System.Globalization;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Preferencias de cómo se comporta la aplicación en esta máquina.
///
/// <b>Todo se guarda al cambiarlo.</b> Antes convivían dos criterios dentro de la misma pantalla:
/// "recordar mi usuario" se aplicaba al instante y "abrir en la última sección" —una casilla
/// visualmente idéntica, a dos centímetros— exigía pulsar Guardar. No había forma de saber cuál
/// era cuál mirándolas, así que la mitad de los cambios se perdían al salir.
///
/// El umbral era la razón de aquel botón: es un número escrito a mano, y aplicar cada tecla
/// dejaría un umbral del 2 % vivo un instante camino del 25 %. Se resuelve validando mientras se
/// escribe y guardando solo cuando el texto es un número válido, que además avisa del error donde
/// está el error en vez de en un cuadro de diálogo aparte.
///
/// El umbral está detrás de un permiso (<see cref="Permisos.Configuracion.Preferencias"/>) y no
/// es celo de más: decide cuándo un vale de combustible se marca con alerta de consumo, así que
/// quien pueda subirlo puede apagarse sus propias alertas.
/// </summary>
public sealed class PreferenciasAppViewModel : ViewModelBase
{
    private const decimal UmbralMaximo = 500m;

    public PreferenciasAppViewModel(IServicioDialogo dialogos, ISesionActual sesion)
    {
        PuedeAjustarOperacion = sesion.Puede(Permisos.Configuracion.Preferencias);

        _abrirEnUltimaSeccion = Ajustes.Actual.AbrirEnUltimaSeccion;
        _umbralTexto = TextoDe(Ajustes.Actual.UmbralAlertaConsumo);
    }

    public bool PuedeAjustarOperacion { get; }

    /// <summary>Explica por qué el campo está gris, en vez de dejarlo apagado sin motivo.</summary>
    public bool MostrarAvisoSinPermiso => !PuedeAjustarOperacion;

    public AvisoGuardado Aviso { get; } = new();

    public string AyudaUmbral =>
        "Cuánto puede superar un vale al promedio histórico del activo antes de marcarse con " +
        $"alerta de consumo. Vacío = usar el valor de appsettings.json ({Porcentaje(Configuration.AppConfig.UmbralAlertaConsumo)} %).";

    private bool _abrirEnUltimaSeccion;
    public bool AbrirEnUltimaSeccion
    {
        get => _abrirEnUltimaSeccion;
        set
        {
            if (!SetProperty(ref _abrirEnUltimaSeccion, value))
                return;

            Ajustes.Actual.AbrirEnUltimaSeccion = value;
            Persistir();
        }
    }

    private string _errorUmbral = string.Empty;

    /// <summary>Vacío mientras el texto sea un umbral válido; el campo lo muestra debajo.</summary>
    public string ErrorUmbral
    {
        get => _errorUmbral;
        private set => SetProperty(ref _errorUmbral, value);
    }

    /// <summary>En porcentaje, que es como se piensa; el archivo lo guarda en fracción.</summary>
    private string _umbralTexto;
    public string UmbralTexto
    {
        get => _umbralTexto;
        set
        {
            if (!SetProperty(ref _umbralTexto, value) || !PuedeAjustarOperacion)
                return;

            if (!Interpretar(value, out var umbral, out var error))
            {
                // Se avisa, pero no se guarda: el ajuste anterior sigue en pie hasta que el
                // texto vuelva a ser un numero.
                ErrorUmbral = error;
                return;
            }

            ErrorUmbral = string.Empty;
            Ajustes.Actual.UmbralAlertaConsumo = umbral;
            Persistir();
        }
    }

    /// <summary>
    /// Lee el umbral escrito a mano. Admite el símbolo de porcentaje y las dos convenciones
    /// decimales, porque en una misma máquina conviven "25,5" y "25.5" según de dónde se copie.
    /// </summary>
    private static bool Interpretar(string texto, out decimal? umbral, out string error)
    {
        umbral = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
            return true;                       // vacío es válido: manda appsettings.json

        var limpio = texto.Trim().TrimEnd('%').Trim();

        if (!decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.CurrentCulture, out var porcentaje)
            && !decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out porcentaje))
        {
            error = "Escribe un número, por ejemplo 25. Déjalo vacío para usar el del archivo de configuración.";
            return false;
        }

        if (porcentaje < 0 || porcentaje > UmbralMaximo)
        {
            error = $"Debe estar entre 0 y {UmbralMaximo:0} %.";
            return false;
        }

        umbral = porcentaje / 100m;
        return true;
    }

    private void Persistir()
    {
        if (Ajustes.Guardar())
        {
            Aviso.Mostrar();
            return;
        }

        // Escribir el archivo es lo unico que puede fallar aqui, y callarlo dejaria creer que
        // el ajuste sobrevive al cierre de la aplicacion cuando no lo hace.
        ErrorUmbral = "No se pudo escribir el archivo de preferencias: " +
                      Configuration.DataSourceFactory.CrearAjustesStore().Ruta;
    }

    private static string TextoDe(decimal? fraccion) =>
        fraccion is { } valor ? Porcentaje(valor) : string.Empty;

    private static string Porcentaje(decimal fraccion) =>
        (fraccion * 100m).ToString("0.##", CultureInfo.CurrentCulture);
}
