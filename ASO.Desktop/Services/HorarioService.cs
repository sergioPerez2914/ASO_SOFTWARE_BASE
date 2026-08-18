using System;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de la gestión de horarios. A diferencia de los documentos de movimiento, aquí no hay
/// máquina de estados: una jornada se abre y se cierra, y nada más. Lo que sí hay es una regla
/// dura de solo-inserción — no se edita ni se borra una asistencia ya registrada — porque de
/// estas horas sale un pago.
///
/// <see cref="HorasEnPeriodo"/> es la puerta por la que Liquidaciones consulta las horas: si
/// mañana cambia la forma de contarlas (descansos, horas extra, recargo nocturno), cambia aquí
/// y la nómina se entera sola.
/// </summary>
public sealed class HorarioService
{
    /// <summary>Tope de sensatez para una jornada; más que esto es un error de captura.</summary>
    private const int MaximoHorasJornada = 24;

    private readonly IJornadaDataSource _jornadas;

    public HorarioService(IJornadaDataSource jornadas) => _jornadas = jornadas;

    public bool PuedeRegistrarSalida(JornadaTrabajo jornada) => jornada.EstaAbierta;

    /// <summary>
    /// Abre una jornada. Una persona no puede tener dos jornadas abiertas a la vez: si aparece
    /// una, es que la anterior quedó sin cerrar y hay que resolver eso antes.
    /// </summary>
    public JornadaTrabajo Registrar(JornadaTrabajo jornada)
    {
        if (jornada.PersonaId == 0 || string.IsNullOrWhiteSpace(jornada.PersonaNombre))
            throw new InvalidOperationException("Seleccione la persona que inicia la jornada.");

        if (jornada.HoraEntrada == default)
            throw new InvalidOperationException("Indique la hora de entrada.");

        if (TieneJornadaAbierta(jornada.TipoPersonal, jornada.PersonaId, out var abierta))
            throw new InvalidOperationException(
                $"{jornada.PersonaNombre} tiene una jornada abierta desde el {abierta!.EntradaTexto}. " +
                "Ciérrela antes de registrar una nueva.");

        jornada.FechaRegistro = DateTime.Now;
        return _jornadas.Add(jornada);
    }

    /// <summary>Cierra la jornada con la hora de salida. Es la única modificación que admite.</summary>
    public JornadaTrabajo RegistrarSalida(JornadaTrabajo jornada, DateTime salida)
    {
        if (!PuedeRegistrarSalida(jornada))
            throw new InvalidOperationException("La jornada ya está cerrada.");

        if (salida <= jornada.HoraEntrada)
            throw new InvalidOperationException(
                $"La salida debe ser posterior a la entrada ({jornada.EntradaTexto}).");

        if ((salida - jornada.HoraEntrada).TotalHours > MaximoHorasJornada)
            throw new InvalidOperationException(
                $"La jornada superaría las {MaximoHorasJornada} horas. Verifique la hora de salida.");

        var copia = jornada.Clonar();
        copia.HoraSalida = salida;
        _jornadas.Update(copia);
        return copia;
    }

    /// <summary>
    /// Horas cerradas de una persona en el período. Las jornadas abiertas no cuentan: todavía
    /// no se sabe cuánto duraron.
    /// </summary>
    public decimal HorasEnPeriodo(TipoPersonal tipo, int personaId, DateTime desde, DateTime hasta) =>
        _jornadas.GetByPersona(tipo, personaId)
            .Where(j => !j.EstaAbierta
                        && j.HoraEntrada.Date >= desde.Date
                        && j.HoraEntrada.Date <= hasta.Date)
            .Sum(j => j.HorasTrabajadas ?? 0m);

    /// <summary>Horas cerradas de todo el centro en el período (indicador del módulo).</summary>
    public decimal HorasTotalesEnPeriodo(DateTime desde, DateTime hasta) =>
        _jornadas.GetByPeriodo(desde, hasta)
            .Where(j => !j.EstaAbierta)
            .Sum(j => j.HorasTrabajadas ?? 0m);

    public bool TieneJornadaAbierta(TipoPersonal tipo, int personaId, out JornadaTrabajo? abierta)
    {
        abierta = _jornadas.GetByPersona(tipo, personaId).FirstOrDefault(j => j.EstaAbierta);
        return abierta is not null;
    }
}
