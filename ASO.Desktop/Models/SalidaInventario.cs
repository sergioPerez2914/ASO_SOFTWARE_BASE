using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados del documento. <c>Borrador</c> es la ventana de corrección; al confirmar se
/// descuenta el stock y la salida queda inmutable.
/// </summary>
public enum EstadoSalida
{
    Borrador,
    Confirmada,
    Anulada
}

/// <summary>
/// Salida de inventario: documento de movimiento que saca un artículo del almacén y le
/// imputa su costo a un activo o a un mantenimiento. Sigue el mismo patrón que la remesa
/// (cabecera, máquina de estados, inmutabilidad tras confirmar, auditoría).
///
/// El costo unitario se copia al confirmar y no se vuelve a tocar: si el artículo se
/// revaloriza después, esta salida sigue valiendo lo que valía el día que salió del almacén.
///
/// PROVISIONAL: una línea por salida. Pasará a cabecera + líneas si el socio aporta un
/// formato de vale de almacén con varios artículos.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class SalidaInventario : IEntidad<int>
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    // --- Artículo (snapshots: el documento debe leerse igual aunque el catálogo cambie) ---
    public string ArticuloCodigo { get; set; } = string.Empty;
    public string ArticuloNombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    /// <summary>Copia del costo del artículo al momento de confirmar. Antes de eso vale 0.</summary>
    public decimal CostoUnitario { get; set; }

    public decimal CostoTotal => Cantidad * CostoUnitario;

    // --- Destino del consumo ---
    public int? ActivoId { get; set; }
    public string ActivoEtiqueta { get; set; } = string.Empty;  // snapshot

    /// <summary>Mantenimiento al que se imputa el costo (opcional).</summary>
    public int? MantenimientoId { get; set; }

    public string Motivo { get; set; } = string.Empty;

    // --- Documento ---
    public EstadoSalida Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    /// <summary>Quedó autorizada por un administrador pese a no haber existencia suficiente.</summary>
    public bool StockForzado { get; set; }

    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public string EstadoTexto => Estado switch
    {
        EstadoSalida.Borrador => "Borrador",
        EstadoSalida.Confirmada => "Confirmada",
        _ => "Anulada"
    };

    public string DestinoTexto => string.IsNullOrWhiteSpace(ActivoEtiqueta)
        ? "Almacén / otro"
        : ActivoEtiqueta;

    public string CantidadTexto => $"{Cantidad:N2} {Unidad}";

    public string CostoTotalTexto => Estado == EstadoSalida.Borrador ? "—" : CostoTotal.ToString("N2");

    public string MantenimientoTexto => MantenimientoId is { } id ? $"Nº {id}" : "—";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public SalidaInventario Clonar() => (SalidaInventario)MemberwiseClone();
}
