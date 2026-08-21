namespace ASO.Desktop.Models;

/// <summary>
/// Nucleo: la empresa o centro donde esta instalado el sistema. Una instalacion atiende a un
/// solo nucleo, y dentro de el todas las referencias apuntan a ese mismo nucleo; lo que si es
/// de uno a muchos es nucleo -> <see cref="Finca"/>.
///
/// Es tambien el ambito de aislamiento: cada fila de las entidades operativas le pertenece y
/// nadie ve las de otro (ver <see cref="IDeOrganizacion"/>).
/// </summary>
public class Organizacion : IEntidad<int>
{
    public int Id { get; set; }

    /// <summary>Codigo corto de uso interno, para etiquetar la instalacion (p. ej. "MAJ").</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// C.O.D: el codigo con el que el CAM identifica al nucleo. Es la base del pago por corte,
    /// alza y empuje, y transporte, y es lo que los documentos estampan como texto. Va aparte
    /// de <see cref="Codigo"/> porque son dos identificadores distintos: uno es nuestro y el
    /// otro lo asigna el central.
    /// </summary>
    public string CodigoCam { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;

    public string Etiqueta => $"{Codigo} · {Nombre}";
    public string EstadoTexto => Activa ? "Activa" : "Inactiva";

    public Organizacion Clonar() => (Organizacion)MemberwiseClone();
}
