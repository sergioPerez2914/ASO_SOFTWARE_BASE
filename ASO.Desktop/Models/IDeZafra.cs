namespace ASO.Desktop.Models;

/// <summary>
/// Marca las entidades cuyo documento pertenece a una <see cref="Zafra"/> concreta. Calco de
/// <see cref="IDeOrganizacion"/>: declarada aquí, pero todavía sin implementarla en ningún
/// documento — eso es una fase posterior (aplicar el filtro real), pendiente de confirmar con
/// el socio qué pasa con un documento que cruza el cierre de una zafra. Ver
/// <c>Services/ZafraActiva.cs</c> y <c>BD/DbContext.cs</c>.
/// </summary>
public interface IDeZafra
{
    int ZafraId { get; set; }
}
