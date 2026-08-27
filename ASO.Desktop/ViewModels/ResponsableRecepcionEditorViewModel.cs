using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// "¿Quién recibió la mercancía?" — se pregunta al confirmar la recepción, no al editarla: es la
/// firma de quien tuvo la carga enfrente, no un dato de las líneas que se corrige en borrador.
/// Sustituye al campo de texto libre que había antes en <see cref="RecepcionMercanciaEditorViewModel"/>,
/// que dejaba escribir cualquier nombre sin relación con el padrón de personal — mismo criterio
/// que <see cref="AsientoBancoEditorViewModel"/> reemplazando al <c>Confirmar</c> de sí/no.
///
/// Lista contra el padrón administrativo (<c>Empleado</c>), no <c>PersonalCampo</c>: quien recibe
/// mercancía en almacén es personal de nómina/taller, no quien firma una remesa en el campo.
/// </summary>
public sealed class ResponsableRecepcionEditorViewModel : CrudEditorViewModelBase
{
    private readonly string _titulo;

    public ResponsableRecepcionEditorViewModel(string titulo, string descripcion, IReadOnlyList<Empleado> empleados)
    {
        _titulo = titulo;
        Descripcion = descripcion;
        Empleados = empleados;
    }

    public override string Titulo => _titulo;

    public override string TextoAccion => "Confirmar recepción";

    public string Descripcion { get; }

    public IReadOnlyList<Empleado> Empleados { get; }

    /// <summary>Sin personal administrativo activo no hay a quién asignarle la recepción. La
    /// vista lo dice en vez de mostrar una lista vacía que no explica por qué no deja continuar.</summary>
    public bool HayEmpleados => Empleados.Count > 0;

    public bool NoHayEmpleados => !HayEmpleados;

    public string AvisoSinEmpleados =>
        "No hay personal administrativo activo. Dé de alta uno en Nómina · Empleados.";

    private Empleado? _empleadoSeleccionado;
    public Empleado? EmpleadoSeleccionado
    {
        get => _empleadoSeleccionado;
        set => SetProperty(ref _empleadoSeleccionado, value);
    }

    /// <summary>Lo que se le pasa al servicio de dominio, ya validado.</summary>
    public string ResponsableNombre => EmpleadoSeleccionado?.Nombre ?? string.Empty;

    protected override bool Validar(out string? error)
    {
        if (EmpleadoSeleccionado is null)
        {
            error = HayEmpleados
                ? "Seleccione quién recibió la mercancía."
                : AvisoSinEmpleados;
            return false;
        }

        error = null;
        return true;
    }
}
