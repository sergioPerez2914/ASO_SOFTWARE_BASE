using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Eventos de seguimiento de ejemplo mientras no existe base de datos. Las horas concuerdan con
/// los tiempos de carga de las remesas semilla de <see cref="MockRemesaDataSource"/>.
///
/// A futuro estos eventos no se capturan aquí: los publican los módulos de Flota (mantenimientos)
/// y Nómina (cambios de turno). Reemplazar por un repositorio real en la capa de infraestructura.
/// </summary>
public class MockEventoOperacionDataSource : IEventoOperacionDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<EventoOperacion> _eventos = new()
    {
        // --- Remesa 1: Borrador de hoy, carga 06:00–08:30 ---
        new()
        {
            Id = 1, RemesaId = 1, Tipo = TipoEventoOperacion.CambioTurno,
            FechaHora = Hoy.AddHours(6).AddMinutes(45),
            Descripcion = "Relevo en el frente del núcleo N-05: entra el tractorista José Graterol en lugar de Pedro Escalona."
        },
        new()
        {
            Id = 2, RemesaId = 1, Tipo = TipoEventoOperacion.Nota,
            FechaHora = Hoy.AddHours(7).AddMinutes(20), Autor = "Ana Torres",
            Descripcion = "Llovizna en el Tablón A; la cosechadora se detuvo unos 20 minutos."
        },

        // --- Remesa 2: Confirmada de ayer, carga 05:45–07:15 ---
        new()
        {
            Id = 3, RemesaId = 2, Tipo = TipoEventoOperacion.CambioTurno,
            FechaHora = Hoy.AddDays(-1).AddHours(6).AddMinutes(20),
            Descripcion = "Cambio de cuadrilla de corte del núcleo N-14."
        },
        new()
        {
            Id = 4, RemesaId = 2, Tipo = TipoEventoOperacion.Mantenimiento,
            FechaHora = Hoy.AddDays(-1).AddHours(6).AddMinutes(50),
            Descripcion = "Ajuste hidráulico de la alzadora en campo; 15 minutos de parada."
        },
        new()
        {
            Id = 5, RemesaId = 2, Tipo = TipoEventoOperacion.Nota,
            FechaHora = Hoy.AddDays(-1).AddHours(8).AddMinutes(40), Autor = "Yelitza Ramírez",
            Descripcion = "En ruta al CAM Las Majaguas; cola estimada en romana de 2 horas."
        },

        // --- Remesa 3: Recibida hace 2 días, carga 06:00–08:00, llegada 11:20 ---
        new()
        {
            Id = 6, RemesaId = 3, Tipo = TipoEventoOperacion.Mantenimiento,
            FechaHora = Hoy.AddDays(-2).AddHours(7).AddMinutes(10),
            Descripcion = "Cambio de correa de la picadora en la cosechadora; 25 minutos de parada."
        },
        new()
        {
            Id = 7, RemesaId = 3, Tipo = TipoEventoOperacion.Nota,
            FechaHora = Hoy.AddDays(-2).AddHours(10).AddMinutes(5), Autor = "Carlos Aguilar",
            Descripcion = "Espera en la Pre-Romana: tres gandolas adelante."
        },

        // --- Remesa 4: Anulada hace 3 días, carga 06:30–07:50 ---
        new()
        {
            Id = 8, RemesaId = 4, Tipo = TipoEventoOperacion.Mantenimiento,
            FechaHora = Hoy.AddDays(-3).AddHours(7).AddMinutes(55),
            Descripcion = "Falla de la bomba de combustible del camión 350 placa B56NP7Q; no puede salir de la finca."
        },
        new()
        {
            Id = 9, RemesaId = 4, Tipo = TipoEventoOperacion.Nota,
            FechaHora = Hoy.AddDays(-3).AddHours(8).AddMinutes(20), Autor = "Ana Torres",
            Descripcion = "La carga se trasladó a la unidad A12BC3D; se emitirá una nueva remesa."
        },
    };

    private int _siguienteId = 10;

    public IEnumerable<EventoOperacion> GetAll() => _eventos;

    public IEnumerable<EventoOperacion> GetByRemesa(int remesaId)
        => _eventos.Where(e => e.RemesaId == remesaId);

    public EventoOperacion? GetById(int id) => _eventos.FirstOrDefault(e => e.Id == id);

    public EventoOperacion Add(EventoOperacion item)
    {
        item.Id = _siguienteId++;
        _eventos.Add(item);
        return item;
    }

    public void Update(EventoOperacion item)
    {
        var indice = _eventos.FindIndex(e => e.Id == item.Id);
        if (indice >= 0)
            _eventos[indice] = item;
    }

    public void Delete(int id)
    {
        _eventos.RemoveAll(e => e.Id == id);
    }
}
