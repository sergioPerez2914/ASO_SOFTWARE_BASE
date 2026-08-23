using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de una salida de almacén (solo en borrador; una vez confirmada es inmutable).
///
/// El combo de artículos muestra la existencia disponible para que quien despacha lo vea antes
/// de pedir de más, y el de mantenimientos se filtra por el activo elegido: imputar una salida
/// al mantenimiento de otra máquina sería un error silencioso en el costo del taller.
/// </summary>
public sealed class SalidaInventarioEditorViewModel : CrudEditorViewModelBase<SalidaInventario>
{
    private readonly SalidaInventario _original;
    private readonly IMantenimientoRegistroDataSource _mantenimientos;

    public SalidaInventarioEditorViewModel(SalidaInventario original,
                                           IInventoryDataSource articulos,
                                           IActivoFlotaDataSource activos,
                                           IMantenimientoRegistroDataSource mantenimientos)
    {
        _original = original;
        _mantenimientos = mantenimientos;

        Articulos = articulos.GetAll().OrderBy(a => a.Nombre).ToList();
        Activos = activos.GetAll().OrderBy(a => a.Codigo).ToList();

        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        Cantidad = original.Cantidad == 0 ? string.Empty : original.Cantidad.ToString("0.##");
        Motivo = original.Motivo;

        ArticuloSeleccionado = Articulos.FirstOrDefault(a => a.Codigo == original.ArticuloCodigo);
        ActivoSeleccionado = Activos.FirstOrDefault(a => a.Id == original.ActivoId);
        RecargarMantenimientos();
        MantenimientoSeleccionado = Mantenimientos.FirstOrDefault(m => m.Id == original.MantenimientoId);
    }

    public override string Titulo => _original.Id == 0 ? "Nueva salida de almacén" : $"Editar salida Nº {_original.Id}";
    public override double AnchoEditor => Ancho.Estandar;

    public IReadOnlyList<InventoryItem> Articulos { get; }
    public IReadOnlyList<ActivoFlota> Activos { get; }

    private IReadOnlyList<MantenimientoRegistro> _mantenimientosDelActivo = [];
    public IReadOnlyList<MantenimientoRegistro> Mantenimientos
    {
        get => _mantenimientosDelActivo;
        private set => SetProperty(ref _mantenimientosDelActivo, value);
    }

    private InventoryItem? _articuloSeleccionado;
    public InventoryItem? ArticuloSeleccionado
    {
        get => _articuloSeleccionado;
        set
        {
            if (SetProperty(ref _articuloSeleccionado, value))
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
                RecargarMantenimientos();
                MantenimientoSeleccionado = null;
            }
        }
    }

    private MantenimientoRegistro? _mantenimientoSeleccionado;
    public MantenimientoRegistro? MantenimientoSeleccionado
    {
        get => _mantenimientoSeleccionado;
        set => SetProperty(ref _mantenimientoSeleccionado, value);
    }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private string _cantidad = string.Empty;
    public string Cantidad
    {
        get => _cantidad;
        set => SetProperty(ref _cantidad, value);
    }

    private string _motivo = string.Empty;
    public string Motivo
    {
        get => _motivo;
        set => SetProperty(ref _motivo, value);
    }

    /// <summary>Existencia del artículo elegido, para decidir con el dato a la vista.</summary>
    public string ExistenciaTexto => ArticuloSeleccionado is { } a
        ? $"Existencia disponible: {a.StockActual:N2} {a.Unidad} · costo {a.CostoUnitario:N2} / {a.Unidad}"
        : "Seleccione un artículo para ver su existencia.";

    private void RecargarMantenimientos() =>
        Mantenimientos = ActivoSeleccionado is { } activo
            ? _mantenimientos.GetByActivo(activo.Id).OrderByDescending(m => m.Fecha).ToList()
            : [];

    protected override bool Validar(out string? error)
    {
        if (ArticuloSeleccionado is null)
        {
            error = "Seleccione el artículo que sale del almacén.";
            return false;
        }

        if (!decimal.TryParse(Cantidad, out var cantidad) || cantidad <= 0)
        {
            error = "La cantidad debe ser un número mayor que cero.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Motivo) && ActivoSeleccionado is null)
        {
            error = "Indique el motivo de la salida o el activo al que se carga.";
            return false;
        }

        error = null;
        return true;
    }

    public override SalidaInventario ObtenerResultado()
    {
        var salida = _original.Clonar();

        salida.Fecha = Fecha;
        salida.ArticuloCodigo = ArticuloSeleccionado?.Codigo ?? string.Empty;
        salida.ArticuloNombre = ArticuloSeleccionado?.Nombre ?? string.Empty;
        salida.Unidad = ArticuloSeleccionado?.Unidad ?? string.Empty;
        salida.Cantidad = decimal.TryParse(Cantidad, out var cantidad) ? cantidad : 0m;
        salida.ActivoId = ActivoSeleccionado?.Id;
        salida.ActivoEtiqueta = ActivoSeleccionado?.Etiqueta ?? string.Empty;
        salida.MantenimientoId = MantenimientoSeleccionado?.Id;
        salida.Motivo = Motivo.Trim();

        return salida;
    }
}
