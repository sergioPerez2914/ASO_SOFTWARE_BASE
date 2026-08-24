using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Captura de una recarga de stock de combustible. No hereda de la base genérica porque no edita
/// una entidad existente: solo recoge los datos con los que el servicio construye y aplica la
/// recarga (el tope de capacidad lo valida <see cref="CombustibleService"/>).
/// </summary>
public sealed class RecargaEditorViewModel : CrudEditorViewModelBase
{
    private readonly IStockCombustibleDataSource _stockCombustible;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    public RecargaEditorViewModel(IStockCombustibleDataSource stockCombustible, IServicioDialogo dialogos, ISesionActual sesion)
    {
        _stockCombustible = stockCombustible;
        _dialogos = dialogos;
        _sesion = sesion;

        StocksCombustible = new ObservableCollection<StockCombustible>(stockCombustible.GetAll().Where(t => t.Activo));
        StockSeleccionado = StocksCombustible.FirstOrDefault();

        NuevoStockCommand = new RelayCommand(NuevoStock, () => _sesion.Puede(Permisos.Combustible.CrearStock));
    }

    public override string Titulo => "Registrar recarga de stock de combustible";
    public override double AnchoEditor => Ancho.Estandar;

    public ObservableCollection<StockCombustible> StocksCombustible { get; }

    public ICommand NuevoStockCommand { get; }

    private void NuevoStock()
    {
        var editor = new StockCombustibleEditorViewModel();
        if (!_dialogos.MostrarEditor(editor))
            return;

        var nuevo = _stockCombustible.Add(editor.ObtenerResultado());
        StocksCombustible.Add(nuevo);
        StockSeleccionado = nuevo;
    }

    private StockCombustible? _stockSeleccionado;
    public StockCombustible? StockSeleccionado
    {
        get => _stockSeleccionado;
        set
        {
            if (SetProperty(ref _stockSeleccionado, value))
                OnPropertyChanged(nameof(EspacioTexto));
        }
    }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private string _litros = string.Empty;
    public string LitrosTexto
    {
        get => _litros;
        set => SetProperty(ref _litros, value);
    }

    private string _costo = string.Empty;
    public string CostoTexto
    {
        get => _costo;
        set => SetProperty(ref _costo, value);
    }

    private string _proveedorNombre = string.Empty;
    public string ProveedorNombre
    {
        get => _proveedorNombre;
        set => SetProperty(ref _proveedorNombre, value);
    }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    public string EspacioTexto => StockSeleccionado is { } t
        ? $"Contiene {t.ExistenciaTexto}; admite {t.CapacidadL - t.ExistenciaL:N0} L más."
        : "Seleccione el stock de combustible que se recarga.";

    protected override bool Validar(out string? error)
    {
        if (StockSeleccionado is null)
        {
            error = "Seleccione el stock de combustible que se recarga.";
            return false;
        }

        if (!decimal.TryParse(LitrosTexto, out var litros) || litros <= 0)
        {
            error = "Los litros recibidos deben ser un número mayor que cero.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(CostoTexto) && !decimal.TryParse(CostoTexto, out _))
        {
            error = "El costo total debe ser un número.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Recarga lista para pasársela a <see cref="CombustibleService.RegistrarRecarga"/>.</summary>
    public RecargaCombustible ObtenerRecarga(int creadoPorId) => new()
    {
        Fecha = Fecha,
        StockCombustibleId = StockSeleccionado?.Id ?? 0,
        StockCombustibleNombre = StockSeleccionado?.Nombre ?? string.Empty,
        Litros = decimal.TryParse(LitrosTexto, out var litros) ? litros : 0m,
        CostoTotal = decimal.TryParse(CostoTexto, out var costo) ? costo : null,
        ProveedorNombre = ProveedorNombre.Trim(),
        Notas = Notas.Trim(),
        CreadoPorId = creadoPorId,
        FechaCreacion = DateTime.Now
    };
}
