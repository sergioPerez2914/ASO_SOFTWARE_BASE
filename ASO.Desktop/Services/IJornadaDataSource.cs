using System;
using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Jornadas de trabajo. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IJornadaDataSource : ICrudDataSource<JornadaTrabajo, int>
{
    /// <summary>Jornadas cuya entrada cae dentro del rango: es el corte que usa la liquidación.</summary>
    IEnumerable<JornadaTrabajo> GetByPeriodo(DateTime desde, DateTime hasta);

    IEnumerable<JornadaTrabajo> GetByPersona(TipoPersonal tipo, int personaId);
}
