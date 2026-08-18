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
public sealed class RegistroOperacionViewModel : CrudViewModelBase<Remesa, int>
{
    private const string FiltroTodas = "Todas";

    private readonly RemesaService _servicio;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly IFincaDataSource _fincas;
    private readonly INucleoDataSource _nucleos;
    private readonly IPersonalCampoDataSource _personal;
    private readonly IVehiculoDataSource _vehiculos;

    /// <summary>Se dispara al pedir volver al dashboard del módulo; la ventana principal navega.</summary>
    public event EventHandler? VolverSolicitado;

    public RegistroOperacionViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearRemesas(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private RegistroOperacionViewModel(Modulo modulo,
                                       Submodulo submodulo,
                                       IRemesaDataSource remesas,
                                       IServicioDialogo dialogos,
                                       ISesionActual sesion)
        : base(remesas, dialogos, sesion)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        _servicio = new RemesaService(remesas);
        _dialogos = dialogos;
        _sesionActual = sesion;
        _fincas = DataSourceFactory.CrearFincas();
        _nucleos = DataSourceFactory.CrearNucleos();
        _personal = DataSourceFactory.CrearPersonalCampo();
        _vehiculos = DataSourceFactory.CrearVehiculos();

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));

        ConfirmarCommand = new RelayCommand(Confirmar,
            () => SelectedItem is { } r && _servicio.PuedeConfirmar(r) && _sesionActual.Puede("Remesas.Confirmar"));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } r && _servicio.PuedeAnular(r) && _sesionActual.Puede("Remesas.Anular"));

        RegistrarRecepcionCommand = new RelayCommand(RegistrarRecepcion,
            () => SelectedItem is { } r && _servicio.PuedeRegistrarRecepcion(r) && _sesionActual.Puede("Remesas.Recepcion"));

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });
    }

    // --- Encabezado de la pantalla (mismo patrón que SubmoduloViewModel) ---
    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }
    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }
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
           || Contiene(item.RemeseroNombre, texto)
           || Contiene(item.NucleoCorteCodigo, texto)
           || Contiene(item.NucleoAlzaEmpujeCodigo, texto)
           || Contiene(item.NucleoTransporteCodigo, texto);

    private static bool Contiene(string valor, string texto)
        => valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Remesa CrearNuevo() => new()
    {
        Estado = EstadoRemesa.Borrador,
        FechaCreacion = DateTime.Now,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0
    };

    protected override CrudEditorViewModelBase<Remesa> CrearEditor(Remesa item)
        => new RemesaEditorViewModel(item, _fincas, _nucleos, _personal, _vehiculos);

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

        Aplicar(remesa, () => _servicio.Confirmar(remesa));
    }

    private void Anular()
    {
        if (SelectedItem is not { } remesa)
            return;

        var editor = new AnularRemesaEditorViewModel(remesa);
        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(remesa, () => _servicio.Anular(remesa, editor.Motivo));
    }

    private void RegistrarRecepcion()
    {
        if (SelectedItem is not { } remesa)
            return;

        var editor = new RecepcionRemesaEditorViewModel(remesa);
        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(remesa, () => _servicio.RegistrarRecepcion(remesa, editor.Llegada, editor.PesoBrutoT, editor.TaraT));
    }

    /// <summary>
    /// Ejecuta una transición y refleja el resultado en la lista. El servicio devuelve una copia,
    /// así que hay que reemplazar el elemento (los modelos no notifican cambios por sí solos).
    /// </summary>
    private void Aplicar(Remesa original, Func<Remesa> transicion)
    {
        Remesa actualizada;
        try
        {
            actualizada = transicion();
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Remesas", ex.Message);
            return;
        }

        var indice = Items.IndexOf(original);
        if (indice >= 0)
            Items[indice] = actualizada;

        SelectedItem = actualizada;
        ItemsView.Refresh();
    }
}
