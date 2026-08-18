using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Jornadas de ejemplo mientras no existe base de datos. Los nombres e Ids coinciden con
/// <see cref="MockEmpleadoDataSource"/> y <see cref="MockPersonalCampoDataSource"/>; hay dos
/// abiertas (turno en curso) para ver el cierre y suficientes cerradas para que la
/// liquidación por horas tenga material.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockJornadaDataSource : IJornadaDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<JornadaTrabajo> _jornadas = new()
    {
        // --- Taller y administración (padrón de empleados) ---
        new()
        {
            Id = 1, Fecha = Hoy.AddDays(-2), TipoPersonal = TipoPersonal.Administrativo,
            PersonaId = 3, PersonaNombre = "Carlos Rodríguez", CargoORol = "Mecánico de taller",
            Turno = TurnoJornada.Diurno,
            HoraEntrada = Hoy.AddDays(-2).AddHours(7), HoraSalida = Hoy.AddDays(-2).AddHours(16),
            CreadoPorId = 1, FechaRegistro = Hoy.AddDays(-2)
        },
        new()
        {
            Id = 2, Fecha = Hoy.AddDays(-1), TipoPersonal = TipoPersonal.Administrativo,
            PersonaId = 3, PersonaNombre = "Carlos Rodríguez", CargoORol = "Mecánico de taller",
            Turno = TurnoJornada.Diurno,
            HoraEntrada = Hoy.AddDays(-1).AddHours(7), HoraSalida = Hoy.AddDays(-1).AddHours(17),
            Observacion = "Reparación de la alzadora ALZ-01.",
            CreadoPorId = 1, FechaRegistro = Hoy.AddDays(-1)
        },
        new()
        {
            Id = 3, Fecha = Hoy.AddDays(-1), TipoPersonal = TipoPersonal.Administrativo,
            PersonaId = 7, PersonaNombre = "Ramón Piñero", CargoORol = "Mecánico de taller",
            Turno = TurnoJornada.Nocturno,
            HoraEntrada = Hoy.AddDays(-1).AddHours(19), HoraSalida = Hoy.AddHours(3),
            CreadoPorId = 1, FechaRegistro = Hoy.AddDays(-1)
        },
        new()
        {
            Id = 4, Fecha = Hoy, TipoPersonal = TipoPersonal.Administrativo,
            PersonaId = 5, PersonaNombre = "Luis Bastidas", CargoORol = "Almacenista",
            Turno = TurnoJornada.Diurno,
            HoraEntrada = Hoy.AddHours(7),
            CreadoPorId = 1, FechaRegistro = Hoy
        },

        // --- Personal de campo ---
        new()
        {
            Id = 5, Fecha = Hoy.AddDays(-2), TipoPersonal = TipoPersonal.Campo,
            PersonaId = 1, PersonaNombre = "Juan Pérez", CargoORol = "Operador", NucleoCodigo = "N-05",
            Turno = TurnoJornada.Diurno,
            HoraEntrada = Hoy.AddDays(-2).AddHours(6), HoraSalida = Hoy.AddDays(-2).AddHours(15),
            CreadoPorId = 1, FechaRegistro = Hoy.AddDays(-2)
        },
        new()
        {
            Id = 6, Fecha = Hoy.AddDays(-1), TipoPersonal = TipoPersonal.Campo,
            PersonaId = 1, PersonaNombre = "Juan Pérez", CargoORol = "Operador", NucleoCodigo = "N-05",
            Turno = TurnoJornada.Diurno,
            HoraEntrada = Hoy.AddDays(-1).AddHours(6), HoraSalida = Hoy.AddDays(-1).AddHours(14).AddMinutes(30),
            CreadoPorId = 1, FechaRegistro = Hoy.AddDays(-1)
        },
        new()
        {
            Id = 7, Fecha = Hoy.AddDays(-1), TipoPersonal = TipoPersonal.Campo,
            PersonaId = 4, PersonaNombre = "Pedro Escalona", CargoORol = "Tractorista", NucleoCodigo = "N-05",
            Turno = TurnoJornada.Nocturno,
            HoraEntrada = Hoy.AddDays(-1).AddHours(18), HoraSalida = Hoy.AddHours(2),
            CreadoPorId = 1, FechaRegistro = Hoy.AddDays(-1)
        },
        new()
        {
            Id = 8, Fecha = Hoy, TipoPersonal = TipoPersonal.Campo,
            PersonaId = 7, PersonaNombre = "María Gómez", CargoORol = "Chofer",
            Turno = TurnoJornada.Diurno,
            HoraEntrada = Hoy.AddHours(6),
            CreadoPorId = 1, FechaRegistro = Hoy
        },
    };

    private int _siguienteId = 9;

    public IEnumerable<JornadaTrabajo> GetAll() => _jornadas;

    public JornadaTrabajo? GetById(int id) => _jornadas.FirstOrDefault(j => j.Id == id);

    public IEnumerable<JornadaTrabajo> GetByPeriodo(DateTime desde, DateTime hasta) =>
        _jornadas.Where(j => j.HoraEntrada.Date >= desde.Date && j.HoraEntrada.Date <= hasta.Date);

    public IEnumerable<JornadaTrabajo> GetByPersona(TipoPersonal tipo, int personaId) =>
        _jornadas.Where(j => j.TipoPersonal == tipo && j.PersonaId == personaId);

    public JornadaTrabajo Add(JornadaTrabajo item)
    {
        item.Id = _siguienteId++;
        _jornadas.Add(item);
        return item;
    }

    public void Update(JornadaTrabajo item)
    {
        var indice = _jornadas.FindIndex(j => j.Id == item.Id);
        if (indice >= 0)
            _jornadas[indice] = item;
    }

    public void Delete(int id) => _jornadas.RemoveAll(j => j.Id == id);
}
