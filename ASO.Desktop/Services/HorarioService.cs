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
///
/// Una jornada de campo se ficha además contra una remesa, y al abrirla y al cerrarla se publica
/// el movimiento en su línea de tiempo de Seguimiento — mismo criterio que
/// <see cref="MantenimientoService.Registrar"/>: lo que se registra en Nómina se ve en
/// Operaciones sin tocar ese módulo.
/// </summary>
public sealed class HorarioService
{
    /// <summary>Tope de sensatez para una jornada; más que esto es un error de captura.</summary>
    private const int MaximoHorasJornada = 24;

    private readonly IJornadaDataSource _jornadas;
    private readonly IEventoOperacionDataSource _eventos;
    private readonly IRemesaDataSource _remesas;

    public HorarioService(IJornadaDataSource jornadas,
                          IEventoOperacionDataSource eventos,
                          IRemesaDataSource remesas)
    {
        _jornadas = jornadas;
        _eventos = eventos;
        _remesas = remesas;
    }

    public bool PuedeRegistrarSalida(JornadaTrabajo jornada) => jornada.EstaAbierta;

    /// <summary>
    /// Abre una jornada. Una persona no puede tener dos jornadas abiertas a la vez: si aparece
    /// una, es que la anterior quedó sin cerrar y hay que resolver eso antes.
    ///
    /// El personal de campo ficha contra una remesa en curso; el administrativo, contra ninguna.
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

        ExigirFrenteValido(jornada);

        jornada.FechaRegistro = DateTime.Now;
        var guardada = _jornadas.Add(jornada);

        PublicarEnSeguimiento(guardada, guardada.HoraEntrada,
            $"Entra al frente {guardada.PersonaNombre}{Oficio(guardada)}, " +
            $"turno {guardada.TurnoTexto.ToLowerInvariant()}.");

        return guardada;
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

        // Sobre la copia ya cerrada: es la única que sabe cuántas horas fueron.
        // No se revalida el estado de la remesa: si se anuló mientras tanto, bloquear el cierre
        // dejaría la jornada abierta para siempre y falsearía las horas de la nómina.
        PublicarEnSeguimiento(copia, salida,
            $"Sale del frente {copia.PersonaNombre}{Oficio(copia)} tras {copia.HorasTexto} de jornada.");

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

    /// <summary>
    /// Comprueba el vínculo con la remesa. La obligatoriedad vive aquí y no solo en el editor:
    /// es lo que impide que una jornada de campo entre por otra vía sin frente.
    /// </summary>
    private void ExigirFrenteValido(JornadaTrabajo jornada)
    {
        if (jornada.TipoPersonal != TipoPersonal.Campo)
        {
            // Un administrativo no ficha contra un documento de campo, venga lo que venga.
            jornada.RemesaId = null;
            return;
        }

        if (jornada.RemesaId is not { } remesaId)
            throw new InvalidOperationException(
                "Seleccione la remesa en la que trabaja el personal de campo.");

        var remesa = _remesas.GetById(remesaId)
            ?? throw new InvalidOperationException("La remesa seleccionada no existe.");

        if (remesa.Estado is EstadoRemesa.Recibida or EstadoRemesa.Anulada)
            throw new InvalidOperationException(
                $"La remesa Nº {remesaId} está {remesa.EstadoTexto.ToLowerInvariant()}: " +
                "elija una remesa en curso.");
    }

    /// <summary>
    /// Refleja el movimiento de personal en la línea de tiempo de la remesa. Solo lo tienen las
    /// jornadas de campo: una jornada administrativa no pertenece a ningún frente.
    ///
    /// Lleva el Id de la jornada para que la ficha del evento pueda abrirla entera (turno, horas,
    /// observación) en vez de quedarse en la frase.
    /// </summary>
    private void PublicarEnSeguimiento(JornadaTrabajo jornada, DateTime fechaHora, string descripcion)
    {
        if (jornada.RemesaId is not { } remesaId)
            return;

        _eventos.Add(new EventoOperacion
        {
            RemesaId = remesaId,
            Tipo = TipoEventoOperacion.CambioTurno,
            FechaHora = fechaHora,
            Descripcion = descripcion,
            Autor = jornada.PersonaNombre,
            OrigenId = jornada.Id
        });
    }

    /// <summary>Sin cargo no se escribe un paréntesis vacío en la línea de tiempo.</summary>
    private static string Oficio(JornadaTrabajo jornada) =>
        string.IsNullOrWhiteSpace(jornada.CargoORol) ? string.Empty : $" ({jornada.CargoORol})";
}
