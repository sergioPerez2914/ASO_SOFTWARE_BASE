using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Liquidaciones de ejemplo mientras no existe base de datos. La cerrada no cita remesas reales
/// a propósito: así las remesas semilla siguen disponibles para probar la generación desde la
/// pantalla sin que aparezcan como ya liquidadas.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockLiquidacionDataSource : ILiquidacionDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<Liquidacion> _liquidaciones = new()
    {
        new()
        {
            Id = 1,
            SujetoTipo = SujetoLiquidacion.Nucleo,
            SujetoCodigo = "N-05", SujetoNombre = "Núcleo Las Majaguas",
            PeriodoDesde = Hoy.AddDays(-21), PeriodoHasta = Hoy.AddDays(-15),
            Estado = EstadoLiquidacion.Cerrada,
            FechaCierre = Hoy.AddDays(-14),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-14),
            Lineas =
            [
                new() { Concepto = "Corte de caña",        Origen = OrigenLinea.Destajo, Cantidad = 310.50m, UnidadTexto = "t", TarifaMonto = 0.90m, Monto = 279.45m },
                new() { Concepto = "Alza y empuje",        Origen = OrigenLinea.Destajo, Cantidad = 310.50m, UnidadTexto = "t", TarifaMonto = 0.35m, Monto = 108.68m },
                new() { Concepto = "Anticipo",             Origen = OrigenLinea.Concepto, UnidadTexto = "—", Monto = 100.00m, EsDeduccion = true },
            ]
        },
        new()
        {
            Id = 2,
            SujetoTipo = SujetoLiquidacion.Empleado,
            SujetoCodigo = "3", SujetoNombre = "Carlos Rodríguez",
            PeriodoDesde = Hoy.AddDays(-7), PeriodoHasta = Hoy,
            Estado = EstadoLiquidacion.Borrador,
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-1),
            Lineas =
            [
                new() { Concepto = "Jornada de taller", Origen = OrigenLinea.Horas, Cantidad = 19.00m, UnidadTexto = "h", TarifaMonto = 1.50m, Monto = 28.50m },
            ]
        },
    };

    private int _siguienteId = 3;

    public IEnumerable<Liquidacion> GetAll() => _liquidaciones;

    public Liquidacion? GetById(int id) => _liquidaciones.FirstOrDefault(l => l.Id == id);

    public Liquidacion Add(Liquidacion item)
    {
        item.Id = _siguienteId++;
        _liquidaciones.Add(item);
        return item;
    }

    public void Update(Liquidacion item)
    {
        var indice = _liquidaciones.FindIndex(l => l.Id == item.Id);
        if (indice >= 0)
            _liquidaciones[indice] = item;
    }

    public void Delete(int id) => _liquidaciones.RemoveAll(l => l.Id == id);
}
