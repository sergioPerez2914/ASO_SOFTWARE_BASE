using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Remesas de ejemplo mientras no existe base de datos: una por estado, para poder
/// probar los filtros y las transiciones.
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockRemesaDataSource : IRemesaDataSource
{
    private static readonly DateTime Hoy = DateTime.Today;

    private readonly List<Remesa> _remesas = new()
    {
        new()
        {
            Id = 1,
            FincaId = 1, FincaCodigoCam = "F-0112", FincaNombre = "La Esperanza",
            LoteNombre = "Lote 1", TablonNombre = "Tablón A", TipoCosecha = TipoCosecha.Mecanizada,
            OperadorId = 1, OperadorNombre = "Juan Pérez", OperadorNucleoCodigo = "N-05",
            TractoristaId = 4, TractoristaNombre = "Pedro Escalona", TractoristaNucleoCodigo = "N-05",
            ChoferId = 8, ChoferNombre = "Douglas Piña",
            VehiculoId = 1, VehiculoPlaca = "A12BC3D",
            RemeseroId = 10, RemeseroNombre = "Ana Torres",
            NucleoCorteCodigo = "N-05", NucleoAlzaEmpujeCodigo = "N-05", NucleoTransporteCodigo = "N-21",
            InicioCarga = Hoy.AddHours(6), FinCarga = Hoy.AddHours(8).AddMinutes(30),
            Estado = EstadoRemesa.Borrador, CreadoPorId = 2, FechaCreacion = Hoy.AddHours(9)
        },
        new()
        {
            Id = 2,
            FincaId = 2, FincaCodigoCam = "F-0245", FincaNombre = "Santa Rita",
            LoteNombre = "Lote Norte", TablonNombre = "Tablón 2", TipoCosecha = TipoCosecha.Manual,
            OperadorId = 2, OperadorNombre = "Rafael Colmenares", OperadorNucleoCodigo = "N-14",
            TractoristaId = 5, TractoristaNombre = "José Graterol", TractoristaNucleoCodigo = "N-14",
            ChoferId = 9, ChoferNombre = "Alexis Camacho",
            VehiculoId = 2, VehiculoPlaca = "A45DE6F",
            RemeseroId = 11, RemeseroNombre = "Yelitza Ramírez",
            NucleoCorteCodigo = "N-14", NucleoAlzaEmpujeCodigo = "N-14", NucleoTransporteCodigo = "N-14",
            InicioCarga = Hoy.AddDays(-1).AddHours(5).AddMinutes(45),
            FinCarga = Hoy.AddDays(-1).AddHours(7).AddMinutes(15),
            Estado = EstadoRemesa.Confirmada, CreadoPorId = 2, FechaCreacion = Hoy.AddDays(-1).AddHours(8),
            FechaConfirmacion = Hoy.AddDays(-1).AddHours(8).AddMinutes(15)
        },
        new()
        {
            Id = 3,
            FincaId = 3, FincaCodigoCam = "F-0301", FincaNombre = "Agropecuaria Turén",
            LoteNombre = "Lote El Samán", TablonNombre = "Tablón B", TipoCosecha = TipoCosecha.Mecanizada,
            OperadorId = 3, OperadorNombre = "Luis Mendoza", OperadorNucleoCodigo = "N-21",
            TractoristaId = 6, TractoristaNombre = "Wilmer Rodríguez", TractoristaNucleoCodigo = "N-33",
            ChoferId = 7, ChoferNombre = "María Gómez",
            VehiculoId = 3, VehiculoPlaca = "A78GH9J",
            RemeseroId = 12, RemeseroNombre = "Carlos Aguilar",
            NucleoCorteCodigo = "N-21", NucleoAlzaEmpujeCodigo = "N-33", NucleoTransporteCodigo = "N-21",
            InicioCarga = Hoy.AddDays(-2).AddHours(6),
            FinCarga = Hoy.AddDays(-2).AddHours(8),
            LlegadaCentral = Hoy.AddDays(-2).AddHours(11).AddMinutes(20),
            PesoBrutoT = 32.40m, TaraT = 14.10m,
            Estado = EstadoRemesa.Recibida, CreadoPorId = 2, FechaCreacion = Hoy.AddDays(-2).AddHours(9),
            FechaConfirmacion = Hoy.AddDays(-2).AddHours(9).AddMinutes(10)
        },
        new()
        {
            Id = 4,
            FincaId = 1, FincaCodigoCam = "F-0112", FincaNombre = "La Esperanza",
            LoteNombre = "Lote 2", TablonNombre = "Tablón C", TipoCosecha = TipoCosecha.Manual,
            OperadorId = 1, OperadorNombre = "Juan Pérez", OperadorNucleoCodigo = "N-05",
            TractoristaId = 4, TractoristaNombre = "Pedro Escalona", TractoristaNucleoCodigo = "N-05",
            ChoferId = 8, ChoferNombre = "Douglas Piña",
            VehiculoId = 5, VehiculoPlaca = "B56NP7Q",
            RemeseroId = 10, RemeseroNombre = "Ana Torres",
            NucleoCorteCodigo = "N-05", NucleoAlzaEmpujeCodigo = "N-05", NucleoTransporteCodigo = "N-05",
            InicioCarga = Hoy.AddDays(-3).AddHours(6).AddMinutes(30),
            FinCarga = Hoy.AddDays(-3).AddHours(7).AddMinutes(50),
            Estado = EstadoRemesa.Anulada, MotivoAnulacion = "Carga trasladada a otra unidad por falla mecánica.",
            CreadoPorId = 2, FechaCreacion = Hoy.AddDays(-3).AddHours(8),
            FechaConfirmacion = Hoy.AddDays(-3).AddHours(8).AddMinutes(5),
            FechaAnulacion = Hoy.AddDays(-3).AddHours(8).AddMinutes(30)
        },
    };

    private int _siguienteId = 5;

    public IEnumerable<Remesa> GetAll() => _remesas;

    public Remesa? GetById(int id) => _remesas.FirstOrDefault(r => r.Id == id);

    public Remesa Add(Remesa item)
    {
        item.Id = _siguienteId++;
        _remesas.Add(item);
        return item;
    }

    public void Update(Remesa item)
    {
        var indice = _remesas.FindIndex(r => r.Id == item.Id);
        if (indice >= 0)
            _remesas[indice] = item;
    }

    public void Delete(int id)
    {
        _remesas.RemoveAll(r => r.Id == id);
    }
}
