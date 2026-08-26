using System;
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
    public LubricantesCrudViewModel(ILubricanteDataSource lubricantes, IServicioDialogo dialogos, ISesionActual sesion)
        : base(lubricantes, dialogos, sesion)
    {
    }

    protected override string ModuloPermiso => "Lubricantes";

    protected override bool CoincideBusqueda(Lubricante item, string texto) =>
        item.Marca.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Tipo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.GradoViscosidad.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Lubricante CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<Lubricante> CrearEditor(Lubricante item) =>
        new LubricanteEditorViewModel(item);
}
