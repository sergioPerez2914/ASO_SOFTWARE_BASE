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

/// <summary>Una entrega de caña al central, derivada de una remesa recibida.</summary>
public sealed record EntregaProducto(
    int RemesaId,
    DateTime Fecha,
    string FincaNombre,
    string Ubicacion,
    string NucleoTransporte,
    decimal Toneladas,
    string FacturadaTexto,
    bool Facturada)
{
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
    public string ToneladasTexto => $"{Toneladas:N2}";
}

/// <summary>
/// Inventario · Producto: la caña cosechada y entregada al ingenio.
///
/// Es una vista de solo lectura, no un catálogo: el "producto" del centro no se almacena ni se
/// da de alta a mano — se produce cortando y se entrega el mismo día. Lo que aquí se lee son
/// las remesas ya recibidas en el central, agregadas por período, con su estado de facturación.
///
/// PROVISIONAL: el corte por período usa rangos fijos. Cuando exista el maestro de zafras, el
/// reporte se filtrará por la zafra activa como el resto del sistema.
/// </summary>
public sealed class ProductoViewModel : ViewModelBase
{
    private const string RangoTodo = "Todo";

    private readonly IRemesaDataSource _remesas;
    private string _rango = RangoTodo;

    public event EventHandler? VolverSolicitado;

    public ProductoViewModel(Modulo modulo, Submodulo submodulo)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        _remesas = DataSourceFactory.CrearRemesas();

        Entregas = [];
        EntregasView = CollectionViewSource.GetDefaultView(Entregas);
        EntregasView.Filter = FiltrarEntrega;

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
        RefrescarCommand = new RelayCommand(Cargar);

        CambiarRangoCommand = new RelayCommand<string>(rango =>
        {
            _rango = rango;
            EntregasView.Refresh();
            ActualizarIndicadores();
        });

        Cargar();
    }

    // --- Encabezado de la pantalla ---
    public Modulo Modulo { get; }
    public Submodulo Submodulo { get; }
    public string Ruta => $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }
    public ICommand RefrescarCommand { get; }
    public ICommand CambiarRangoCommand { get; }

    public ObservableCollection<EntregaProducto> Entregas { get; }
    public ICollectionView EntregasView { get; }

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set
        {
            if (SetProperty(ref _textoBusqueda, value))
            {
                EntregasView.Refresh();
                ActualizarIndicadores();
            }
        }
    }

    // --- Indicadores del reporte ---

    private string _toneladasTexto = "0,00";
    public string ToneladasTexto
    {
        get => _toneladasTexto;
        private set => SetProperty(ref _toneladasTexto, value);
    }

    private string _entregasTexto = "0";
    public string EntregasTexto
    {
        get => _entregasTexto;
        private set => SetProperty(ref _entregasTexto, value);
    }

    private string _toneladasHoyTexto = "0,00";
    public string ToneladasHoyTexto
    {
        get => _toneladasHoyTexto;
        private set => SetProperty(ref _toneladasHoyTexto, value);
    }

    private string _sinFacturarTexto = "0,00";
    public string SinFacturarTexto
    {
        get => _sinFacturarTexto;
        private set => SetProperty(ref _sinFacturarTexto, value);
    }

    private void Cargar()
    {
        Entregas.Clear();

        var entregas = _remesas.GetAll()
            .Where(r => r.Estado == EstadoRemesa.Recibida)
            .Select(r => new EntregaProducto(
                r.Id,
                r.LlegadaCentral ?? r.FechaConfirmacion ?? r.FechaCreacion,
                r.FincaNombre,
                r.UbicacionTexto,
                r.NucleoTransporteCodigo,
                r.PesoNetoT ?? 0m,
                r.FacturadaTexto,
                r.Facturada))
            .OrderByDescending(e => e.Fecha);

        foreach (var entrega in entregas)
            Entregas.Add(entrega);

        ActualizarIndicadores();
    }

    private bool FiltrarEntrega(object obj)
    {
        if (obj is not EntregaProducto entrega)
            return false;

        var texto = TextoBusqueda.Trim();
        var coincide = string.IsNullOrWhiteSpace(texto)
            || entrega.FincaNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || entrega.Ubicacion.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || entrega.NucleoTransporte.Contains(texto, StringComparison.OrdinalIgnoreCase);

        var enRango = _rango switch
        {
            "Hoy" => entrega.Fecha.Date == DateTime.Today,
            "Semana" => entrega.Fecha.Date >= DateTime.Today.AddDays(-7),
            _ => true
        };

        return coincide && enRango;
    }

    /// <summary>Los indicadores miran lo filtrado, no todo el histórico: acompañan a la vista.</summary>
    private void ActualizarIndicadores()
    {
        var visibles = EntregasView.Cast<EntregaProducto>().ToList();

        ToneladasTexto = $"{visibles.Sum(e => e.Toneladas):N2}";
        EntregasTexto = visibles.Count.ToString();
        ToneladasHoyTexto = $"{visibles.Where(e => e.Fecha.Date == DateTime.Today).Sum(e => e.Toneladas):N2}";
        SinFacturarTexto = $"{visibles.Where(e => !e.Facturada).Sum(e => e.Toneladas):N2}";
    }
}
