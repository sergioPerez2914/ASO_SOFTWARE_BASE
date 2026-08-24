using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Inventario · Combustible: los vales despachados y el estado de las cisternas.
///
/// Las reglas viven en <see cref="CombustibleService"/>; aquí solo se pide la acción y se
/// refleja el resultado. Tras cada transición se recargan las cisternas porque su existencia
/// cambió: la tarjeta de arriba y la grilla miran el mismo hecho desde dos ángulos.
/// </summary>
public sealed class CombustibleViewModel : PantallaCrudViewModel<ValeCombustible, int>
{
    private const string FiltroTodos = "Todos";

    private readonly IValeCombustibleDataSource _vales;
    private readonly ITanqueCombustibleDataSource _tanques;
    private readonly IServicioDialogo _dialogos;
    private readonly SolicitudesDeCambio _solicitudes;
    private readonly ISesionActual _sesionActual;
    private readonly CombustibleService _servicio;

    private string _filtroEstado = FiltroTodos;

    public CombustibleViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearValesCombustible(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private CombustibleViewModel(Modulo modulo,
                                 Submodulo submodulo,
                                 IValeCombustibleDataSource vales,
                                 IServicioDialogo dialogos,
                                 ISesionActual sesion)
        : base(modulo, submodulo, vales, dialogos, sesion)
    {
        _vales = vales;
        _dialogos = dialogos;
        _solicitudes = new SolicitudesDeCambio(sesion, dialogos);
        _sesionActual = sesion;
        _tanques = DataSourceFactory.CrearTanquesCombustible();

        _servicio = new CombustibleService(
            vales, _tanques, DataSourceFactory.CrearRecargasCombustible(), DataSourceFactory.CrearActivosFlota());

        Tanques = new ObservableCollection<TanqueCombustible>(_tanques.GetAll());

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        ConfirmarCommand = new RelayCommand(Confirmar,
            () => SelectedItem is { } v && _servicio.PuedeConfirmar(v) && _sesionActual.Puede("Combustible.Confirmar"));

        // Anular un vale y recargar la cisterna son de administrador; el remesero las solicita.
        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } v && _servicio.PuedeAnular(v) && _solicitudes.PuedeIntentar(Permisos.Combustible.Anular));

        RegistrarRecargaCommand = new RelayCommand(RegistrarRecarga,
            () => _solicitudes.PuedeIntentar(Permisos.Combustible.Recargar));
    }

    // --- Encabezado de la pantalla ---

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand ConfirmarCommand { get; }
    public ICommand AnularCommand { get; }
    public ICommand RegistrarRecargaCommand { get; }

    public ObservableCollection<TanqueCombustible> Tanques { get; }

    /// <summary>
    /// Rendimiento del centro en la última semana: litros despachados por tonelada recibida.
    /// Es el número que el reglamento vuelve comparable entre zafras.
    /// </summary>
    public string RendimientoTexto
    {
        get
        {
            var litrosPorTonelada = _servicio.LitrosPorTonelada(DataSourceFactory.CrearRemesas());
            return litrosPorTonelada is { } valor
                ? $"Rendimiento (7 días): {valor:N2} L/t"
                : "Rendimiento (7 días): sin caña recibida en el período";
        }
    }

    // --- Puntos de extensión del CRUD ---

    protected override string ModuloPermiso => "Combustible";

    protected override bool CoincideBusqueda(ValeCombustible item, string texto) =>
        item.ActivoCodigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.ActivoEtiqueta.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.TanqueNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.ResponsableNombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(ValeCombustible item) => _filtroEstado switch
    {
        "Borrador" => item.Estado == EstadoVale.Borrador,
        "Confirmado" => item.Estado == EstadoVale.Confirmado,
        "Anulado" => item.Estado == EstadoVale.Anulado,
        "Alerta" => item.AlertaConsumo,
        _ => true
    };

    protected override bool PuedeEditar(ValeCombustible item) => _servicio.PuedeEditar(item);

    protected override bool PuedeEliminar(ValeCombustible item) => _servicio.PuedeEliminar(item);

    protected override ValeCombustible CrearNuevo() => new()
    {
        Fecha = DateTime.Today,
        Estado = EstadoVale.Borrador,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0,
        FechaCreacion = DateTime.Now
    };

    protected override CrudEditorViewModelBase<ValeCombustible> CrearEditor(ValeCombustible item) =>
        new ValeCombustibleEditorViewModel(item, _tanques, DataSourceFactory.CrearActivosFlota(), _dialogos, _sesionActual);

    // --- Transiciones ---

    private void Confirmar()
    {
        if (SelectedItem is not { } vale)
            return;

        Aplicar(() => _servicio.Confirmar(vale));
    }

    private void Anular()
    {
        if (SelectedItem is not { } vale)
            return;

        if (_solicitudes.RequierePeticion(Permisos.Combustible.Anular))
        {
            _solicitudes.Solicitar(Permisos.Combustible.Anular, "Anular vale de combustible",
                nameof(ValeCombustible), vale.Id.ToString(),
                $"Vale Nº {vale.Id} · {vale.ActivoEtiqueta} — {vale.LitrosTexto}");
            return;
        }

        var editor = new MotivoEditorViewModel(
            $"Anular vale Nº {vale.Id}",
            $"{vale.ActivoEtiqueta} — {vale.LitrosTexto} desde {vale.TanqueNombre}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Anular(vale, editor.Motivo));
    }

    private void RegistrarRecarga()
    {
        if (_solicitudes.RequierePeticion(Permisos.Combustible.Recargar))
        {
            _solicitudes.Solicitar(Permisos.Combustible.Recargar, "Registrar recarga de cisterna",
                nameof(TanqueCombustible), string.Empty, "Entrada de combustible a la cisterna");
            return;
        }

        var editor = new RecargaEditorViewModel(_tanques, _dialogos, _sesionActual);
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            _servicio.RegistrarRecarga(editor.ObtenerRecarga(_sesionActual.UsuarioActual?.Id ?? 0));
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo registrar la recarga", ex.Message);
        }
    }

    /// <summary>
    /// Ejecuta una transición. Si el servicio la rechaza se informa en vez de tragarse el error:
    /// el botón habilitado es cortesía, la regla la impone el servicio.
    ///
    /// Ni la lista ni las cisternas se tocan aquí. Confirmar un vale escribe el vale, descuenta
    /// la cisterna y adelanta la lectura del activo; esas escrituras disparan por sí solas la
    /// recarga de la pantalla (ver <see cref="Recargar"/> y <c>Services/CambiosDeDatos</c>), que
    /// es justo lo que antes había que acordarse de cablear en cada acción nueva.
    /// </summary>
    private void Aplicar(Func<ValeCombustible> transicion)
    {
        try
        {
            var actualizado = transicion();
            SeleccionarTrasRecargar(actualizado.Id);

            if (actualizado.AlertaConsumo)
                _dialogos.Informar("Consumo por encima de lo habitual",
                    $"El vale Nº {actualizado.Id} marca {actualizado.ConsumoTexto} frente a un promedio de " +
                    $"{actualizado.PromedioTexto} en {actualizado.ActivoEtiqueta}. Conviene revisar la unidad.");
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
    }

    /// <summary>Relee los vales y, además, el nivel de las cisternas y el rendimiento.</summary>
    public override void Recargar()
    {
        base.Recargar();

        Tanques.Clear();
        foreach (var tanque in _tanques.GetAll())
            Tanques.Add(tanque);
    }
}
