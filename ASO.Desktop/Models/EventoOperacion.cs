using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Tipos de evento de la línea de tiempo. El orden de declaración sigue el ciclo de vida del
/// documento y sirve de desempate cuando dos eventos comparten la misma hora (p. ej. la llegada
/// al central y el pesaje, que se registran juntos).
/// </summary>
public enum TipoEventoOperacion
{
    Registro,
    InicioCarga,
    FinCarga,
    CambioTurno,
    Mantenimiento,
    Nota,
    Confirmacion,
    LlegadaCentral,
    Pesaje,
    Anulacion
}

/// <summary>
/// Evento del seguimiento de una remesa.
///
/// Los eventos del ciclo de vida del documento (registro, carga, confirmación, llegada, pesaje,
/// anulación) NO se almacenan: <see cref="Services.SeguimientoService"/> los deriva en vivo de los
/// campos de la propia remesa, así que siempre están en sincronía y llevan <c>Id = 0</c>.
/// Solo se guardan los que no viven en el documento: cambios de turno, mantenimientos y notas.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class EventoOperacion : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public int RemesaId { get; set; }
    public TipoEventoOperacion Tipo { get; set; }
    public DateTime FechaHora { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Quién lo registró. Solo lo llevan las notas manuales; los de sistema van vacíos.</summary>
    public string Autor { get; set; } = string.Empty;

    public string EtiquetaTipo => Tipo switch
    {
        TipoEventoOperacion.Registro => "Remesa registrada",
        TipoEventoOperacion.InicioCarga => "Inicio de carga",
        TipoEventoOperacion.FinCarga => "Fin de carga",
        TipoEventoOperacion.CambioTurno => "Cambio de turno",
        TipoEventoOperacion.Mantenimiento => "Mantenimiento",
        TipoEventoOperacion.Nota => "Nota",
        TipoEventoOperacion.Confirmacion => "Remesa confirmada",
        TipoEventoOperacion.LlegadaCentral => "Llegada al central",
        TipoEventoOperacion.Pesaje => "Pesaje en romana",
        _ => "Remesa anulada"
    };

    /// <summary>Glifo de Segoe MDL2 Assets que representa el tipo.</summary>
    public string Glifo => Tipo switch
    {
        TipoEventoOperacion.Registro => "",        // Document
        TipoEventoOperacion.InicioCarga => "",     // StockUp
        TipoEventoOperacion.FinCarga => "",        // Package
        TipoEventoOperacion.CambioTurno => "",     // People
        TipoEventoOperacion.Mantenimiento => "",   // Repair
        TipoEventoOperacion.Nota => "",            // ClipboardList
        TipoEventoOperacion.Confirmacion => "",    // CheckMark
        TipoEventoOperacion.LlegadaCentral => "",  // MapPin
        TipoEventoOperacion.Pesaje => "",          // Calculator
        _ => ""                                    // Cancel
    };

    public string FechaHoraTexto => FechaHora.ToString("HH:mm · dd/MM/yyyy");
}
