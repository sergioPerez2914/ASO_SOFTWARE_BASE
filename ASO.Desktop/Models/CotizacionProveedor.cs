using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Precio cotizado por un proveedor para atender una <see cref="Requisicion"/>. No es un
/// documento con máquina de estados propia — es el apunte que deja quien compara precios antes
/// de armar la orden de compra, para que la comparación quede a la vista y no solo en la memoria
/// de quien decidió. Se captura una fila por proveedor consultado; la ganadora la marca
/// <see cref="OrdenCompra.CotizacionSeleccionadaId"/>.
/// </summary>
public class CotizacionProveedor : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int RequisicionId { get; set; }

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;   // snapshot

    public decimal MontoTotal { get; set; }
    public string Notas { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string MontoTexto => MontoTotal.ToString("N2");

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public CotizacionProveedor Clonar() => (CotizacionProveedor)MemberwiseClone();
}
