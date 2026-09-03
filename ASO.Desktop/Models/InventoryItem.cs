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
/// </summary>
public class InventoryItem : IEntidad<string>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    /// <summary>Identidad para el esqueleto CRUD genérico: el artículo se identifica por su código.</summary>
    public string Id => Codigo;

    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    /// <summary>
    /// Existencia actual. Es <c>decimal</c> y no <c>int</c> porque hay artículos que se
    /// almacenan por metro, kilo o litro (mangueras, lubricantes) y admiten fracciones.
    /// </summary>
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
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

    public string StockTexto => $"{StockActual:N2} {Unidad}";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public InventoryItem Clonar() => (InventoryItem)MemberwiseClone();
}
