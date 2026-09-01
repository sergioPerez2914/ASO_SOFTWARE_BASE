using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Confirmar una recepción: decir quién recibió la mercancía. No corrige líneas — eso es
/// responsabilidad exclusiva de "Editar" (<see cref="RecepcionMercanciaEditorViewModel"/>), que
/// sigue existiendo aparte para quien necesite corregir la cantidad recibida o, en Diésel, su
/// presentación antes de confirmar. Si algo queda incompleto,
/// <c>ComprasService.ConfirmarRecepcion</c> lo rechaza señalando que se corrija con "Editar".
/// </summary>
public sealed class ConfirmarRecepcionEditorViewModel : CrudEditorViewModelBase<RecepcionMercancia>
{
    private readonly RecepcionMercancia _original;

    public ConfirmarRecepcionEditorViewModel(RecepcionMercancia original, IReadOnlyList<Empleado> empleados)
    {
        _original = original;
        Notas = original.Notas;
        Empleados = empleados;
    }

    public override string Titulo => $"Confirmar recepción Nº {_original.Id}";
    public override string TextoAccion => "Confirmar";

    public string ResumenProveedor => $"{_original.ProveedorNombre} · orden de compra Nº {_original.OrdenCompraId}";

    public IReadOnlyList<Empleado> Empleados { get; }

    /// <summary>Sin personal administrativo activo no hay a quién asignarle la recepción. La
    /// vista lo dice en vez de mostrar una lista vacía que no explica por qué no deja continuar.</summary>
    public bool HayEmpleados => Empleados.Count > 0;

    public bool NoHayEmpleados => !HayEmpleados;

    public string AvisoSinEmpleados =>
        "No hay personal administrativo activo. Dé de alta uno en Nómina · Empleados.";

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    private Empleado? _responsableSeleccionado;
    public Empleado? ResponsableSeleccionado
    {
        get => _responsableSeleccionado;
        set => SetProperty(ref _responsableSeleccionado, value);
    }

    /// <summary>Lo que se le pasa al servicio de dominio, ya validado.</summary>
    public string ResponsableNombre => ResponsableSeleccionado?.Nombre ?? string.Empty;

    protected override bool Validar(out string? error)
    {
        if (ResponsableSeleccionado is null)
        {
            error = HayEmpleados
                ? "Seleccione quién recibió la mercancía."
                : AvisoSinEmpleados;
            return false;
        }

        error = null;
        return true;
    }

    public override RecepcionMercancia ObtenerResultado()
    {
        var recepcion = _original.Clonar();
        recepcion.Notas = Notas.Trim();
        return recepcion;
    }
}
