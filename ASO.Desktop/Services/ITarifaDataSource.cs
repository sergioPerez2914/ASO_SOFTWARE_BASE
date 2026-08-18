using System;
using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos del tarifario. Hoy la implementa un mock en memoria; mañana la
/// implementará un repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface ITarifaDataSource : ICrudDataSource<Tarifa, int>
{
    /// <summary>Tarifas activas que rigen en la fecha indicada.</summary>
    IEnumerable<Tarifa> GetVigentes(DateTime fecha);
}
