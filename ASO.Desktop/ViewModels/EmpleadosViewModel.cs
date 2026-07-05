using System;
using ASO.Desktop.Models;
using ASO.Desktop.Services;
using ASO.Desktop.BD;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Lógica de presentación del catálogo de Empleados: listado filtrable con alta/edición/baja.
/// </summary>
public class EmpleadosViewModel : CrudViewModelBase<Empleado, int>
{
    public EmpleadosViewModel() : this(new SqlEmpleadoDataSource()) { }

    public EmpleadosViewModel(IEmpleadoDataSource source) : base(source) { }

    protected override string ModuloPermiso => "Empleados";

    protected override bool CoincideBusqueda(Empleado item, string texto)
        => item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cedula.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cargo.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Empleado CrearNuevo() => new();

    protected override CrudEditorViewModelBase<Empleado> CrearEditor(Empleado item) => new EmpleadoEditorViewModel(item);
}
