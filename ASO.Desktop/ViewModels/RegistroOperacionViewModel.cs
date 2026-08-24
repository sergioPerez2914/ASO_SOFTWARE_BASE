using System;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Operaciones · Registro de Operación: listado de Remesas de caña con su alta/edición y las
/// transiciones de estado (confirmar, anular, registrar recepción).
///
/// Las reglas viven en <see cref="RemesaService"/>; aquí solo se pide la acción y se refleja
/// el resultado. Si el servicio rechaza una transición, se informa al usuario en vez de tragarse
/// el error: el botón deshabilitado es cortesía, la regla la impone el servicio.
/// </summary>
public sealed class RegistroOperacionViewModel : PantallaCrudViewModel<Remesa, int>
{
    private const string FiltroTodas = "Todas";

    private readonly RemesaService _servicio;
    private readonly IServicioDialogo _dialogos;
    private readonly SolicitudesDeCambio _solicitudes = new();
    private readonly ISesionActual _sesionActual;
    private readonly IFincaDataSource _fincas;
    private readonly IPersonalCampoDataSource _personal;
    private readonly IActivoFlotaDataSource _vehiculos;

    /// <summary>Se dispara al pedir volver al dashboard del módulo; la ventana principal navega.</summary>

    public RegistroOperacionViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearRemesas(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private RegistroOperacionViewModel(Modulo modulo,
                                       Submodulo submodulo,
                                       IRemesaDataSource remesas,
                                       IServicioDialogo dialogos,
                                       ISesionActual sesion)
        : base(modulo, submodulo, remesas, dialogos, sesion)
    {
        _servicio = new RemesaService(remesas);
        _dialogos = dialogos;
        _sesionActual = sesion;
        _fincas = DataSourceFactory.CrearFincas();
        _personal = DataSourceFactory.CrearPersonalCampo();
        _vehiculos = DataSourceFactory.CrearActivosFlota();

        ConfirmarCommand = new RelayCommand(Confirmar,
            () => SelectedItem is { } r && _servicio.PuedeConfirmar(r) && _sesionActual.Puede("Remesas.Confirmar"));

        // Se habilitan tambien para quien solo puede SOLICITARLAS: el boton no queda gris sin
        // explicacion, sino que abre una peticion al administrador (ver SolicitudesDeCambio).
        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } r && _servicio.PuedeAnular(r) && _solicitudes.PuedeIntentar(Permisos.Remesas.Anular));

        RegistrarRecepcionCommand = new RelayCommand(RegistrarRecepcion,
            () => SelectedItem is { } r && _servicio.PuedeRegistrarRecepcion(r) && _solicitudes.PuedeIntentar(Permisos.Remesas.Recepcion));

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });
    }

    // --- Encabezado de la pantalla (mismo patrón que SubmoduloViewModel) ---

    public ICommand ConfirmarCommand { get; }
    public ICommand AnularCommand { get; }
    public ICommand RegistrarRecepcionCommand { get; }
    public ICommand CambiarFiltroEstadoCommand { get; }

    protected override string ModuloPermiso => "Remesas";

    private string _filtroEstado = FiltroTodas;

    protected override bool PasaFiltroExtra(Remesa item)
        => _filtroEstado == FiltroTodas || item.EstadoTexto == _filtroEstado;

    protected override bool CoincideBusqueda(Remesa item, string texto)
        => Contiene(item.Id.ToString(), texto)
           || Contiene(item.FincaCodigoCam, texto)
           || Contiene(item.FincaNombre, texto)
           || Contiene(item.VehiculoPlaca, texto)
           || Contiene(item.OperadorNombre, texto)
           || Contiene(item.TractoristaNombre, texto)
           || Contiene(item.ChoferNombre, texto)
           || Contiene(item.RemeseroNombre, texto);

    private static bool Contiene(string valor, string texto)
        => valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Remesa CrearNuevo() => new()
    {
        Estado = EstadoRemesa.Borrador,
        FechaCreacion = DateTime.Now,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0
    };

    protected override CrudEditorViewModelBase<Remesa> CrearEditor(Remesa item)
        => new RemesaEditorViewModel(item, _fincas, _personal, _vehiculos);

    // Una remesa deja de ser editable en cuanto se confirma.
    protected override bool PuedeEditar(Remesa item) => _servicio.PuedeEditar(item);

    protected override bool PuedeEliminar(Remesa item) => _servicio.PuedeEliminar(item);

    private void Confirmar()
    {
        if (SelectedItem is not { } remesa)
            return;

        if (!_dialogos.Confirmar("Confirmar remesa",
                $"¿Confirmar la remesa Nº {remesa.Id}? Después de confirmarla no se podrá editar."))
            return;

        Aplicar(() => _servicio.Confirmar(remesa));
    }

    /// <summary>Como se ve la remesa en la bandeja del administrador, congelado al solicitar.</summary>
    private static string Describir(Remesa remesa) =>
        string.IsNullOrWhiteSpace(remesa.FincaNombre)
            ? $"Remesa Nº {remesa.Id}"
            : $"Remesa Nº {remesa.Id} · {remesa.FincaNombre}";

    private void Anular()
    {
        if (SelectedItem is not { } remesa)
            return;

        if (_solicitudes.RequierePeticion(Permisos.Remesas.Anular))
        {
            _solicitudes.Solicitar(Permisos.Remesas.Anular, "Anular remesa",
                nameof(Remesa), remesa.Id.ToString(), Describir(remesa));
            return;
        }

        var editor = new AnularRemesaEditorViewModel(remesa);
        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Anular(remesa, editor.Motivo));
    }

    private void RegistrarRecepcion()
    {
        if (SelectedItem is not { } remesa)
            return;

        if (_solicitudes.RequierePeticion(Permisos.Remesas.Recepcion))
        {
            _solicitudes.Solicitar(Permisos.Remesas.Recepcion, "Registrar recepción de remesa",
                nameof(Remesa), remesa.Id.ToString(), Describir(remesa));
            return;
        }

        var editor = new RecepcionRemesaEditorViewModel(remesa);
        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.RegistrarRecepcion(remesa, editor.Llegada, editor.PesoBrutoT, editor.TaraT));
    }

    /// <summary>
    /// Ejecuta una transición. La lista NO se toca aquí: el servicio guarda, y esa escritura
    /// dispara la recarga de la pantalla (ver <c>Services/CambiosDeDatos</c>). Lo único que se
    /// conserva es qué fila dejar seleccionada, porque la recarga trae objetos nuevos y la
    /// referencia anterior ya no está en la lista.
    ///
    /// Si el servicio rechaza la transición se informa y no se guarda nada, así que tampoco hay
    /// recarga: la pantalla se queda como estaba, que es lo correcto.
    /// </summary>
    private void Aplicar(Func<Remesa> transicion)
    {
        try
        {
            var actualizada = transicion();
            SeleccionarTrasRecargar(actualizada.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Remesas", ex.Message);
        }
    }
}
