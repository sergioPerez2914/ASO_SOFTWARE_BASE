namespace ASO.Desktop.Models;

/// <summary>
/// Unidad de transporte que traslada la caña del campo al central. La placa es el dato
/// que identifica la unidad en la remesa.
///
/// Hoy se proyecta desde el catálogo único de flota (ver <see cref="Services.VehiculoDataSourceAdapter"/>);
/// el maestro real es <see cref="ActivoFlota"/>.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class Vehiculo : IEntidad<int>
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public string Etiqueta => $"{Placa} · {Descripcion}";
}
