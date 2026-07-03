namespace ASO.Desktop.Models;

/// <summary>
/// Estado de existencias de un artículo respecto a su stock mínimo.
/// </summary>
public enum StockStatus
{
    Ok,
    Bajo,
    Agotado
}

/// <summary>
/// Repuesto o consumible de taller/almacén.
/// Modelo de presentación temporal; se alineará con la entidad
/// <c>ArticuloInventario</c> del dominio cuando exista la capa de datos.
/// </summary>
public class InventoryItem
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public decimal CostoUnitario { get; set; }

    public decimal ValorTotal => StockActual * CostoUnitario;

    public StockStatus Estado =>
        StockActual == 0 ? StockStatus.Agotado :
        StockActual <= StockMinimo ? StockStatus.Bajo :
        StockStatus.Ok;

    public string EstadoTexto => Estado switch
    {
        StockStatus.Agotado => "Agotado",
        StockStatus.Bajo => "Stock bajo",
        _ => "Disponible"
    };
}
