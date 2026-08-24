namespace ASO.Desktop.Models;

/// <summary>
/// A qué apunta una línea de compra: el stock de combustible/aceite o el catálogo de
/// repuestos. Lo comparten las líneas de <see cref="Requisicion"/>, <see cref="OrdenCompra"/> y
/// la futura Recepción de mercancía, para que las tres sepan si el efecto de inventario recae
/// sobre <see cref="StockCombustible"/> o sobre <see cref="InventoryItem"/>.
/// </summary>
public enum TipoInsumo
{
    Combustible,
    Repuesto
}

/// <summary>
/// Qué se solicita cuando la línea es de combustible: diésel (para máquinas y transporte) o
/// lubricante — y si es lubricante, su grado/viscosidad va aparte
/// (<c>RequisicionLinea.TipoLubricante</c> / <c>OrdenCompraLinea.TipoLubricante</c>, p. ej.
/// "20W50"). No hay stock que elegir todavía: la empresa guarda el aceite en la presentación en
/// que llega (barril, garrafa), no en un envase común rastreable por catálogo.
/// </summary>
public enum TipoCombustible
{
    Diesel,
    Lubricante
}
