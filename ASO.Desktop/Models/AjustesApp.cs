namespace ASO.Desktop.Models;

/// <summary>Tema visual de la aplicacion. La paleta de cada uno vive en <c>Services/Tema.cs</c>.</summary>
public enum TemaApp
{
    Claro,
    Oscuro
}

/// <summary>
/// Preferencias de quien usa la aplicacion en ESTA maquina. No son datos del negocio y no
/// pertenecen a ningun nucleo: viven en %AppData%\ASO\ajustes.json (ver
/// <c>Configuration/AjustesStoreJson.cs</c>), fuera de la base y fuera del repositorio.
///
/// Es un POCO plano a proposito: lo serializa System.Text.Json sin configuracion, y un campo
/// nuevo se agrega aqui y ya. Los archivos viejos siguen leyendose porque lo que falta toma el
/// valor por defecto de su propiedad.
/// </summary>
public class AjustesApp
{
    public TemaApp Tema { get; set; } = TemaApp.Claro;

    /// <summary>Factor de escala de toda la ventana (1,0 = 100 %). Ver <c>MainWindow.xaml</c>.</summary>
    public double EscalaInterfaz { get; set; } = 1.0;

    public bool RecordarUltimoUsuario { get; set; } = true;

    /// <summary>Solo el nombre de usuario, nunca la contrasenna.</summary>
    public string UltimoUsuario { get; set; } = string.Empty;

    /// <summary>Si no, la aplicacion abre siempre en Inicio.</summary>
    public bool AbrirEnUltimaSeccion { get; set; }

    /// <summary>Clave del modulo que se estaba viendo al cerrar.</summary>
    public string UltimaSeccion { get; set; } = string.Empty;

    /// <summary>
    /// Sobrescribe el umbral de alerta de consumo de <c>appsettings.json</c>, en fraccion
    /// (0,25 = 25 %). Null significa "usar el del archivo": no es lo mismo que cero, que seria
    /// marcar con alerta cualquier vale por encima del promedio.
    /// </summary>
    public decimal? UmbralAlertaConsumo { get; set; }

    public AjustesApp Clonar() => (AjustesApp)MemberwiseClone();
}
