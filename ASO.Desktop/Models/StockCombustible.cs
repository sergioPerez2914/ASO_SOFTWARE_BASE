namespace ASO.Desktop.Models;

/// <summary>
/// Existencia general de un producto de combustible/aceite del centro (p. ej. "Diesel",
/// "Aceite hidráulico"). No representa un envase físico: en la empresa el aceite se queda en la
/// presentación en que llega (barril, garrafa), no se vierte a una cisterna común. Mientras no se
/// defina con el socio cómo rastrear esas presentaciones, cada producto lleva una capacidad y
/// existencia general en litros — puede haber varias filas, una por producto.
///
/// PROVISIONAL: la medición se asume por contómetro (litros despachados). Si el socio mide
/// por aforo (regla de nivel), habrá que añadir la conversión y un ajuste de inventario.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class StockCombustible : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    /// <summary>El producto (p. ej. "Diesel", "Aceite hidráulico"), no un nombre de envase.</summary>
    public string Nombre { get; set; } = string.Empty;

    public decimal CapacidadL { get; set; }
    public decimal ExistenciaL { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Falso para un producto sin cisterna física (p. ej. el "Diesel" general que
    /// resuelve solo <c>ComprasService.ConfirmarRecepcion</c>): sin capacidad no hay contra qué
    /// medir un porcentaje, así que la tarjeta no debe mostrar la barra ni el "/ tope".</summary>
    public bool TieneCapacidadFija => CapacidadL > 0;

    public decimal PorcentajeLleno => CapacidadL <= 0 ? 0 : ExistenciaL * 100m / CapacidadL;

    public string ExistenciaTexto => TieneCapacidadFija
        ? $"{ExistenciaL:N0} / {CapacidadL:N0} L"
        : $"{ExistenciaL:N0} L";

    public string PorcentajeTexto => $"{PorcentajeLleno:N0} %";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public StockCombustible Clonar() => (StockCombustible)MemberwiseClone();
}
