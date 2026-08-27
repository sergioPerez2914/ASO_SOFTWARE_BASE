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
    private readonly IServicioDialogo _dialogos;
    private readonly HorarioService _horarios;
    private string _filtroRol = FiltroTodos;

    public PersonalCampoCrudViewModel(IPersonalCampoDataSource personal,
                                      IServicioDialogo dialogos,
                                      ISesionActual sesion,
                                      HorarioService horarios)
        : base(personal, dialogos, sesion)
    {
        _personal = personal;
        _dialogos = dialogos;
        _horarios = horarios;

        CambiarFiltroRolCommand = new RelayCommand<string>(filtro =>
        {
            _filtroRol = filtro;
            ItemsView.Refresh();
        });

        VerHistorialCommand = new RelayCommand(VerHistorial, () => SelectedItem is not null);
    }

    public ICommand CambiarFiltroRolCommand { get; }

    /// <summary>
    /// Las jornadas de esta persona, que aquí llevan además el frente contra el que se ficharon.
    /// Sin permiso propio, por lo mismo que en el padrón administrativo.
    /// </summary>
    public ICommand VerHistorialCommand { get; }

    protected override string ModuloPermiso => "PersonalCampo";

    protected override bool CoincideBusqueda(PersonalCampo item, string texto) =>
        item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cedula.Contains(texto, StringComparison.OrdinalIgnoreCase)
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
        new PersonalCampoEditorViewModel(item, _personal);

    private void VerHistorial()
    {
        if (SelectedItem is not { } persona)
            return;

        _dialogos.MostrarEditor(new HistorialTrabajoViewModel(
            TipoPersonal.Campo, persona.Id, persona.Nombre, persona.RolTexto, _horarios));
    }
}
