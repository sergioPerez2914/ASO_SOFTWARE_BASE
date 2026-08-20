using ASO.Desktop.Configuration;
using ASO.Desktop.ViewModels;

namespace ASO.Desktop.Services;

/// <summary>
/// Puente entre un comando bloqueado y la bandeja del administrador.
///
/// Sin esto, a un remesero el boton "Anular" le queda gris y sin explicacion. Con esto el
/// boton sigue vivo: al pulsarlo pide el motivo y deja una peticion. La regla de que accion
/// admite peticion vive en <see cref="MatrizPermisos.Solicitables"/>, no aqui.
/// </summary>
public class SolicitudesDeCambio
{
    private readonly ISesionActual _sesion;
    private readonly IServicioDialogo _dialogos;
    private readonly PeticionService _servicio;

    public SolicitudesDeCambio(ISesionActual? sesion = null,
                               IServicioDialogo? dialogos = null,
                               PeticionService? servicio = null)
    {
        _sesion = sesion ?? SesionActual.Instancia;
        _dialogos = dialogos ?? new ServicioDialogo();
        _servicio = servicio ?? new PeticionService(DataSourceFactory.CrearPeticiones());
    }

    /// <summary>
    /// Para el <c>CanExecute</c>: el comando se habilita tanto si puede hacerlo como si puede
    /// pedirlo. Lo que cambia es lo que ocurre al pulsarlo.
    /// </summary>
    public bool PuedeIntentar(string permiso) =>
        _sesion.Puede(permiso) || _sesion.PuedeSolicitar(permiso);

    /// <summary>Para el <c>Execute</c>: no puede hacerlo, pero si pedirlo.</summary>
    public bool RequierePeticion(string permiso) =>
        !_sesion.Puede(permiso) && _sesion.PuedeSolicitar(permiso);

    /// <summary>
    /// Pide el motivo y registra la peticion.
    /// </summary>
    /// <returns><c>true</c> si quedo registrada; <c>false</c> si el usuario cancelo.</returns>
    public bool Solicitar(string permiso,
                          string accion,
                          string tipoEntidad,
                          string entidadId,
                          string entidadDescripcion)
    {
        if (_sesion.UsuarioActual is not { } solicitante)
            return false;

        var editor = new MotivoEditorViewModel(
            $"Solicitar: {accion}",
            entidadDescripcion,
            "Motivo de la solicitud",
            "Indique por qué necesita este cambio.");

        if (!_dialogos.MostrarEditor(editor))
            return false;

        try
        {
            _servicio.Crear(permiso, accion, tipoEntidad, entidadId,
                            entidadDescripcion, editor.Motivo, solicitante);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo registrar la solicitud", ex.Message);
            return false;
        }

        _dialogos.Informar(
            "Solicitud enviada",
            $"Queda registrada para el administrador de tu núcleo:\n\n{accion} · {entidadDescripcion}");

        return true;
    }
}
