using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Nucleo sobre el que trabaja la sesion actual: la <see cref="Organizacion"/> donde esta
/// instalado el sistema.
///
/// Es estatico y global a proposito: <see cref="BD.AsoDbContext"/> se construye sin argumentos
/// en cada metodo de las fuentes Sql, asi que necesita una fuente ambiental de la que leer el
/// ambito. Lo fija <see cref="SesionActual.IniciarSesion"/> al autenticar, a partir de la
/// pertenencia del usuario, y no cambia mientras dure la sesion: una instalacion atiende a un
/// solo nucleo.
///
/// Fail-closed: si no hay nucleo fijado no se ve NADA, en vez de verse todo. Un ambito sin
/// fijar es un error de programacion, no un permiso implicito.
/// </summary>
public static class Ambito
{
    /// <summary>Nucleo activo; null mientras no haya sesion iniciada.</summary>
    public static Organizacion? Actual { get; private set; }

    public static int? OrganizacionId => Actual?.Id;

    public static bool EstaFijado => Actual is not null;

    internal static void Fijar(Organizacion? organizacion) => Actual = organizacion;

    /// <summary>
    /// Refresca la copia cacheada despues de editar los datos del nucleo, para que los
    /// documentos que se emitan a continuacion estampen el C.O.D nuevo y no el de antes.
    /// </summary>
    internal static void Actualizar(Organizacion organizacion)
    {
        if (Actual is null || Actual.Id != organizacion.Id)
            throw new InvalidOperationException(
                "Solo se refrescan los datos del nucleo activo de la sesion.");

        Actual = organizacion;
    }

    /// <summary>Nucleo activo, o excepcion si no hay ninguno. Para escrituras.</summary>
    public static int Exigir() =>
        Actual?.Id ?? throw new InvalidOperationException(
            "No hay un nucleo activo en la sesion. Inicie sesion antes de operar sobre datos.");

    /// <summary>
    /// C.O.D del nucleo activo: el codigo con el que lo identifica el CAM. Es lo que los
    /// documentos estampan como texto (remesa, personal de campo, jornada, factura), de modo
    /// que un papel reimpreso conserve el codigo con el que se emitio aunque el nucleo se
    /// renombre despues.
    /// </summary>
    public static string ExigirCodigoCam()
    {
        if (Actual is not { } nucleo)
            throw new InvalidOperationException(
                "No hay un nucleo activo en la sesion. Inicie sesion antes de operar sobre datos.");

        if (string.IsNullOrWhiteSpace(nucleo.CodigoCam))
            throw new InvalidOperationException(
                $"El nucleo {nucleo.Nombre} no tiene C.O.D cargado. Registrelo antes de emitir documentos.");

        return nucleo.CodigoCam;
    }
}
