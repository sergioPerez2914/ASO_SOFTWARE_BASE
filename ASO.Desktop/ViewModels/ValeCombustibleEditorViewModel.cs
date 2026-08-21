using System;
using System.Collections.Generic;
using System.Linq;
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

    public ValeCombustibleEditorViewModel(ValeCombustible original,
                                          ITanqueCombustibleDataSource tanques,
                                          IActivoFlotaDataSource activos)
    {
        _original = original;

        Tanques = tanques.GetAll().Where(t => t.Activo).ToList();
        Activos = activos.GetAll().OrderBy(a => a.Codigo).ToList();

        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        Litros = original.Litros == 0 ? string.Empty : original.Litros.ToString("0.##");
        Lectura = original.Lectura?.ToString("0.##") ?? string.Empty;
        ResponsableNombre = original.ResponsableNombre;
        Notas = original.Notas;

        TanqueSeleccionado = Tanques.FirstOrDefault(t => t.Id == original.TanqueId) ?? Tanques.FirstOrDefault();
        ActivoSeleccionado = Activos.FirstOrDefault(a => a.Id == original.ActivoId);
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo vale de combustible" : $"Editar vale Nº {_original.Id}";
    public override double AnchoEditor => 520;

    public IReadOnlyList<TanqueCombustible> Tanques { get; }
    public IReadOnlyList<ActivoFlota> Activos { get; }

    private TanqueCombustible? _tanqueSeleccionado;
    public TanqueCombustible? TanqueSeleccionado
    {
        get => _tanqueSeleccionado;
        set
        {
            if (SetProperty(ref _tanqueSeleccionado, value))
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

    public string ExistenciaTexto => TanqueSeleccionado is { } t
        ? $"Existencia en {t.Nombre.ToLowerInvariant()}: {t.ExistenciaTexto} ({t.PorcentajeTexto})"
        : "Seleccione la cisterna de origen.";

    public string EtiquetaLectura => ActivoSeleccionado?.EsTransporte == true
        ? "Odómetro (km)"
        : "Horómetro (h)";

    public string AyudaLectura => ActivoSeleccionado is null
        ? "Elija el activo para saber qué instrumento se lee."
        : $"Última lectura registrada: {ActivoSeleccionado.UsoTexto}.";

    protected override bool Validar(out string? error)
    {
        if (TanqueSeleccionado is null)
        {
            error = "Seleccione la cisterna de origen.";
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
        vale.TanqueId = TanqueSeleccionado?.Id ?? 0;
        vale.TanqueNombre = TanqueSeleccionado?.Nombre ?? string.Empty;
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
