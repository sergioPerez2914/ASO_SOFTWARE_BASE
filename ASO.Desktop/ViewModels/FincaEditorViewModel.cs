using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Fila editable de un Lote dentro del editor de Finca. <see cref="Models.Lote"/> no notifica
/// cambios (regla del proyecto: los modelos no implementan INotifyPropertyChanged), así que el
/// editor trabaja sobre esta copia observable y recién arma la lista real al guardar.
/// </summary>
public sealed class LoteEditorRow : ViewModelBase
{
    public LoteEditorRow(Lote? origen = null)
    {
        if (origen is not null)
        {
            Nombre = origen.Nombre;
            foreach (var tablon in origen.Tablones)
                Tablones.Add(new TablonEditorRow(tablon));
        }

        AgregarTablonCommand = new RelayCommand(() => Tablones.Add(new TablonEditorRow()));
        EliminarTablonCommand = new RelayCommand<TablonEditorRow>(t => Tablones.Remove(t));
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    public ObservableCollection<TablonEditorRow> Tablones { get; } = new();

    public ICommand AgregarTablonCommand { get; }
    public ICommand EliminarTablonCommand { get; }
}

/// <summary>Fila editable de un Tablón, siempre dentro de un <see cref="LoteEditorRow"/>.</summary>
public sealed class TablonEditorRow : ViewModelBase
{
    public TablonEditorRow(Tablon? origen = null)
    {
        if (origen is not null)
            Nombre = origen.Nombre;
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }
}

/// <summary>
/// Alta/edición de una finca, con sus Lotes y Tablones anidados. El código CAM identifica la
/// finca ante el central, así que no puede repetirse.
///
/// Al guardar se reconstruye toda la colección de Lotes/Tablones desde cero (Ids en 0):
/// <see cref="BD.SqlFincaDataSource"/>.Update ya está pensado como reemplazo total de la
/// colección, no como un diff, así que no hace falta preservar los Ids viejos aquí.
/// </summary>
public sealed class FincaEditorViewModel : CrudEditorViewModelBase<Finca>
{
    private readonly Finca _original;
    private readonly IFincaDataSource _fincas;

    public FincaEditorViewModel(Finca original, IFincaDataSource fincas)
    {
        _original = original;
        _fincas = fincas;

        CodigoCam = original.CodigoCam;
        Nombre = original.Nombre;

        foreach (var lote in original.Lotes)
            Lotes.Add(new LoteEditorRow(lote));

        AgregarLoteCommand = new RelayCommand(() => Lotes.Add(new LoteEditorRow()));
        EliminarLoteCommand = new RelayCommand<LoteEditorRow>(l => Lotes.Remove(l));
    }

    public override string Titulo => _original.Id == 0 ? "Nueva finca" : $"Editar finca Nº {_original.Id}";

    public override double AnchoEditor => Ancho.Amplio;

    private string _codigoCam = string.Empty;
    public string CodigoCam
    {
        get => _codigoCam;
        set => SetProperty(ref _codigoCam, value);
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    public ObservableCollection<LoteEditorRow> Lotes { get; } = new();

    public ICommand AgregarLoteCommand { get; }
    public ICommand EliminarLoteCommand { get; }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(CodigoCam))
        {
            error = "Indique el código CAM de la finca.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el nombre de la finca.";
            return false;
        }

        var repetida = _fincas.GetAll()
            .Any(f => f.Id != _original.Id
                      && string.Equals(f.CodigoCam.Trim(), CodigoCam.Trim(), StringComparison.OrdinalIgnoreCase));

        if (repetida)
        {
            error = $"Ya existe una finca con el código {CodigoCam.Trim()}.";
            return false;
        }

        if (Lotes.Any(l => string.IsNullOrWhiteSpace(l.Nombre)))
        {
            error = "Todos los lotes deben tener nombre.";
            return false;
        }

        if (Lotes.Any(l => l.Tablones.Any(t => string.IsNullOrWhiteSpace(t.Nombre))))
        {
            error = "Todos los tablones deben tener nombre.";
            return false;
        }

        error = null;
        return true;
    }

    public override Finca ObtenerResultado() => new()
    {
        Id = _original.Id,
        CodigoCam = CodigoCam.Trim(),
        Nombre = Nombre.Trim(),
        Lotes = Lotes.Select(l => new Lote
        {
            Nombre = l.Nombre.Trim(),
            Tablones = l.Tablones.Select(t => new Tablon { Nombre = t.Nombre.Trim() }).ToList()
        }).ToList()
    };
}
