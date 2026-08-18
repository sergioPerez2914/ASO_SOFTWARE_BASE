using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Cisternas de ejemplo mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockTanqueCombustibleDataSource : ITanqueCombustibleDataSource
{
    private readonly List<TanqueCombustible> _tanques = new()
    {
        new() { Id = 1, Nombre = "Cisterna principal", CapacidadL = 20_000m, ExistenciaL = 12_500m, Activo = true },
        new() { Id = 2, Nombre = "Tanque de taller",   CapacidadL = 2_000m,  ExistenciaL = 600m,    Activo = true },
    };

    private int _siguienteId = 3;

    public IEnumerable<TanqueCombustible> GetAll() => _tanques;

    public TanqueCombustible? GetById(int id) => _tanques.FirstOrDefault(t => t.Id == id);

    public TanqueCombustible Add(TanqueCombustible item)
    {
        item.Id = _siguienteId++;
        _tanques.Add(item);
        return item;
    }

    public void Update(TanqueCombustible item)
    {
        var indice = _tanques.FindIndex(t => t.Id == item.Id);
        if (indice >= 0)
            _tanques[indice] = item;
    }

    public void Delete(int id) => _tanques.RemoveAll(t => t.Id == id);
}

/// <summary>
/// Vales de ejemplo mientras no existe base de datos. Las lecturas son coherentes con los
/// horómetros y odómetros de <see cref="MockActivoFlotaDataSource"/> (van por detrás de la
/// lectura actual del activo, como corresponde a despachos ya pasados), y hay uno con consumo
/// disparado para ver la alerta y otro de cada estado para ver los filtros.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockValeCombustibleDataSource : IValeCombustibleDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<ValeCombustible> _vales = new()
    {
        // COS-01 (activo 6, horómetro actual 6.240 h): dos despachos con consumo normal.
        new()
        {
            Id = 1, Fecha = Hoy.AddDays(-12),
            TanqueId = 1, TanqueNombre = "Cisterna principal",
            ActivoId = 6, ActivoCodigo = "COS-01", ActivoEtiqueta = "COS-01 · Case IH A8000", EsTransporte = false,
            Litros = 420m, Lectura = 6_150m,
            ResponsableNombre = "Luis Bastidas",
            ConsumoPorUnidad = 21.00m, PromedioHistorico = null,
            Estado = EstadoVale.Confirmado, FechaConfirmacion = Hoy.AddDays(-12),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-12)
        },
        new()
        {
            Id = 2, Fecha = Hoy.AddDays(-5),
            TanqueId = 1, TanqueNombre = "Cisterna principal",
            ActivoId = 6, ActivoCodigo = "COS-01", ActivoEtiqueta = "COS-01 · Case IH A8000", EsTransporte = false,
            Litros = 400m, Lectura = 6_200m,
            ResponsableNombre = "Luis Bastidas",
            ConsumoPorUnidad = 8.00m, PromedioHistorico = 21.00m,
            Estado = EstadoVale.Confirmado, FechaConfirmacion = Hoy.AddDays(-5),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-5)
        },

        // CHU-01 (activo 1, odómetro actual 148.500 km): consumo por encima del promedio.
        new()
        {
            Id = 3, Fecha = Hoy.AddDays(-3),
            TanqueId = 1, TanqueNombre = "Cisterna principal",
            ActivoId = 1, ActivoCodigo = "CHU-01", ActivoEtiqueta = "CHU-01 · A12BC3D", EsTransporte = true,
            Litros = 260m, Lectura = 148_400m,
            ResponsableNombre = "Douglas Piña",
            ConsumoPorUnidad = 0.87m, PromedioHistorico = 0.52m, AlertaConsumo = true,
            Notas = "Se revisó el filtro de combustible tras el despacho.",
            Estado = EstadoVale.Confirmado, FechaConfirmacion = Hoy.AddDays(-3),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-3)
        },

        new()
        {
            Id = 4, Fecha = Hoy,
            TanqueId = 2, TanqueNombre = "Tanque de taller",
            ActivoId = 8, ActivoCodigo = "TRA-01", ActivoEtiqueta = "TRA-01 · John Deere 6135J", EsTransporte = false,
            Litros = 80m, Lectura = 6_090m,
            ResponsableNombre = "Ramón Piñero",
            Estado = EstadoVale.Borrador,
            CreadoPorId = 1, FechaCreacion = Hoy
        },

        new()
        {
            Id = 5, Fecha = Hoy.AddDays(-8),
            TanqueId = 1, TanqueNombre = "Cisterna principal",
            ActivoId = 10, ActivoCodigo = "ALZ-01", ActivoEtiqueta = "ALZ-01 · Cameco SP1800", EsTransporte = false,
            Litros = 150m, Lectura = 7_400m,
            ResponsableNombre = "Luis Bastidas",
            Estado = EstadoVale.Anulado,
            MotivoAnulacion = "Se despachó a la máquina equivocada; se rehízo el vale.",
            FechaAnulacion = Hoy.AddDays(-8),
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-8)
        },
    };

    private int _siguienteId = 6;

    public IEnumerable<ValeCombustible> GetAll() => _vales;

    public ValeCombustible? GetById(int id) => _vales.FirstOrDefault(v => v.Id == id);

    public IEnumerable<ValeCombustible> GetByActivo(int activoId) =>
        _vales.Where(v => v.ActivoId == activoId);

    public ValeCombustible Add(ValeCombustible item)
    {
        item.Id = _siguienteId++;
        _vales.Add(item);
        return item;
    }

    public void Update(ValeCombustible item)
    {
        var indice = _vales.FindIndex(v => v.Id == item.Id);
        if (indice >= 0)
            _vales[indice] = item;
    }

    public void Delete(int id) => _vales.RemoveAll(v => v.Id == id);
}

/// <summary>
/// Recargas de ejemplo mientras no existe base de datos.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockRecargaCombustibleDataSource : IRecargaCombustibleDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<RecargaCombustible> _recargas = new()
    {
        new()
        {
            Id = 1, Fecha = Hoy.AddDays(-15), TanqueId = 1, TanqueNombre = "Cisterna principal",
            Litros = 8_000m, CostoTotal = 4_800m, ProveedorNombre = "Estación de servicio Los Llanos",
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-15)
        },
        new()
        {
            Id = 2, Fecha = Hoy.AddDays(-4), TanqueId = 2, TanqueNombre = "Tanque de taller",
            Litros = 1_000m, CostoTotal = 620m, ProveedorNombre = "Estación de servicio Los Llanos",
            CreadoPorId = 1, FechaCreacion = Hoy.AddDays(-4)
        },
    };

    private int _siguienteId = 3;

    public IEnumerable<RecargaCombustible> GetAll() => _recargas;

    public RecargaCombustible? GetById(int id) => _recargas.FirstOrDefault(r => r.Id == id);

    public IEnumerable<RecargaCombustible> GetByTanque(int tanqueId) =>
        _recargas.Where(r => r.TanqueId == tanqueId);

    public RecargaCombustible Add(RecargaCombustible item)
    {
        item.Id = _siguienteId++;
        _recargas.Add(item);
        return item;
    }

    public void Update(RecargaCombustible item)
    {
        var indice = _recargas.FindIndex(r => r.Id == item.Id);
        if (indice >= 0)
            _recargas[indice] = item;
    }

    public void Delete(int id) => _recargas.RemoveAll(r => r.Id == id);
}
