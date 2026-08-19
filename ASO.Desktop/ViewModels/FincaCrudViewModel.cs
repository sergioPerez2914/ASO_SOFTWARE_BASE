using System;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Catálogo de fincas, con sus lotes y tablones. Sub-listado de Operaciones · Fincas y Núcleos
/// (ver <see cref="FincasYNucleosViewModel"/>).
/// </summary>
public sealed class FincaCrudViewModel : CrudViewModelBase<Finca, int>
{
    private readonly IFincaDataSource _fincas;

    public FincaCrudViewModel(IFincaDataSource fincas, IServicioDialogo dialogos, ISesionActual sesion)
        : base(fincas, dialogos, sesion)
    {
        _fincas = fincas;
    }

    protected override string ModuloPermiso => "Fincas";

    protected override bool CoincideBusqueda(Finca item, string texto) =>
        item.CodigoCam.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Finca CrearNuevo() => new();

    protected override CrudEditorViewModelBase<Finca> CrearEditor(Finca item) =>
        new FincaEditorViewModel(item, _fincas);
}
