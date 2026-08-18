using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Mantenimientos de ejemplo mientras no existe base de datos. Las lecturas y fechas están
/// calibradas contra los intervalos de <see cref="MockReglaMantenimientoDataSource"/> para que
/// las recomendaciones muestren los tres estados: TRA-01 y CHU-01 vencidos, TRA-02 y CHU-03
/// próximos, el resto al día.
///
/// Los correctivos de ALZ-01 y CAM-02 corresponden a eventos ya sembrados en el seguimiento
/// (remesas 2 y 4 de <see cref="MockEventoOperacionDataSource"/>); aquí van SIN RemesaId para
/// no duplicar esos eventos al arrancar.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockMantenimientoRegistroDataSource : IMantenimientoRegistroDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<MantenimientoRegistro> _registros = new()
    {
        // --- COS-01 (Id 6, horómetro actual 6.240 h) ---
        new()
        {
            Id = 1, ActivoId = 6, ActivoCodigo = "COS-01", ActivoEtiqueta = "COS-01 · Case IH A8000",
            Fecha = Hoy.AddDays(-30).AddHours(16), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Revisión hidráulica y engrase general.", LecturaUso = 6_100m,
            RepuestosUsados = "Grasa multipropósito x 4 kg\nAceite hidráulico ISO 68 x 20 L",
            CostoRepuestos = 120m, CostoManoObra = 60m, RealizadoPor = "Taller propio — R. Mendoza",
            FechaRegistro = Hoy.AddDays(-30).AddHours(17)
        },
        new()
        {
            // Corresponde al evento "cambio de correa de la picadora" de la remesa 3 (ya sembrado en Seguimiento).
            Id = 2, ActivoId = 6, ActivoCodigo = "COS-01", ActivoEtiqueta = "COS-01 · Case IH A8000",
            Fecha = Hoy.AddDays(-2).AddHours(7).AddMinutes(10), Tipo = TipoMantenimiento.Correctivo,
            Descripcion = "Cambio de correa de la picadora en campo; 25 minutos de parada.", LecturaUso = 6_190m,
            RepuestosUsados = "Correa de picadora B-1450", CostoRepuestos = 95m, CostoManoObra = 30m,
            RealizadoPor = "Taller propio — R. Mendoza", FechaRegistro = Hoy.AddDays(-2).AddHours(9)
        },

        // --- COS-02 (Id 7, 3.180 h) ---
        new()
        {
            Id = 3, ActivoId = 7, ActivoCodigo = "COS-02", ActivoEtiqueta = "COS-02 · John Deere CH570",
            Fecha = Hoy.AddDays(-10).AddHours(15), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Revisión hidráulica y engrase general.", LecturaUso = 3_150m,
            RealizadoPor = "Servicio John Deere", FechaRegistro = Hoy.AddDays(-10).AddHours(16)
        },

        // --- TRA-01 (Id 8, 6.080 h): 330 h desde la lectura → VENCIDO (intervalo 300 h) ---
        new()
        {
            Id = 4, ActivoId = 8, ActivoCodigo = "TRA-01", ActivoEtiqueta = "TRA-01 · John Deere 6135J",
            Fecha = Hoy.AddDays(-100).AddHours(14), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Cambio de aceite y filtros de motor.", LecturaUso = 5_750m,
            RepuestosUsados = "Aceite 15W-40 x 12 L\nFiltro de aceite P550425\nFiltro de combustible",
            CostoRepuestos = 85m, CostoManoObra = 40m, RealizadoPor = "Taller propio — R. Mendoza",
            FechaRegistro = Hoy.AddDays(-100).AddHours(15)
        },

        // --- TRA-02 (Id 9, 4.510 h): 280 h desde la lectura → PRÓXIMO (93 % de 300 h) ---
        new()
        {
            Id = 5, ActivoId = 9, ActivoCodigo = "TRA-02", ActivoEtiqueta = "TRA-02 · New Holland TM7040",
            Fecha = Hoy.AddDays(-15).AddHours(10), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Cambio de aceite y filtros de motor.", LecturaUso = 4_230m,
            RealizadoPor = "Taller propio — J. Castillo", FechaRegistro = Hoy.AddDays(-15).AddHours(11)
        },

        // --- ALZ-01 (Id 10, 7.420 h) — corresponde al evento "ajuste hidráulico" de la remesa 2 ---
        new()
        {
            Id = 6, ActivoId = 10, ActivoCodigo = "ALZ-01", ActivoEtiqueta = "ALZ-01 · Cameco SP1800",
            Fecha = Hoy.AddDays(-1).AddHours(6).AddMinutes(50), Tipo = TipoMantenimiento.Correctivo,
            Descripcion = "Ajuste hidráulico de la alzadora en campo; 15 minutos de parada.", LecturaUso = 7_410m,
            RealizadoPor = "Taller propio — J. Castillo", FechaRegistro = Hoy.AddDays(-1).AddHours(9)
        },

        // --- CAM-02 (Id 5) — corresponde a la falla que anuló la remesa 4; unidad aún en taller ---
        new()
        {
            Id = 7, ActivoId = 5, ActivoCodigo = "CAM-02", ActivoEtiqueta = "CAM-02 · B56NP7Q",
            Fecha = Hoy.AddDays(-3).AddHours(15), Tipo = TipoMantenimiento.Correctivo,
            Descripcion = "Cambio de la bomba de combustible.", LecturaUso = 176_400m,
            RepuestosUsados = "Bomba de combustible Ford 350", CostoRepuestos = 260m, CostoManoObra = 90m,
            RealizadoPor = "Taller propio — R. Mendoza", FechaRegistro = Hoy.AddDays(-3).AddHours(16)
        },

        // --- CHU-01 (Id 1): hace 95 días → VENCIDO (intervalo 90 días) ---
        new()
        {
            Id = 8, ActivoId = 1, ActivoCodigo = "CHU-01", ActivoEtiqueta = "CHU-01 · A12BC3D",
            Fecha = Hoy.AddDays(-95).AddHours(9), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Cambio de aceite y filtros.", LecturaUso = 141_200m,
            RealizadoPor = "Taller propio — J. Castillo", FechaRegistro = Hoy.AddDays(-95).AddHours(10)
        },

        // --- CHU-02 (Id 2): hace 20 días → al día ---
        new()
        {
            Id = 9, ActivoId = 2, ActivoCodigo = "CHU-02", ActivoEtiqueta = "CHU-02 · A45DE6F",
            Fecha = Hoy.AddDays(-20).AddHours(8), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Cambio de aceite y filtros.", LecturaUso = 119_800m,
            RealizadoPor = "Taller propio — J. Castillo", FechaRegistro = Hoy.AddDays(-20).AddHours(9)
        },

        // --- CHU-03 (Id 4): hace 85 días → PRÓXIMO (94 % de 90 días) ---
        new()
        {
            Id = 10, ActivoId = 4, ActivoCodigo = "CHU-03", ActivoEtiqueta = "CHU-03 · B21KL4M",
            Fecha = Hoy.AddDays(-85).AddHours(11), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Cambio de aceite y filtros.", LecturaUso = 226_900m,
            RealizadoPor = "Taller propio — R. Mendoza", FechaRegistro = Hoy.AddDays(-85).AddHours(12)
        },

        // --- CAM-01 (Id 3): hace 40 días → al día ---
        new()
        {
            Id = 11, ActivoId = 3, ActivoCodigo = "CAM-01", ActivoEtiqueta = "CAM-01 · A78GH9J",
            Fecha = Hoy.AddDays(-40).AddHours(14), Tipo = TipoMantenimiento.Preventivo,
            Descripcion = "Cambio de aceite y filtros.", LecturaUso = 199_500m,
            RealizadoPor = "Taller propio — J. Castillo", FechaRegistro = Hoy.AddDays(-40).AddHours(15)
        },
    };

    private int _siguienteId = 12;

    public IEnumerable<MantenimientoRegistro> GetAll() => _registros;

    public IEnumerable<MantenimientoRegistro> GetByActivo(int activoId)
        => _registros.Where(r => r.ActivoId == activoId);

    public MantenimientoRegistro? GetById(int id) => _registros.FirstOrDefault(r => r.Id == id);

    public MantenimientoRegistro Add(MantenimientoRegistro item)
    {
        item.Id = _siguienteId++;
        _registros.Add(item);
        return item;
    }

    public void Update(MantenimientoRegistro item)
    {
        var indice = _registros.FindIndex(r => r.Id == item.Id);
        if (indice >= 0)
            _registros[indice] = item;
    }

    public void Delete(int id)
    {
        _registros.RemoveAll(r => r.Id == id);
    }
}
