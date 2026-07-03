using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Base para pantallas "listado con alta/edición/baja" de un catálogo maestro:
/// selección, filtro de texto y los cuatro comandos CRUD con su gating por permiso.
/// Cada maestro solo aporta los cuatro puntos de extensión de abajo.
/// </summary>
public abstract class CrudViewModelBase<T, TId> : ViewModelBase where T : IEntidad<TId>
{
    private readonly ICrudDataSource<T, TId> _source;
    private readonly IServicioDialogo _dialogo;
    private readonly ISesionActual _sesion;

    public ObservableCollection<T> Items { get; }
    public ICollectionView ItemsView { get; }

    protected CrudViewModelBase(ICrudDataSource<T, TId> source,
                                IServicioDialogo? dialogo = null,
                                ISesionActual? sesion = null)
    {
        _source = source;
        _dialogo = dialogo ?? new ServicioDialogo();
        _sesion = sesion ?? SesionActual.Instancia;

        Items = new ObservableCollection<T>(_source.GetAll());
        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FiltrarItem;

        AgregarCommand = new RelayCommand(Agregar, () => _sesion.Puede($"{ModuloPermiso}.Crear"));
        EditarCommand = new RelayCommand(Editar, () => SelectedItem is not null && _sesion.Puede($"{ModuloPermiso}.Editar"));
        EliminarCommand = new RelayCommand(Eliminar, () => SelectedItem is not null && _sesion.Puede($"{ModuloPermiso}.Eliminar"));
        RefrescarCommand = new RelayCommand(Refrescar);
    }

    private T? _selectedItem;
    public T? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (SetProperty(ref _textoBusqueda, value)) ItemsView.Refresh(); }
    }

    public ICommand AgregarCommand { get; }
    public ICommand EditarCommand { get; }
    public ICommand EliminarCommand { get; }
    public ICommand RefrescarCommand { get; }

    /// <summary>Prefijo de permiso RBAC del módulo, p. ej. "Empleados" (→ "Empleados.Crear").</summary>
    protected abstract string ModuloPermiso { get; }

    protected abstract bool CoincideBusqueda(T item, string texto);
    protected abstract T CrearNuevo();
    protected abstract CrudEditorViewModelBase<T> CrearEditor(T item);

    private bool FiltrarItem(object obj)
        => obj is T item && (string.IsNullOrWhiteSpace(TextoBusqueda) || CoincideBusqueda(item, TextoBusqueda.Trim()));

    private void Agregar()
    {
        var editor = CrearEditor(CrearNuevo());
        if (!_dialogo.MostrarEditor(editor))
            return;

        var agregado = _source.Add(editor.ObtenerResultado());
        Items.Add(agregado);
        SelectedItem = agregado;
    }

    private void Editar()
    {
        if (SelectedItem is not { } actual)
            return;

        var editor = CrearEditor(actual);
        if (!_dialogo.MostrarEditor(editor))
            return;

        var actualizado = editor.ObtenerResultado();
        _source.Update(actualizado);

        var indice = Items.IndexOf(actual);
        if (indice >= 0)
            Items[indice] = actualizado;

        SelectedItem = actualizado;
    }

    private void Eliminar()
    {
        if (SelectedItem is not { } actual)
            return;

        if (!_dialogo.Confirmar("Eliminar", "¿Eliminar el registro seleccionado? Esta acción no se puede deshacer."))
            return;

        _source.Delete(actual.Id);
        Items.Remove(actual);
        SelectedItem = default;
    }

    private void Refrescar()
    {
        Items.Clear();
        foreach (var item in _source.GetAll())
            Items.Add(item);
    }
}
