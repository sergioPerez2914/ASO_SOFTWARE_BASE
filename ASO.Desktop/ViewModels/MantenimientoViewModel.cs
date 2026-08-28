using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>Opción del filtro por activo; la primera es "Todos" (Activo null).</summary>
public sealed class OpcionActivo
{
    public OpcionActivo(ActivoFlota? activo) => Activo = activo;

    public ActivoFlota? Activo { get; }
    public string Etiqueta => Activo?.Etiqueta ?? "Todos los activos";
}

/// <summary>
/// Flota · Mantenimiento: revisiones recomendadas (calculadas por intervalo contra el
/// historial) y el registro de mantenimientos realizados. Los registros son constancias
/// inmutables: se agregan, no se editan ni se borran.
/// </summary>
public sealed class MantenimientoViewModel : PantallaViewModelBase
{
    private const string FiltroTodos = "Todos";

    private readonly MantenimientoService _servicio;
    private readonly IMantenimientoRegistroDataSource _registrosFuente;
    private readonly IActivoFlotaDataSource _activosFuente;
    private readonly IRemesaDataSource _remesas;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    /// <summary>Se dispara al pedir volver al dashboard del módulo; la ventana principal navega.</summary>

    public MantenimientoViewModel(Modulo modulo, Submodulo submodulo, ISesionActual? sesion = null)
        : base(modulo, submodulo)
    {
        // Resuelto ANTES de construir _servicio: mismo bug de orden que ya se corrigió en
        // GestionFlotaViewModel — asignar _sesion al final la dejaba en null para el servicio.
        _sesion = sesion ?? SesionActual.Instancia;

        _registrosFuente = DataSourceFactory.CrearMantenimientos();
        _activosFuente = DataSourceFactory.CrearActivosFlota();
        _remesas = DataSourceFactory.CrearRemesas();
        _servicio = new MantenimientoService(_registrosFuente, _activosFuente,
            DataSourceFactory.CrearReglasMantenimiento(), DataSourceFactory.CrearEventosOperacion(), _remesas, _sesion);
        _dialogos = new ServicioDialogo();

        // El filtro debe tener valor ANTES de cablear la vista: agregar el SortDescription
        // dispara un Refresh que ya ejecuta Filtrar.
        OpcionesActivo = [new OpcionActivo(null), .. _activosFuente.GetAll()
            .OrderBy(a => a.Codigo).Select(a => new OpcionActivo(a))];
        _activoFiltro = OpcionesActivo[0];

        Registros = new ObservableCollection<MantenimientoRegistro>(_registrosFuente.GetAll());
        RegistrosView = CollectionViewSource.GetDefaultView(Registros);
        RegistrosView.Filter = Filtrar;
        RegistrosView.SortDescriptions.Add(
            new SortDescription(nameof(MantenimientoRegistro.Fecha), ListSortDirection.Descending));

        RegistrarCommand = new RelayCommand(() => Registrar(null, null),
            () => _sesion.Puede("Mantenimiento.Registrar"));

        RegistrarDesdeRecomendacionCommand = new RelayCommand<RecomendacionMantenimiento>(
            r => Registrar(r.Activo, r.Regla.Revision),
            _ => _sesion.Puede("Mantenimiento.Registrar"));

        CambiarFiltroTipoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroTipo = filtro;
            RegistrosView.Refresh();
        });

        RecalcularRecomendaciones();
    }

    // --- Encabezado ---

    public ICommand RegistrarCommand { get; }
    public ICommand RegistrarDesdeRecomendacionCommand { get; }
    public ICommand CambiarFiltroTipoCommand { get; }

    public ObservableCollection<MantenimientoRegistro> Registros { get; }
    public ICollectionView RegistrosView { get; }

    /// <summary>Solo las revisiones que exigen atención (vencidas o próximas).</summary>
    public ObservableCollection<RecomendacionMantenimiento> Recomendaciones { get; } = [];

    private bool _hayRecomendaciones;
    public bool HayRecomendaciones
    {
        get => _hayRecomendaciones;
        private set => SetProperty(ref _hayRecomendaciones, value);
    }

    public ObservableCollection<OpcionActivo> OpcionesActivo { get; }

    // Nullable a propósito: el ComboBox puede dejar la selección en null.
    private OpcionActivo? _activoFiltro;
    public OpcionActivo? ActivoFiltro
    {
        get => _activoFiltro;
        set { if (SetProperty(ref _activoFiltro, value)) RegistrosView.Refresh(); }
    }

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (SetProperty(ref _textoBusqueda, value)) RegistrosView.Refresh(); }
    }

    private string _filtroTipo = FiltroTodos;

    private bool Filtrar(object obj)
    {
        if (obj is not MantenimientoRegistro registro) return false;

        if (ActivoFiltro?.Activo is { } activo && registro.ActivoId != activo.Id)
            return false;

        if (_filtroTipo != FiltroTodos && registro.TipoTexto != _filtroTipo)
            return false;

        var texto = TextoBusqueda.Trim();
        return texto.Length == 0
               || Contiene(registro.ActivoCodigo, texto)
               || Contiene(registro.ActivoEtiqueta, texto)
               || Contiene(registro.Descripcion, texto)
               || Contiene(registro.RealizadoPor, texto);
    }

    private static bool Contiene(string valor, string texto)
        => valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    private void Registrar(ActivoFlota? preseleccionado, string? descripcionSugerida)
    {
        var editor = new MantenimientoEditorViewModel(new MantenimientoRegistro(),
            [.. _activosFuente.GetAll().OrderBy(a => a.Codigo)], _remesas,
            preseleccionado, descripcionSugerida);

        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            // Guardar dispara la recarga de la pantalla, que repuebla los registros y vuelve a
            // calcular las revisiones pendientes.
            _servicio.Registrar(editor.ObtenerResultado());
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("Mantenimiento", ex.Message);
        }
    }

    /// <summary>Relee los registros y vuelve a calcular las revisiones pendientes.</summary>
    public override void Recargar()
    {
        Registros.Clear();
        foreach (var registro in _registrosFuente.GetAll())
            Registros.Add(registro);

        RecalcularRecomendaciones();
    }

    private void RecalcularRecomendaciones()
    {
        Recomendaciones.Clear();
        foreach (var recomendacion in _servicio.CalcularRecomendaciones()
                     .Where(r => r.Estado != EstadoRecomendacion.AlDia))
            Recomendaciones.Add(recomendacion);

        HayRecomendaciones = Recomendaciones.Count > 0;
    }
}
