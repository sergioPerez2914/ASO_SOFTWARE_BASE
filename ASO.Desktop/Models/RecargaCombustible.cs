using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Recarga de una cisterna: el combustible que entra al centro desde el proveedor.
///
/// Es de solo inserción y aplica su efecto al registrarse (no tiene borrador ni confirmación):
/// cuando el camión del proveedor descarga, el combustible ya está en el tanque. Si el socio
/// define un formato con recepción y verificación, pasará a documento con estados como el vale.
///
/// PROVISIONAL: el proveedor se guarda como texto. Pasará a ProveedorId cuando exista Cuentas
/// por Pagar, y entonces la recarga podrá generar la factura de compra.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class RecargaCombustible : IEntidad<int>
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public int TanqueId { get; set; }
    public string TanqueNombre { get; set; } = string.Empty;  // snapshot

    public decimal Litros { get; set; }
    public decimal? CostoTotal { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public string LitrosTexto => $"{Litros:N2} L";

    public string CostoTexto => CostoTotal is { } costo ? costo.ToString("N2") : "—";

    public decimal? CostoPorLitro => CostoTotal is { } costo && Litros > 0 ? costo / Litros : null;

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public RecargaCombustible Clonar() => (RecargaCombustible)MemberwiseClone();
}
