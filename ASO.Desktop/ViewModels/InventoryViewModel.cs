using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;
using ASO.Desktop.Configuration;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Lógica de presentación del módulo de inventario: catálogo maestro de artículos con
/// alta/edición/baja (reutiliza el esqueleto CRUD genérico) más filtro por categoría e
/// indicadores de stock que reaccionan a cada cambio de la lista.
/// </summary>
public class InventoryViewModel : CrudViewModelBase<InventoryItem, string>
{
    public const string TodasCategorias = "Todas las categorías";

    public InventoryViewModel() : this(DataSourceFactory.CrearInventario()) { }

    public InventoryViewModel(IInventoryDataSource source) : base(source)
    {
        Categorias = new ObservableCollection<string>();
        ReconstruirCategorias();

        // Los indicadores y la lista de categorías dependen del contenido completo:
        // se recalculan ante cada alta/edición/baja/refresco.
        Items.CollectionChanged += OnItemsChanged;
    }

    protected override string ModuloPermiso => "Inventario";

    // --- Filtro por categoría (se combina con el buscador de texto del base) ---

    public ObservableCollection<string> Categorias { get; }

    // Inicializador de campo: en C# corre ANTES del constructor base, que ya evalúa el
    // filtro al asignar ItemsView.Filter. Así PasaFiltroExtra nunca ve un valor nulo.
    private string _categoriaSeleccionada = TodasCategorias;
    public string CategoriaSeleccionada
    {
        get => _categoriaSeleccionada;
        set { if (SetProperty(ref _categoriaSeleccionada, value)) ItemsView.Refresh(); }
    }

    protected override bool PasaFiltroExtra(InventoryItem item)
        => _categoriaSeleccionada == TodasCategorias || item.Categoria == _categoriaSeleccionada;

    protected override bool CoincideBusqueda(InventoryItem item, string texto)
        => item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase);

    // --- Alta/edición ---

    protected override InventoryItem CrearNuevo() => new();

    protected override CrudEditorViewModelBase<InventoryItem> CrearEditor(InventoryItem item)
        => new InventoryEditorViewModel(item, CodigoDisponible);

    /// <summary>¿El código no está usado por otro artículo distinto al que se edita?</summary>
    private bool CodigoDisponible(string codigo, InventoryItem editado)
        => !Items.Any(i => !ReferenceEquals(i, editado)
                        && string.Equals(i.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    // --- Indicadores (sobre el inventario completo, no el filtrado) ---

    public int TotalArticulos => Items.Count;
    public decimal ValorTotal => Items.Sum(i => i.ValorTotal);
    public int BajoMinimo => Items.Count(i => i.Estado == StockStatus.Bajo);
    public int Agotados => Items.Count(i => i.Estado == StockStatus.Agotado);

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalArticulos));
        OnPropertyChanged(nameof(ValorTotal));
        OnPropertyChanged(nameof(BajoMinimo));
        OnPropertyChanged(nameof(Agotados));
        ReconstruirCategorias();
    }

    private void ReconstruirCategorias()
    {
        var seleccion = _categoriaSeleccionada;

        Categorias.Clear();
        Categorias.Add(TodasCategorias);
        foreach (var categoria in Items.Select(i => i.Categoria).Distinct().OrderBy(c => c))
            Categorias.Add(categoria);

        // Conservar la selección si sigue existiendo; si no, volver a "Todas".
        CategoriaSeleccionada = Categorias.Contains(seleccion) ? seleccion : TodasCategorias;
    }
}
