using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using ASO.Desktop.Models;
using ASO.Desktop.Services;
using ASO.Desktop.BD;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Lógica de presentación del módulo de inventario: listado filtrable e indicadores.
/// </summary>
public class InventoryViewModel : ViewModelBase
{
    public const string TodasCategorias = "Todas las categorías";

    private readonly List<InventoryItem> _allItems;

    public ICollectionView ItemsView { get; }
    public ObservableCollection<string> Categorias { get; }

    public InventoryViewModel() : this(new SqlInventoryDataSource()) { }

    public InventoryViewModel(IInventoryDataSource source)
    {
        _allItems = source.GetItems().ToList();

        ItemsView = CollectionViewSource.GetDefaultView(_allItems);
        ItemsView.Filter = FilterItem;

        Categorias = new ObservableCollection<string>(
            new[] { TodasCategorias }
                .Concat(_allItems.Select(i => i.Categoria).Distinct().OrderBy(c => c)));

        _categoriaSeleccionada = TodasCategorias;
    }

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) ItemsView.Refresh(); }
    }

    private string _categoriaSeleccionada;
    public string CategoriaSeleccionada
    {
        get => _categoriaSeleccionada;
        set { if (SetProperty(ref _categoriaSeleccionada, value)) ItemsView.Refresh(); }
    }

    // Indicadores calculados sobre el inventario completo (no dependen del filtro).
    public int TotalArticulos => _allItems.Count;
    public decimal ValorTotal => _allItems.Sum(i => i.ValorTotal);
    public int BajoMinimo => _allItems.Count(i => i.Estado == StockStatus.Bajo);
    public int Agotados => _allItems.Count(i => i.Estado == StockStatus.Agotado);

    private bool FilterItem(object obj)
    {
        if (obj is not InventoryItem item)
            return false;

        if (_categoriaSeleccionada != TodasCategorias && item.Categoria != _categoriaSeleccionada)
            return false;

        if (string.IsNullOrWhiteSpace(_busqueda))
            return true;

        var q = _busqueda.Trim();
        return item.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
            || item.Codigo.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
