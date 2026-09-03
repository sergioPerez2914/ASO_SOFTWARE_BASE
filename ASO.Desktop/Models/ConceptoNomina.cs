namespace ASO.Desktop.Models;

/// <summary>
/// Naturaleza de un concepto de nómina: suma (devengo) o resta (deducción) al neto a pagar.
/// </summary>
public enum TipoConcepto
{
    Devengo,
    Deduccion
}

/// <summary>
/// Concepto fijo de nómina que se suma o resta en la liquidación
/// (p. ej. "Bono de asistencia" = devengo, "Anticipo" = deducción). Dato maestro.
/// </summary>
public class ConceptoNomina : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoConcepto Tipo { get; set; } = TipoConcepto.Devengo;
    public bool Activo { get; set; } = true;

    public string TipoTexto => Tipo == TipoConcepto.Devengo ? "Devengo" : "Deducción";
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
}
