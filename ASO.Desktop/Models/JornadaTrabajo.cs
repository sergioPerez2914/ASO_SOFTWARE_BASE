using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Turno de la jornada.
/// PROVISIONAL: dos turnos. Pendiente del cuadro de turnos real del socio (la zafra suele
/// trabajar a tres relevos en pico de cosecha).
/// </summary>
public enum TurnoJornada
{
    Diurno,
    Nocturno
}

/// <summary>Padrón del que sale la persona: son dos catálogos distintos que no se mezclan.</summary>
public enum TipoPersonal
{
    Administrativo,
    Campo
}

/// <summary>
/// Jornada trabajada por una persona: entrada, salida y turno. Es la fuente de las horas que
/// luego liquida la nómina.
///
/// Es un registro de solo inserción: se abre al entrar y se cierra al salir, pero no se edita
/// ni se elimina. Una asistencia que se puede reescribir no sirve como base de un pago, y
/// además el modelo de solo-inserción es el que hace viable la sincronización cuando la
/// captura se haga desde el campo.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class JornadaTrabajo : IEntidad<int>
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    // --- Persona (snapshots: la jornada debe leerse igual aunque el padrón cambie) ---
    public TipoPersonal TipoPersonal { get; set; }
    public int PersonaId { get; set; }
    public string PersonaNombre { get; set; } = string.Empty;
    public string CargoORol { get; set; } = string.Empty;
    public string NucleoCodigo { get; set; } = string.Empty;  // solo personal de campo

    public TurnoJornada Turno { get; set; }

    public DateTime HoraEntrada { get; set; }
    public DateTime? HoraSalida { get; set; }

    public string Observacion { get; set; } = string.Empty;

    public int CreadoPorId { get; set; }
    public DateTime FechaRegistro { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras; todo se filtra por la zafra activa.

    public bool EstaAbierta => HoraSalida is null;

    public decimal? HorasTrabajadas => HoraSalida is { } salida
        ? Math.Round((decimal)(salida - HoraEntrada).TotalHours, 2)
        : null;

    public string HorasTexto => HorasTrabajadas is { } horas ? $"{horas:N2} h" : "—";

    public string TurnoTexto => Turno == TurnoJornada.Diurno ? "Diurno" : "Nocturno";

    public string TipoPersonalTexto => TipoPersonal == TipoPersonal.Administrativo ? "Administrativo" : "Campo";

    public string EstadoTexto => EstaAbierta ? "Abierta" : "Cerrada";

    public string EntradaTexto => HoraEntrada.ToString("dd/MM/yyyy HH:mm");

    public string SalidaTexto => HoraSalida is { } salida ? salida.ToString("dd/MM/yyyy HH:mm") : "—";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public JornadaTrabajo Clonar() => (JornadaTrabajo)MemberwiseClone();
}
