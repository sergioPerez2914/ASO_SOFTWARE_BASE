using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Flota de ejemplo mientras no existe base de datos.
///
/// Los activos 1–5 son las unidades de transporte que ya referencian las remesas semilla:
/// conservan los MISMOS Ids, placas y descripciones que tenía el catálogo de vehículos, porque
/// <see cref="VehiculoDataSourceAdapter"/> los proyecta hacia el combo de remesas.
///
/// Reemplazar por un repositorio real (EF Core / SQL Server) en la capa de infraestructura.
/// </summary>
public class MockActivoFlotaDataSource : IActivoFlotaDataSource
{
    private readonly List<ActivoFlota> _activos = new()
    {
        // --- Transporte (ids 1–5 = ids históricos de Vehiculo; textos literales) ---
        new()
        {
            Id = 1, Codigo = "CHU-01", Tipo = TipoActivo.Chuto, Marca = "Mack", Modelo = "Granite", Anio = 2014,
            Placa = "A12BC3D", Descripcion = "Chuto Mack + batea cañera", OdometroKm = 148_500m,
            Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 2, Codigo = "CHU-02", Tipo = TipoActivo.Chuto, Marca = "Iveco", Modelo = "Trakker", Anio = 2016,
            Placa = "A45DE6F", Descripcion = "Chuto Iveco + batea cañera", OdometroKm = 121_300m,
            Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 3, Codigo = "CAM-01", Tipo = TipoActivo.Camion, Marca = "Ford", Modelo = "Cargo 750", Anio = 2012,
            Placa = "A78GH9J", Descripcion = "Camión 750 con jaula", OdometroKm = 205_700m,
            Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 4, Codigo = "CHU-03", Tipo = TipoActivo.Chuto, Marca = "Ford", Modelo = "9000", Anio = 2010,
            Placa = "B21KL4M", Descripcion = "Chuto Ford + batea cañera", OdometroKm = 232_100m,
            Estado = EstadoActivo.Operativo
        },
        new()
        {
            // En taller: la remesa 4 se anuló por la falla de la bomba de combustible de esta unidad.
            Id = 5, Codigo = "CAM-02", Tipo = TipoActivo.Camion, Marca = "Ford", Modelo = "350", Anio = 2015,
            Placa = "B56NP7Q", Descripcion = "Camión 350 con jaula", OdometroKm = 176_400m,
            Estado = EstadoActivo.EnTaller
        },

        // --- Máquinas de campo ---
        new()
        {
            Id = 6, Codigo = "COS-01", Tipo = TipoActivo.Cosechadora, Marca = "Case IH", Modelo = "A8000", Anio = 2018,
            HorometroHoras = 6_240m, Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 7, Codigo = "COS-02", Tipo = TipoActivo.Cosechadora, Marca = "John Deere", Modelo = "CH570", Anio = 2021,
            HorometroHoras = 3_180m, Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 8, Codigo = "TRA-01", Tipo = TipoActivo.Tractor, Marca = "John Deere", Modelo = "6135J", Anio = 2019,
            HorometroHoras = 6_080m, Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 9, Codigo = "TRA-02", Tipo = TipoActivo.Tractor, Marca = "New Holland", Modelo = "TM7040", Anio = 2013,
            HorometroHoras = 4_510m, Estado = EstadoActivo.Operativo
        },
        new()
        {
            Id = 10, Codigo = "ALZ-01", Tipo = TipoActivo.Alzadora, Marca = "Cameco", Modelo = "SP1800", Anio = 2011,
            HorometroHoras = 7_420m, Estado = EstadoActivo.Operativo
        },
    };

    private int _siguienteId = 11;

    public IEnumerable<ActivoFlota> GetAll() => _activos;

    public ActivoFlota? GetById(int id) => _activos.FirstOrDefault(a => a.Id == id);

    public ActivoFlota Add(ActivoFlota item)
    {
        item.Id = _siguienteId++;
        _activos.Add(item);
        return item;
    }

    public void Update(ActivoFlota item)
    {
        var indice = _activos.FindIndex(a => a.Id == item.Id);
        if (indice >= 0)
            _activos[indice] = item;
    }

    public void Delete(int id)
    {
        _activos.RemoveAll(a => a.Id == id);
    }
}
