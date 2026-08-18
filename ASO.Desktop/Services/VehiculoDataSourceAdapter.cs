using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Compatibilidad con las remesas: proyecta los activos de transporte del catálogo único de
/// flota como <see cref="Vehiculo"/>, con los MISMOS Ids. Así el editor de remesas sigue
/// funcionando sin cambios y hay una sola verdad sobre las unidades.
///
/// El alta y edición de unidades vive en Flota · Gestión de Flota, por eso las operaciones de
/// escritura no están soportadas aquí.
/// </summary>
public sealed class VehiculoDataSourceAdapter : IVehiculoDataSource
{
    private const string MensajeSoloLectura =
        "Las unidades de transporte se administran desde Flota · Gestión de Flota.";

    private readonly IActivoFlotaDataSource _activos;

    public VehiculoDataSourceAdapter(IActivoFlotaDataSource activos) => _activos = activos;

    public IEnumerable<Vehiculo> GetAll()
        => _activos.GetAll()
                   .Where(a => a.EsTransporte)
                   .Select(a => new Vehiculo { Id = a.Id, Placa = a.Placa, Descripcion = a.Descripcion });

    public Vehiculo? GetById(int id) => GetAll().FirstOrDefault(v => v.Id == id);

    public Vehiculo Add(Vehiculo item) => throw new NotSupportedException(MensajeSoloLectura);

    public void Update(Vehiculo item) => throw new NotSupportedException(MensajeSoloLectura);

    public void Delete(int id) => throw new NotSupportedException(MensajeSoloLectura);
}
