using System;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Padrón de campo: operadores, tractoristas, choferes y remeseros que firman la remesa.
/// Sub-listado de Nómina · Empleados (ver <see cref="EmpleadosViewModel"/>).
/// </summary>
public sealed class PersonalCampoCrudViewModel : CrudViewModelBase<PersonalCampo, int>
{
    private const string FiltroTodos = "Todos";

    private readonly IPersonalCampoDataSource _personal;
    private readonly INucleoDataSource _nucleos;
    private string _filtroRol = FiltroTodos;

    public PersonalCampoCrudViewModel(IPersonalCampoDataSource personal,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion)
        : base(personal, dialogos, sesion)
    {
        _personal = personal;
        _nucleos = DataSourceFactory.CrearNucleos();

        CambiarFiltroRolCommand = new RelayCommand<string>(filtro =>
        {
            _filtroRol = filtro;
            ItemsView.Refresh();
        });
    }

    public ICommand CambiarFiltroRolCommand { get; }

    protected override string ModuloPermiso => "Empleados";

    protected override bool CoincideBusqueda(PersonalCampo item, string texto) =>
        item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cedula.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.NucleoCodigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.RolTexto.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(PersonalCampo item) => _filtroRol switch
    {
        "Operador" => item.Rol == RolCampo.Operador,
        "Tractorista" => item.Rol == RolCampo.Tractorista,
        "Chofer" => item.Rol == RolCampo.Chofer,
        "Remesero" => item.Rol == RolCampo.Remesero,
        _ => true
    };

    protected override PersonalCampo CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<PersonalCampo> CrearEditor(PersonalCampo item) =>
        new PersonalCampoEditorViewModel(item, _personal, _nucleos);
}
