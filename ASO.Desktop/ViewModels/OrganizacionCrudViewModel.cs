using System;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Padrón de núcleos que usan ASO. Solo lo ve el Desarrollador: es la tabla que define el
/// ámbito, así que no lleva filtro por organización y el permiso es toda la barrera.
/// </summary>
public sealed class OrganizacionCrudViewModel : CrudViewModelBase<Organizacion, int>
{
    public OrganizacionCrudViewModel(IOrganizacionDataSource organizaciones,
                                     IServicioDialogo dialogos,
                                     ISesionActual sesion)
        : base(organizaciones, dialogos, sesion)
    {
    }

    protected override string ModuloPermiso => "Organizaciones";

    protected override bool CoincideBusqueda(Organizacion item, string texto) =>
        item.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Organizacion CrearNuevo() => new();

    protected override CrudEditorViewModelBase<Organizacion> CrearEditor(Organizacion item) =>
        new OrganizacionEditorViewModel(item);

    /// <summary>
    /// Un núcleo no se borra: sus filas quedarían huérfanas y, con el filtro fail-closed,
    /// invisibles para siempre sin que nadie pueda recuperarlas. Se desactiva desde el editor.
    /// </summary>
    protected override bool PuedeEliminar(Organizacion item) => false;
}
