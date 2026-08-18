using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Tarifario de ejemplo mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
///
/// PROVISIONAL: los montos son ficticios. Pendiente del tarifario real del socio (lo que el
/// ingenio paga por tonelada y lo que el centro paga a cada núcleo).
/// </summary>
public class MockTarifaDataSource : ITarifaDataSource
{
    private static readonly DateTime InicioZafra = new(2025, 11, 1);

    private readonly List<Tarifa> _tarifas = new()
    {
        // --- Lo que se le cobra al ingenio ---
        new() { Id = 1, Concepto = "Corte de caña",        Servicio = ServicioZafra.Corte,      Ambito = AmbitoTarifa.Cobro,       Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 3.50m, VigenteDesde = InicioZafra },
        new() { Id = 2, Concepto = "Alza y empuje",        Servicio = ServicioZafra.AlzaEmpuje, Ambito = AmbitoTarifa.Cobro,       Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 1.20m, VigenteDesde = InicioZafra },
        new() { Id = 3, Concepto = "Transporte al central", Servicio = ServicioZafra.Transporte, Ambito = AmbitoTarifa.Cobro,      Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 2.80m, VigenteDesde = InicioZafra },

        // --- Lo que se le paga al núcleo o al trabajador ---
        new() { Id = 4, Concepto = "Corte de caña",        Servicio = ServicioZafra.Corte,      Ambito = AmbitoTarifa.PagoDestajo, Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 0.90m, VigenteDesde = InicioZafra },
        new() { Id = 5, Concepto = "Alza y empuje",        Servicio = ServicioZafra.AlzaEmpuje, Ambito = AmbitoTarifa.PagoDestajo, Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 0.35m, VigenteDesde = InicioZafra },
        new() { Id = 6, Concepto = "Transporte al central", Servicio = ServicioZafra.Transporte, Ambito = AmbitoTarifa.PagoDestajo, Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 0.75m, VigenteDesde = InicioZafra },
        new() { Id = 7, Concepto = "Jornada de taller",    Servicio = ServicioZafra.Otro,       Ambito = AmbitoTarifa.PagoDestajo, Unidad = UnidadTarifa.Hora,     MontoPorUnidad = 1.50m, VigenteDesde = InicioZafra, Notas = "Personal administrativo y de taller pagado por hora." },

        // Tarifa cerrada de la zafra anterior: sirve para ver el filtro "Vencida" y para
        // comprobar que un documento viejo conserva su monto aunque ya no rija.
        new() { Id = 8, Concepto = "Transporte al central", Servicio = ServicioZafra.Transporte, Ambito = AmbitoTarifa.Cobro,      Unidad = UnidadTarifa.Tonelada, MontoPorUnidad = 2.50m, VigenteDesde = new DateTime(2024, 11, 1), VigenteHasta = new DateTime(2025, 10, 31) },
    };

    private int _siguienteId = 9;

    public IEnumerable<Tarifa> GetAll() => _tarifas;

    public Tarifa? GetById(int id) => _tarifas.FirstOrDefault(t => t.Id == id);

    public IEnumerable<Tarifa> GetVigentes(DateTime fecha) =>
        _tarifas.Where(t => t.Activa && t.RigeEn(fecha));

    public Tarifa Add(Tarifa item)
    {
        item.Id = _siguienteId++;
        _tarifas.Add(item);
        return item;
    }

    public void Update(Tarifa item)
    {
        var indice = _tarifas.FindIndex(t => t.Id == item.Id);
        if (indice >= 0)
            _tarifas[indice] = item;
    }

    public void Delete(int id)
    {
        _tarifas.RemoveAll(t => t.Id == id);
    }
}
