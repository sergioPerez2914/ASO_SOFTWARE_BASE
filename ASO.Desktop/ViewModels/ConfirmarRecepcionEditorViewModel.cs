using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Confirmar una recepción en un solo paso: corregir la cantidad realmente recibida y la
/// presentación de cada línea de diésel, y decir quién recibió la mercancía — todo en la misma
/// ventana que dispara la confirmación.
///
/// Antes esto eran dos pasos obligatorios en dos ventanas distintas: "Editar" para corregir las
/// líneas (donde vivía la presentación) y, aparte, "Confirmar" para decir el responsable — y si
/// la presentación no se había cargado en el primer paso, el segundo la rechazaba por datos
/// incompletos sin dar dónde corregirla. Fusiona <see cref="RecepcionMercanciaEditorViewModel"/>
/// (líneas) y el extinto <c>ResponsableRecepcionEditorViewModel</c> (responsable) en una sola
/// ventana, reservada a la acción "Confirmar". "Editar" (guardar el borrador sin confirmar) sigue
/// existiendo aparte y sigue usando el editor de solo líneas, para quien quiera dejar corregido
/// un dato sin confirmar todavía.
/// </summary>
public sealed class ConfirmarRecepcionEditorViewModel : CrudEditorViewModelBase<RecepcionMercancia>
{
    private readonly RecepcionMercancia _original;

    public ConfirmarRecepcionEditorViewModel(RecepcionMercancia original, IReadOnlyList<Empleado> empleados)
    {
        _original = original;
        Notas = original.Notas;
        Lineas = new ObservableCollection<RecepcionMercanciaLinea>(original.Lineas.Select(l => l.Clonar()));
        Empleados = empleados;
    }

    public override string Titulo => $"Confirmar recepción Nº {_original.Id}";
    public override double AnchoEditor => Ancho.Amplio;
    public override string TextoAccion => "Confirmar";

    public string ResumenProveedor => $"{_original.ProveedorNombre} · orden de compra Nº {_original.OrdenCompraId}";

    public IReadOnlyList<string> PresentacionesCombustible => RecepcionMercanciaLinea.PresentacionesDiesel;

    public ObservableCollection<RecepcionMercanciaLinea> Lineas { get; }

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
        if (Lineas.Any(l => l.CantidadRecibida < 0))
        {
            error = "La cantidad recibida no puede ser negativa.";
            return false;
        }

        if (Lineas.All(l => l.CantidadRecibida <= 0))
        {
            error = "Indique al menos una cantidad recibida mayor que cero.";
            return false;
        }

        if (Lineas.Any(l => l.EsDiesel && l.CantidadRecibida > 0 && string.IsNullOrWhiteSpace(l.Presentacion)))
        {
            error = "Seleccione la presentación de cada línea de diésel recibida.";
            return false;
        }

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
        recepcion.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return recepcion;
    }
}
