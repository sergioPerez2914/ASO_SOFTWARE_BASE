namespace ASO.Desktop.Models;

/// <summary>
/// Depósito de combustible del centro (cisterna principal, tanque de taller). Su existencia
/// sube con las recargas al proveedor y baja con cada vale despachado a una máquina.
///
/// PROVISIONAL: la medición se asume por contómetro (litros despachados). Si el socio mide
/// por aforo (regla de nivel), habrá que añadir la conversión y un ajuste de inventario.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class TanqueCombustible : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CapacidadL { get; set; }
    public decimal ExistenciaL { get; set; }
    public bool Activo { get; set; } = true;

    public decimal PorcentajeLleno => CapacidadL <= 0 ? 0 : ExistenciaL * 100m / CapacidadL;

    public string ExistenciaTexto => $"{ExistenciaL:N0} / {CapacidadL:N0} L";

    public string PorcentajeTexto => $"{PorcentajeLleno:N0} %";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public TanqueCombustible Clonar() => (TanqueCombustible)MemberwiseClone();
}
