namespace ASO.Desktop.Models;

/// <summary>Función que cumple la persona dentro de la remesa.</summary>
public enum RolCampo
{
    Operador,
    Tractorista,
    Chofer,
    Remesero
}

/// <summary>
/// Persona que interviene en la remesa: operador de la máquina de corte, tractorista,
/// chofer de la unidad de transporte o remesero (autorizado por el núcleo para llenar el formato).
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class PersonalCampo : IEntidad<int>
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public RolCampo Rol { get; set; }

    /// <summary>C.O.D: núcleo al que pertenece, según el sistema del CAM.</summary>
    public string NucleoCodigo { get; set; } = string.Empty;
}
