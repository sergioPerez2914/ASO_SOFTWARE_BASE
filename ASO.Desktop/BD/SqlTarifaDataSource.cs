using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlTarifaDataSource : SqlCrudDataSource<Tarifa, int>, ITarifaDataSource
{
    /// <summary>
    /// RigeEn() es un metodo C# que EF no sabe traducir a SQL, asi que el filtro de vigencia se
    /// resuelve en memoria: traer la tabla entera antes de filtrar es deliberado, no un descuido.
    /// </summary>
    public IEnumerable<Tarifa> GetVigentes(DateTime fecha)
        => GetAll().Where(t => t.Activa && t.RigeEn(fecha)).ToList();
}
