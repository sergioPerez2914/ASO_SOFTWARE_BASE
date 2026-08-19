using System;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Catálogo de núcleos de productores. Sub-listado de Operaciones · Fincas y Núcleos
/// (ver <see cref="FincasYNucleosViewModel"/>).
/// </summary>
public sealed class NucleoCrudViewModel : CrudViewModelBase<Nucleo, int>
{
    private readonly INucleoDataSource _nucleos;

    public NucleoCrudViewModel(INucleoDataSource nucleos, IServicioDialogo dialogos, ISesionActual sesion)
        : base(nucleos, dialogos, sesion)
    {
        _nucleos = nucleos;
    }

    protected override string ModuloPermiso => "Nucleos";

    protected override bool CoincideBusqueda(Nucleo item, string texto) =>
        item.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Nucleo CrearNuevo() => new();

    protected override CrudEditorViewModelBase<Nucleo> CrearEditor(Nucleo item) =>
        new NucleoEditorViewModel(item, _nucleos);
}
