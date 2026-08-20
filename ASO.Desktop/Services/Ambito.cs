using System.Windows.Input;

namespace ASO.Desktop.Services;

/// <summary>
/// Organizacion (nucleo) sobre la que trabaja la sesion actual.
///
/// Es estatico y global a proposito: <see cref="BD.AsoDbContext"/> se construye sin argumentos
/// en cada metodo de las fuentes Sql, asi que necesita una fuente ambiental de la que leer el
/// ambito. Lo fija <see cref="SesionActual.IniciarSesion"/> al autenticar y solo el rol
/// Desarrollador puede cambiarlo despues.
///
/// Fail-closed: si <see cref="OrganizacionId"/> es null no se ve NADA, en vez de verse todo.
/// Un ambito sin fijar es un error de programacion, no un permiso implicito.
/// </summary>
public static class Ambito
{
    /// <summary>Organizacion activa; null mientras no haya sesion iniciada.</summary>
    public static int? OrganizacionId { get; private set; }

    public static bool EstaFijado => OrganizacionId is not null;

    /// <summary>Se dispara al cambiar de organizacion, para que el shell se reconstruya.</summary>
    public static event EventHandler? Cambio;

    internal static void Fijar(int? organizacionId) => OrganizacionId = organizacionId;

    /// <summary>
    /// Cambia de nucleo sin cerrar sesion. Reservado al rol Desarrollador; quien llama
    /// debe haber comprobado el permiso <c>Organizaciones.Cambiar</c>.
    /// </summary>
    public static void Cambiar(int organizacionId)
    {
        if (OrganizacionId == organizacionId)
            return;

        OrganizacionId = organizacionId;

        // Los CanExecute que dependen del ambito no se refrescan solos: RequerySuggested
        // reacciona a la entrada del usuario, no a un cambio de estado como este.
        CommandManager.InvalidateRequerySuggested();
        Cambio?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Organizacion activa, o excepcion si no hay ninguna. Para escrituras.</summary>
    public static int Exigir() =>
        OrganizacionId ?? throw new InvalidOperationException(
            "No hay una organizacion activa en la sesion. Inicie sesion antes de operar sobre datos.");
}
