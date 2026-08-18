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
/// Padrón separado de <see cref="Empleado"/>: el personal de campo pertenece a un núcleo
/// (C.O.D) y determina el pago por destajo, mientras que el empleado es nómina del centro.
/// No se unifican hasta que el socio defina si son la misma persona en dos roles.
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

    public bool Activo { get; set; } = true;

    public string RolTexto => Rol switch
    {
        RolCampo.Operador => "Operador",
        RolCampo.Tractorista => "Tractorista",
        RolCampo.Chofer => "Chofer",
        _ => "Remesero"
    };

    public string EstadoTexto => Activo ? "Activo" : "Inactivo";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public PersonalCampo Clonar() => (PersonalCampo)MemberwiseClone();
}
