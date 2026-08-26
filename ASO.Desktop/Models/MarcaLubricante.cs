namespace ASO.Desktop.Models;

/// <summary>
/// Catálogo de marcas reales de lubricante (Mobil, Castrol, PDV, etc.). Se siembra con las
/// marcas de uso común en el mercado venezolano y se completa con "+ Nuevo" desde donde se
/// elige la marca (Orden de Compra, editor de <see cref="Lubricante"/>).
///
/// A propósito NO implementa <see cref="IDeOrganizacion"/>: es un hecho del mundo real (esta
/// marca existe), no un dato de negocio del núcleo — mismo criterio que <see cref="Organizacion"/>
/// es la única tabla hoy sin ese filtro. Esto también permite sembrarlo por migración
/// (<c>HasData</c> corre en tiempo de diseño, sin ámbito de organización activo). Si algún día
/// una sola base de datos sirve a varios núcleos de verdad al mismo tiempo, esto se revisa.
/// </summary>
public class MarcaLubricante : IEntidad<int>
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public MarcaLubricante Clonar() => (MarcaLubricante)MemberwiseClone();
}
