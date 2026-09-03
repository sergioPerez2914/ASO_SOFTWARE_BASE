using System.Collections.Generic;

namespace ASO.Desktop.Models;

/// <summary>
/// Finca del productor. El código debe corresponder con el aperturado en el CAM y el
/// nombre con el del título de propiedad (reglamento de remesas).
/// </summary>
public class Finca : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string CodigoCam { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public decimal Hectareas { get; set; }
    public List<Lote> Lotes { get; set; } = new();

    public string Etiqueta => $"{CodigoCam} · {Nombre}";

    public string HectareasTexto => $"{Hectareas:N2} ha";
}

/// <summary>Área de corte dentro de una parcela.</summary>
public class Lote
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<Tablon> Tablones { get; set; } = new();
}

/// <summary>Área de corte dentro de un lote.</summary>
public class Tablon
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
