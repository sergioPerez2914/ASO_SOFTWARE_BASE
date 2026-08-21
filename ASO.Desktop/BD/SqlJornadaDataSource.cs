using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlJornadaDataSource : SqlCrudDataSource<JornadaTrabajo, int>, IJornadaDataSource
{
    public IEnumerable<JornadaTrabajo> GetByPeriodo(DateTime desde, DateTime hasta)
        => Consultar(q => q.Where(j => j.HoraEntrada >= desde && j.HoraEntrada <= hasta));

    public IEnumerable<JornadaTrabajo> GetByPersona(TipoPersonal tipo, int personaId)
        => Consultar(q => q.Where(j => j.TipoPersonal == tipo && j.PersonaId == personaId));
}
