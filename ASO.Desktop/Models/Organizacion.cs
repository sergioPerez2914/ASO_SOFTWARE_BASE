namespace ASO.Desktop.Models;

/// <summary>
/// Nucleo que usa ASO: la empresa o centro donde esta instalado el sistema.
/// Es el ambito de aislamiento: cada fila de las entidades operativas pertenece a una
/// organizacion y nadie ve las de otra (ver <see cref="IDeOrganizacion"/>).
///
/// NO confundir con <see cref="Nucleo"/>, que es el catalogo de nucleos de PRODUCTORES
/// (codigo C.O.D del CAM) y sirve para decidir a quien se le paga el corte, el alza y el
/// transporte. Una remesa cita tres de esos a la vez; de una organizacion solo tiene una.
/// </summary>
public class Organizacion : IEntidad<int>
{
    public int Id { get; set; }

    /// <summary>Codigo corto para el selector del desarrollador (p. ej. "MAJ").</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;

    public string Etiqueta => $"{Codigo} · {Nombre}";
    public string EstadoTexto => Activa ? "Activa" : "Inactiva";

    public Organizacion Clonar() => (Organizacion)MemberwiseClone();
}
