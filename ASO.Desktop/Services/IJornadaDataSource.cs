using System;
using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Jornadas de trabajo. Hoy la implementa un mock en memoria; mañana la implementará un
/// repositorio EF Core sin cambiar la UI ni el ViewModel.
/// </summary>
public interface IJornadaDataSource : ICrudDataSource<JornadaTrabajo, int>
{
    /// <summary>Jornadas cuya entrada cae dentro del rango: es el corte que usa la liquidación.</summary>
    IEnumerable<JornadaTrabajo> GetByPeriodo(DateTime desde, DateTime hasta);

    IEnumerable<JornadaTrabajo> GetByPersona(TipoPersonal tipo, int personaId);
}
