using System;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Padrón administrativo y de taller: los empleados de nómina del centro.
/// Sub-listado de Nómina · Empleados; el encabezado y el "Volver" los pone
/// <see cref="EmpleadosViewModel"/>, que lo aloja junto al padrón de campo.
/// </summary>
public sealed class EmpleadosAdminViewModel : CrudViewModelBase<Empleado, int>
{
    private const string FiltroTodos = "Todos";

    private readonly IEmpleadoDataSource _empleados;
    private readonly IServicioDialogo _dialogos;
    private readonly HorarioService _horarios;
    private string _filtroEstado = FiltroTodos;

    public EmpleadosAdminViewModel(IEmpleadoDataSource empleados,
                                   IServicioDialogo dialogos,
                                   ISesionActual sesion,
                                   HorarioService horarios)
        : base(empleados, dialogos, sesion)
    {
        _empleados = empleados;
        _dialogos = dialogos;
        _horarios = horarios;

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        VerHistorialCommand = new RelayCommand(VerHistorial, () => SelectedItem is not null);
    }

    public ICommand CambiarFiltroEstadoCommand { get; }

    /// <summary>
    /// Abre las jornadas de la persona seleccionada. No lleva permiso propio: es una ficha de
    /// solo lectura dentro de una pantalla que ya gobierna <c>Ver.Empleados</c>, igual que la
    /// ficha de un evento en Seguimiento.
    /// </summary>
    public ICommand VerHistorialCommand { get; }

    protected override string ModuloPermiso => "Empleados";

    protected override bool CoincideBusqueda(Empleado item, string texto) =>
        item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cedula.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cargo.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(Empleado item) => _filtroEstado switch
    {
        "Activos" => item.Activo,
        "Inactivos" => !item.Activo,
        _ => true
    };

    protected override Empleado CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<Empleado> CrearEditor(Empleado item) =>
        new EmpleadoEditorViewModel(item, _empleados);

    private void VerHistorial()
    {
        if (SelectedItem is not { } empleado)
            return;

        _dialogos.MostrarEditor(new HistorialTrabajoViewModel(
            TipoPersonal.Administrativo, empleado.Id, empleado.Nombre, empleado.Cargo, _horarios));
    }
}
