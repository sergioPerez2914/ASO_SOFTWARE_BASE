using System;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Operaciones · Fincas: el catálogo de fincas del núcleo, con sus lotes y tablones. Es lo que
/// alimenta la cascada finca → lote → tablón del Registro de Operación.
///
/// Un núcleo tiene muchas fincas, y una instalación atiende a un solo núcleo: por eso la
/// pantalla es un padrón y no un conmutador de dos catálogos como lo era Fincas y Núcleos.
/// </summary>
public sealed class FincasViewModel : PantallaCrudViewModel<Finca, int>
{
    private readonly IFincaDataSource _fincas;

    public FincasViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearFincas(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private FincasViewModel(Modulo modulo,
                            Submodulo submodulo,
                            IFincaDataSource fincas,
                            IServicioDialogo dialogos,
                            ISesionActual sesion)
        : base(modulo, submodulo, fincas, dialogos, sesion)
    {
        _fincas = fincas;
    }

    protected override string ModuloPermiso => "Fincas";

    protected override bool CoincideBusqueda(Finca item, string texto) =>
        item.CodigoCam.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Dueno.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Finca CrearNuevo() => new();

    protected override CrudEditorViewModelBase<Finca> CrearEditor(Finca item) =>
        new FincaEditorViewModel(item, _fincas);
}
