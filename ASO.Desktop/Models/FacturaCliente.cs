using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO.Desktop.Models;

/// <summary>
/// Estados de la factura al ingenio. "Vencida" no es un estado sino una condición derivada de
/// la fecha (ver <see cref="FacturaCliente.EstaVencida"/>): una factura emitida se vuelve
/// vencida sola al pasar su plazo, sin que nadie la toque.
/// </summary>
public enum EstadoFacturaCliente
{
    Borrador,
    Emitida,
    Cobrada,
    Anulada
}

/// <summary>
/// Factura de Cuentas por Cobrar: lo que el centro le cobra al ingenio por la caña entregada.
///
/// Se arma desde las remesas recibidas, con tres líneas por remesa — corte, alza y empuje, y
/// transporte — porque el reglamento atribuye cada servicio a un núcleo distinto y el ingenio
/// paga los tres por separado.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class FacturaCliente : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public string NumeroTexto => $"FC-{Id:D4}";

    // PROVISIONAL: cliente único (el central al que se entrega). Pendiente del maestro de
    // clientes si el socio confirma que se factura a más de un ingenio.
    public string ClienteNombre { get; set; } = "Central Azucarero Las Majaguas";

    // PROVISIONAL: plazo de crédito fijo. Pendiente de las condiciones reales del ingenio.
    public int DiasCredito { get; set; } = 30;

    public List<FacturaClienteLinea> Lineas { get; set; } = [];

    public EstadoFacturaCliente Estado { get; set; }
    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime? FechaCobro { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public decimal Total => Lineas.Sum(l => l.Monto);

    public decimal Toneladas => Lineas
        .Where(l => l.Servicio == ServicioZafra.Corte)
        .Sum(l => l.Toneladas);

    public string TotalTexto => Total.ToString("N2");

    /// <summary>Emitida y con el plazo cumplido. Es condición derivada, no un estado guardado.</summary>
    public bool EstaVencida =>
        Estado == EstadoFacturaCliente.Emitida
        && FechaVencimiento is { } vencimiento
        && vencimiento.Date < DateTime.Today;

    public string EstadoTexto => EstaVencida
        ? "Vencida"
        : Estado switch
        {
            EstadoFacturaCliente.Borrador => "Borrador",
            EstadoFacturaCliente.Emitida => "Emitida",
            EstadoFacturaCliente.Cobrada => "Cobrada",
            _ => "Anulada"
        };

    public string EmisionTexto => FechaEmision is { } fecha ? fecha.ToString("dd/MM/yyyy") : "—";

    public string VencimientoTexto => FechaVencimiento is { } fecha ? fecha.ToString("dd/MM/yyyy") : "—";

    public string RemesasTexto
    {
        get
        {
            var remesas = Lineas.Select(l => l.RemesaId).Distinct().Count();
            return remesas == 1 ? "1 remesa" : $"{remesas} remesas";
        }
    }

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public FacturaCliente Clonar()
    {
        var copia = (FacturaCliente)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Un servicio facturado de una remesa. <see cref="TarifaMonto"/> es copia de la tarifa
/// aplicada, no una referencia: la factura debe reimprimirse igual aunque el tarifario cambie.
/// </summary>
public class FacturaClienteLinea
{
    public int RemesaId { get; set; }
    public string FincaNombre { get; set; } = string.Empty;   // snapshot
    public DateTime FechaRecepcion { get; set; }

    public ServicioZafra Servicio { get; set; }

    /// <summary>Núcleo al que el reglamento atribuye este servicio en esta remesa.</summary>
    public string NucleoCodigo { get; set; } = string.Empty;

    public decimal Toneladas { get; set; }
    public decimal TarifaMonto { get; set; }

    public decimal Monto => Math.Round(Toneladas * TarifaMonto, 2);

    public string ServicioTexto => Servicio switch
    {
        ServicioZafra.Corte => "Corte",
        ServicioZafra.AlzaEmpuje => "Alza y empuje",
        ServicioZafra.Transporte => "Transporte",
        _ => "Otro"
    };

    public string RemesaTexto => $"Nº {RemesaId}";

    public string ToneladasTexto => $"{Toneladas:N2} t";

    public string MontoTexto => Monto.ToString("N2");

    public FacturaClienteLinea Clonar() => (FacturaClienteLinea)MemberwiseClone();
}
