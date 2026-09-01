using System;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Catálogo de lubricantes (Marca + Tipo + Grado), pestaña de Inventario · Combustible. Maestro
/// simple sin máquina de estados — mismo arquetipo que <c>RequisicionesCrudViewModel</c> dentro
/// de <see cref="ComprasViewModel"/>: un padrón CRUD propio compuesto dentro de la pantalla de
/// otro. El remesero ve la pestaña (tiene <c>Ver.Combustible</c>) pero no <see cref="Permisos.Lubricantes"/>,
/// así que sus botones de alta/edición/borrado quedan deshabilitados — solo lectura de hecho.
/// </summary>
public sealed class LubricantesCrudViewModel : CrudViewModelBase<Lubricante, int>
{
    private readonly IMarcaLubricanteDataSource _marcasLubricante;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    public LubricantesCrudViewModel(ILubricanteDataSource lubricantes,
                                    IMarcaLubricanteDataSource marcasLubricante,
                                    IServicioDialogo dialogos,
                                    ISesionActual sesion)
        : base(lubricantes, dialogos, sesion)
    {
        _marcasLubricante = marcasLubricante;
        _dialogos = dialogos;
        _sesion = sesion;

        // La tarjeta de valor depende de SelectedItem, TextoBusqueda (cambia qué entra en
        // ItemsView) y de una recarga completa (alta/edición/borrado) — la base solo notifica
        // las dos primeras por su propio nombre, así que hace falta escucharse a sí mismo.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null or "" or nameof(SelectedItem) or nameof(TextoBusqueda))
            {
                OnPropertyChanged(nameof(TituloValorTexto));
                OnPropertyChanged(nameof(ValorMostradoTexto));
            }
        };
    }

    protected override string ModuloPermiso => "Lubricantes";

    /// <summary>Suma de ExistenciaL × CostoUnitario de lo que se ve en la tabla ahora mismo (con
    /// el buscador aplicado), no del catálogo completo.</summary>
    public decimal ValorTotalCatalogo => ItemsView.Cast<Lubricante>().Sum(l => l.ValorTotal);

    /// <summary>Título de la tarjeta de valor: el total del catálogo visible, o la fila
    /// seleccionada cuando hay una.</summary>
    public string TituloValorTexto => SelectedItem is null
        ? "Valor total del catálogo"
        : $"Valor de {SelectedItem.MarcaLubricanteNombre} · {SelectedItem.Tipo} {SelectedItem.GradoViscosidad}";

    public string ValorMostradoTexto => (SelectedItem?.ValorTotal ?? ValorTotalCatalogo).ToString("N2");

    protected override bool CoincideBusqueda(Lubricante item, string texto) =>
        item.MarcaLubricanteNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Tipo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.GradoViscosidad.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Lubricante CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<Lubricante> CrearEditor(Lubricante item) =>
        new LubricanteEditorViewModel(item, _marcasLubricante, _dialogos, _sesion);
}
