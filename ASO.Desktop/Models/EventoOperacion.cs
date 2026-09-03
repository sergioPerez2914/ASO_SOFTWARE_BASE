using System;
using ASO.Desktop.Controls;

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
    Anulacion,

    // Los miembros nuevos van SIEMPRE al final, aunque ocurran antes en el ciclo de vida:
    // `Tipo` se persiste como int, asi que declarar uno en medio reinterpretaria las filas ya
    // guardadas y una nota pasaria a leerse como otra cosa. El orden de la linea de tiempo ya
    // no depende de esta declaracion, sino de OrdenCicloVida.
    Facturacion,
    Cobro,
    Liquidacion,
    Edicion,
    Peticion
}

/// <summary>
/// Evento del seguimiento de una remesa.
///
/// Los eventos que se pueden leer de un documento NO se almacenan:
/// <see cref="Services.SeguimientoService"/> los deriva en vivo —de la propia remesa (registro,
/// carga, confirmación, llegada, pesaje, anulación) y de los documentos que la citan (factura,
/// liquidación, petición de cambio)—, así que siempre están en sincronía y llevan <c>Id = 0</c>.
///
/// Solo se guardan los hechos que no dejan huella en ningún documento: cambios de turno,
/// mantenimientos, notas, ediciones del borrador y la liberación de una remesa al anular su
/// factura (ese sí borra el campo que lo delataba).
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

    /// <summary>Quién lo registró. Los eventos derivados del documento van sin autor.</summary>
    public string Autor { get; set; } = string.Empty;

    /// <summary>
    /// Id del documento que originó el evento, para poder abrir su ficha desde la línea de
    /// tiempo: la jornada de un cambio de turno, el registro de un mantenimiento.
    ///
    /// No hace falta guardar además de qué tipo es: <see cref="Tipo"/> ya lo dice. Los eventos
    /// anteriores a la Fase 14 lo llevan nulo y su ficha se queda en descripción y autor.
    /// </summary>
    public int? OrigenId { get; set; }

    // Los tres switches que siguen NO llevan arco de descarte, y es deliberado: sin `_ =>`, el
    // compilador avisa (CS8509) en cuanto se declare un tipo de evento y se olvide mapearlo aquí.
    // Antes había descarte y caía en "Remesa anulada", así que un tipo nuevo se habría disfrazado
    // de anulación en silencio, que es el peor fallo posible en una línea de tiempo.
    //
    // A cambio hay que callar CS8524, que avisa de los enteros que no corresponden a ningún
    // miembro: solo pueden venir de una fila corrupta, y ahí preferimos la excepción al disimulo.
#pragma warning disable CS8524

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
        TipoEventoOperacion.Anulacion => "Remesa anulada",
        TipoEventoOperacion.Facturacion => "Facturación",
        TipoEventoOperacion.Cobro => "Cobro de la factura",
        TipoEventoOperacion.Liquidacion => "Liquidación",
        TipoEventoOperacion.Edicion => "Remesa editada",
        TipoEventoOperacion.Peticion => "Petición de cambio"
    };

    /// <summary>Glifo que representa el tipo. Ver <see cref="Iconos"/>.</summary>
    public string Glifo => Tipo switch
    {
        TipoEventoOperacion.Registro => Iconos.Registro,
        TipoEventoOperacion.InicioCarga => Iconos.CargaInicio,
        TipoEventoOperacion.FinCarga => Iconos.CargaFin,
        TipoEventoOperacion.CambioTurno => Iconos.CambioTurno,
        TipoEventoOperacion.Mantenimiento => Iconos.Mantenimiento,
        TipoEventoOperacion.Nota => Iconos.Nota,
        TipoEventoOperacion.Confirmacion => Iconos.Confirmacion,
        TipoEventoOperacion.LlegadaCentral => Iconos.Ubicacion,
        TipoEventoOperacion.Pesaje => Iconos.Pesaje,
        TipoEventoOperacion.Anulacion => Iconos.Anulacion,
        TipoEventoOperacion.Facturacion => Iconos.Factura,
        TipoEventoOperacion.Cobro => Iconos.Cobro,
        TipoEventoOperacion.Liquidacion => Iconos.Liquidaciones,
        TipoEventoOperacion.Edicion => Iconos.Edicion,
        TipoEventoOperacion.Peticion => Iconos.Peticiones
    };

    /// <summary>
    /// Posición en el ciclo de vida, para desempatar dos eventos de la misma hora (la llegada al
    /// central precede a su pesaje).
    ///
    /// Va aparte del orden de declaración del enum a propósito: los miembros nuevos tienen que
    /// declararse al final para no reinterpretar lo ya guardado, pero ocurren en mitad de la
    /// historia. Aquí se dice dónde va cada uno sin tocar el valor persistido.
    /// </summary>
    public int OrdenCicloVida => Tipo switch
    {
        TipoEventoOperacion.Registro => 0,
        TipoEventoOperacion.Edicion => 1,
        TipoEventoOperacion.Peticion => 2,
        TipoEventoOperacion.InicioCarga => 3,
        TipoEventoOperacion.CambioTurno => 4,
        TipoEventoOperacion.Mantenimiento => 5,
        TipoEventoOperacion.FinCarga => 6,
        TipoEventoOperacion.Nota => 7,
        TipoEventoOperacion.Confirmacion => 8,
        TipoEventoOperacion.LlegadaCentral => 9,
        TipoEventoOperacion.Pesaje => 10,
        TipoEventoOperacion.Anulacion => 11,
        TipoEventoOperacion.Facturacion => 12,
        TipoEventoOperacion.Cobro => 13,
        TipoEventoOperacion.Liquidacion => 14
    };

#pragma warning restore CS8524

    public string FechaHoraTexto => FechaHora.ToString("HH:mm · dd/MM/yyyy");
}
