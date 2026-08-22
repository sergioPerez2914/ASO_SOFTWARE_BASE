using System.Globalization;
using System.Windows.Input;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Preferencias de cómo se comporta la aplicación en esta máquina.
///
/// A diferencia de Apariencia, aquí sí hay botón de guardar: el umbral es un número escrito a
/// mano, y aplicar cada tecla mientras se teclea dejaría un umbral del 2 % vivo un instante
/// camino del 25 %.
///
/// El umbral está detrás de un permiso (<see cref="Permisos.Configuracion.Preferencias"/>) y no
/// es celo de más: decide cuándo un vale de combustible se marca con alerta de consumo, así que
/// quien pueda subirlo puede apagarse sus propias alertas.
/// </summary>
public sealed class PreferenciasAppViewModel : ViewModelBase
{
    private const decimal UmbralMaximo = 500m;

    private readonly IServicioDialogo _dialogos;

    public PreferenciasAppViewModel(IServicioDialogo dialogos, ISesionActual sesion)
    {
        _dialogos = dialogos;

        PuedeAjustarOperacion = sesion.Puede(Permisos.Configuracion.Preferencias);

        _abrirEnUltimaSeccion = Ajustes.Actual.AbrirEnUltimaSeccion;
        _umbralTexto = TextoDe(Ajustes.Actual.UmbralAlertaConsumo);

        GuardarCommand = new RelayCommand(Guardar);
    }

    public bool PuedeAjustarOperacion { get; }

    /// <summary>Explica por qué el campo está gris, en vez de dejarlo apagado sin motivo.</summary>
    public bool MostrarAvisoSinPermiso => !PuedeAjustarOperacion;

    public ICommand GuardarCommand { get; }

    public string AyudaUmbral =>
        "Cuánto puede superar un vale al promedio histórico del activo antes de marcarse con " +
        $"alerta de consumo. Vacío = usar el valor de appsettings.json ({Porcentaje(Configuration.AppConfig.UmbralAlertaConsumo)} %).";

    private bool _abrirEnUltimaSeccion;
    public bool AbrirEnUltimaSeccion
    {
        get => _abrirEnUltimaSeccion;
        set => SetProperty(ref _abrirEnUltimaSeccion, value);
    }

    /// <summary>En porcentaje, que es como se piensa; el archivo lo guarda en fracción.</summary>
    private string _umbralTexto;
    public string UmbralTexto
    {
        get => _umbralTexto;
        set => SetProperty(ref _umbralTexto, value);
    }

    private void Guardar()
    {
        decimal? umbral = null;

        if (!string.IsNullOrWhiteSpace(UmbralTexto))
        {
            if (!decimal.TryParse(UmbralTexto.Trim().TrimEnd('%').Trim(),
                                  NumberStyles.Any, CultureInfo.CurrentCulture, out var porcentaje)
                && !decimal.TryParse(UmbralTexto.Trim().TrimEnd('%').Trim(),
                                     NumberStyles.Any, CultureInfo.InvariantCulture, out porcentaje))
            {
                _dialogos.Informar("Umbral no válido",
                    "Escribe el umbral como un número de porcentaje, por ejemplo 25. " +
                    "Déjalo vacío para usar el del archivo de configuración.");
                return;
            }

            if (porcentaje < 0 || porcentaje > UmbralMaximo)
            {
                _dialogos.Informar("Umbral fuera de rango",
                    $"El umbral debe estar entre 0 y {UmbralMaximo:0} %.");
                return;
            }

            umbral = porcentaje / 100m;
        }

        Ajustes.Actual.AbrirEnUltimaSeccion = AbrirEnUltimaSeccion;

        // Solo se toca si quien guarda tiene el permiso: sin esta guarda, un remesero que
        // cambiara "abrir en la última sección" arrastraría el umbral del cuadro deshabilitado.
        if (PuedeAjustarOperacion)
            Ajustes.Actual.UmbralAlertaConsumo = umbral;

        if (!Ajustes.Guardar())
        {
            _dialogos.Informar("No se pudo guardar",
                $"No se pudo escribir el archivo de preferencias:\n{Configuration.DataSourceFactory.CrearAjustesStore().Ruta}");
            return;
        }

        UmbralTexto = TextoDe(Ajustes.Actual.UmbralAlertaConsumo);
        _dialogos.Informar("Preferencias guardadas", "Los cambios ya están aplicados.");
    }

    private static string TextoDe(decimal? fraccion) =>
        fraccion is { } valor ? Porcentaje(valor) : string.Empty;

    private static string Porcentaje(decimal fraccion) =>
        (fraccion * 100m).ToString("0.##", CultureInfo.CurrentCulture);
}
