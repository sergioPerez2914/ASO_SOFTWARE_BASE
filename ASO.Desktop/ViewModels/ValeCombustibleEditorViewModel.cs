using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un vale de combustible (solo en borrador).
///
/// La etiqueta y la ayuda del campo de lectura cambian según el activo elegido: en transporte
/// se pide odómetro y en máquinas horómetro. Mostrar la última lectura conocida evita el error
/// más común del despacho, que es teclear una lectura por debajo de la anterior.
/// </summary>
public sealed class ValeCombustibleEditorViewModel : CrudEditorViewModelBase<ValeCombustible>
{
    private readonly ValeCombustible _original;
    private readonly IStockCombustibleDataSource _stockCombustible;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    public ValeCombustibleEditorViewModel(ValeCombustible original,
                                          IStockCombustibleDataSource stockCombustible,
                                          IActivoFlotaDataSource activos,
                                          IServicioDialogo dialogos,
                                          ISesionActual sesion)
    {
        _original = original;
        _stockCombustible = stockCombustible;
        _dialogos = dialogos;
        _sesion = sesion;

        StocksCombustible = new ObservableCollection<StockCombustible>(stockCombustible.GetAll().Where(t => t.Activo));
        Activos = activos.GetAll().OrderBy(a => a.Codigo).ToList();

        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        Litros = original.Litros == 0 ? string.Empty : original.Litros.ToString("0.##");
        Lectura = original.Lectura?.ToString("0.##") ?? string.Empty;
        ResponsableNombre = original.ResponsableNombre;
        Notas = original.Notas;

        StockSeleccionado = StocksCombustible.FirstOrDefault(t => t.Id == original.StockCombustibleId) ?? StocksCombustible.FirstOrDefault();
        ActivoSeleccionado = Activos.FirstOrDefault(a => a.Id == original.ActivoId);

        NuevoStockCommand = new RelayCommand(NuevoStock, () => _sesion.Puede(Permisos.Combustible.CrearStock));
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo vale de combustible" : $"Editar vale Nº {_original.Id}";
    public override double AnchoEditor => Ancho.Estandar;

    public ObservableCollection<StockCombustible> StocksCombustible { get; }
    public IReadOnlyList<ActivoFlota> Activos { get; }

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
                OnPropertyChanged(nameof(ExistenciaTexto));
        }
    }

    private ActivoFlota? _activoSeleccionado;
    public ActivoFlota? ActivoSeleccionado
    {
        get => _activoSeleccionado;
        set
        {
            if (SetProperty(ref _activoSeleccionado, value))
            {
                OnPropertyChanged(nameof(EtiquetaLectura));
                OnPropertyChanged(nameof(AyudaLectura));
            }
        }
    }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private string _litros = string.Empty;
    public string Litros
    {
        get => _litros;
        set => SetProperty(ref _litros, value);
    }

    private string _lectura = string.Empty;
    public string Lectura
    {
        get => _lectura;
        set => SetProperty(ref _lectura, value);
    }

    private string _responsableNombre = string.Empty;
    public string ResponsableNombre
    {
        get => _responsableNombre;
        set => SetProperty(ref _responsableNombre, value);
    }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    public string ExistenciaTexto => StockSeleccionado is { } t
        ? $"Existencia de {t.Nombre}: {t.ExistenciaTexto} ({t.PorcentajeTexto})"
        : "Seleccione el stock de combustible de origen.";

    public string EtiquetaLectura => ActivoSeleccionado?.EsTransporte == true
        ? "Odómetro (km)"
        : "Horómetro (h)";

    public string AyudaLectura => ActivoSeleccionado is null
        ? "Elija el activo para saber qué instrumento se lee."
        : $"Última lectura registrada: {ActivoSeleccionado.UsoTexto}.";

    protected override bool Validar(out string? error)
    {
        if (StockSeleccionado is null)
        {
            error = "Seleccione el stock de combustible de origen.";
            return false;
        }

        if (ActivoSeleccionado is null)
        {
            error = "Seleccione el activo que recibe el combustible.";
            return false;
        }

        if (!decimal.TryParse(Litros, out var litros) || litros <= 0)
        {
            error = "Los litros despachados deben ser un número mayor que cero.";
            return false;
        }

        if (!decimal.TryParse(Lectura, out var lectura) || lectura <= 0)
        {
            error = $"Indique la lectura del {(ActivoSeleccionado.EsTransporte ? "odómetro" : "horómetro")}.";
            return false;
        }

        error = null;
        return true;
    }

    public override ValeCombustible ObtenerResultado()
    {
        var vale = _original.Clonar();

        vale.Fecha = Fecha;
        vale.StockCombustibleId = StockSeleccionado?.Id ?? 0;
        vale.StockCombustibleNombre = StockSeleccionado?.Nombre ?? string.Empty;
        vale.ActivoId = ActivoSeleccionado?.Id ?? 0;
        vale.ActivoCodigo = ActivoSeleccionado?.Codigo ?? string.Empty;
        vale.ActivoEtiqueta = ActivoSeleccionado?.Etiqueta ?? string.Empty;
        vale.EsTransporte = ActivoSeleccionado?.EsTransporte ?? false;
        vale.Litros = decimal.TryParse(Litros, out var litros) ? litros : 0m;
        vale.Lectura = decimal.TryParse(Lectura, out var lectura) ? lectura : null;
        vale.ResponsableNombre = ResponsableNombre.Trim();
        vale.Notas = Notas.Trim();

        return vale;
    }
}
