using System;

namespace ASO.Desktop.Models;

/// <summary>
/// A quién mira la tarifa: <c>Cobro</c> es lo que el centro le factura al ingenio;
/// <c>PagoDestajo</c> es lo que el centro le paga al núcleo o al trabajador por el mismo
/// servicio. Se guardan en una sola tabla porque comparten servicio y unidad, y porque la
/// diferencia entre ambas es el margen: tenerlas juntas hace evidente esa comparación.
/// </summary>
public enum AmbitoTarifa
{
    Cobro,
    PagoDestajo
}

/// <summary>Unidad sobre la que se aplica el monto.</summary>
public enum UnidadTarifa
{
    Tonelada,
    Hora,
    Viaje,

    /// <summary>
    /// Disponible para el tarifario, todavía sin uso: la remesa no captura distancia.
    /// PROVISIONAL: pendiente de que el socio defina de dónde sale el kilometraje.
    /// </summary>
    Kilometro
}

/// <summary>
/// Servicio de la zafra al que aplica la tarifa. Los tres primeros son los que el
/// reglamento de remesas atribuye a un núcleo distinto y que determinan el pago.
/// </summary>
public enum ServicioZafra
{
    Corte,
    AlzaEmpuje,
    Transporte,
    Otro
}

/// <summary>
/// Tarifa vigente para un servicio: cuánto se cobra o se paga por unidad
/// (p. ej. "Corte de caña" → 3.50 por tonelada al ingenio, 0.90 por tonelada al núcleo).
///
/// La vigencia es obligatoria: una factura o una liquidación reimpresa no puede cambiar de
/// monto porque el tarifario se actualizó después. Por eso los documentos guardan el monto
/// aplicado como copia (<c>TarifaMonto</c>) y esta entidad solo dice qué regía en cada fecha.
/// </summary>
public class Tarifa : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public string Concepto { get; set; } = string.Empty;
    public ServicioZafra Servicio { get; set; }
    public AmbitoTarifa Ambito { get; set; }
    public UnidadTarifa Unidad { get; set; } = UnidadTarifa.Tonelada;
    public decimal MontoPorUnidad { get; set; }

    public DateTime VigenteDesde { get; set; } = DateTime.Today;

    /// <summary><c>null</c> = sigue vigente sin fecha de cierre.</summary>
    public DateTime? VigenteHasta { get; set; }

    public bool Activa { get; set; } = true;
    public string Notas { get; set; } = string.Empty;

    public string ServicioTexto => Servicio switch
    {
        ServicioZafra.Corte => "Corte",
        ServicioZafra.AlzaEmpuje => "Alza y empuje",
        ServicioZafra.Transporte => "Transporte",
        _ => "Otro"
    };

    public string AmbitoTexto => Ambito == AmbitoTarifa.Cobro ? "Cobro al ingenio" : "Pago por destajo";

    public string UnidadTexto => Unidad switch
    {
        UnidadTarifa.Tonelada => "Tonelada",
        UnidadTarifa.Hora => "Hora",
        UnidadTarifa.Viaje => "Viaje",
        _ => "Kilómetro"
    };

    public string UnidadCorta => Unidad switch
    {
        UnidadTarifa.Tonelada => "t",
        UnidadTarifa.Hora => "h",
        UnidadTarifa.Viaje => "viaje",
        _ => "km"
    };

    public string MontoTexto => $"{MontoPorUnidad:N2} / {UnidadCorta}";

    public string VigenciaTexto => VigenteHasta is { } hasta
        ? $"{VigenteDesde:dd/MM/yyyy} – {hasta:dd/MM/yyyy}"
        : $"desde {VigenteDesde:dd/MM/yyyy}";

    /// <summary>¿Rige en la fecha indicada? No mira <see cref="Activa"/>: eso lo decide el servicio.</summary>
    public bool RigeEn(DateTime fecha) =>
        VigenteDesde.Date <= fecha.Date && (VigenteHasta is null || fecha.Date <= VigenteHasta.Value.Date);

    public string EstadoTexto =>
        !Activa ? "Inactiva" :
        VigenteHasta is { } hasta && hasta.Date < DateTime.Today ? "Vencida" :
        VigenteDesde.Date > DateTime.Today ? "Futura" :
        "Vigente";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public Tarifa Clonar() => (Tarifa)MemberwiseClone();
}
