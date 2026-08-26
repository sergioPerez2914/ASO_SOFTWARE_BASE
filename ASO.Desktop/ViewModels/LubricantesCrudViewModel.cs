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
    }

    protected override string ModuloPermiso => "Lubricantes";

    protected override bool CoincideBusqueda(Lubricante item, string texto) =>
        item.MarcaLubricanteNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Tipo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.GradoViscosidad.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Presentacion.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Lubricante CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<Lubricante> CrearEditor(Lubricante item) =>
        new LubricanteEditorViewModel(item, _marcasLubricante, _dialogos, _sesion);
}
