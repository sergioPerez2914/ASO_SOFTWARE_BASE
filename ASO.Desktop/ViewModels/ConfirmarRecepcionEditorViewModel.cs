using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Confirmar una recepción: decir quién recibió la mercancía y, si hay líneas de repuesto, dónde
/// quedan en el almacén — es lo único de una línea que se corrige acá, porque es lo único que
/// solo se sabe en este momento, con el repuesto físico enfrente. Todo lo demás (cantidad
/// recibida, presentación de diésel) sigue siendo responsabilidad exclusiva de "Editar"
/// (<see cref="RecepcionMercanciaEditorViewModel"/>). Si algo queda incompleto,
/// <c>ComprasService.ConfirmarRecepcion</c> lo rechaza señalando que se corrija con "Editar".
/// </summary>
public sealed class ConfirmarRecepcionEditorViewModel : CrudEditorViewModelBase<RecepcionMercancia>
{
    private readonly RecepcionMercancia _original;

    /// <summary>Posición de cada línea de <see cref="LineasRepuesto"/> dentro de
    /// <c>_original.Lineas</c> — las líneas no tienen identidad propia en C# (el Id es una shadow
    /// property de EF), así que es la única forma de devolverlas a su lugar en
    /// <see cref="ObtenerResultado"/>.</summary>
    private readonly List<int> _indicesLineasRepuesto;

    public ConfirmarRecepcionEditorViewModel(RecepcionMercancia original, IReadOnlyList<Empleado> empleados)
    {
        _original = original;
        Notas = original.Notas;
        Empleados = empleados;

        // Solo las que de verdad se van a aplicar al confirmar — mismo filtro "aAplicar" que usa
        // ComprasService.ConfirmarRecepcion. Pedir la ubicación de una línea en cero no tendría
        // sentido: no va a mover stock.
        var conIndice = original.Lineas
            .Select((linea, indice) => (linea, indice))
            .Where(x => x.linea.TipoInsumo == TipoInsumo.Repuesto && x.linea.CantidadRecibida > 0)
            .ToList();

        _indicesLineasRepuesto = conIndice.Select(x => x.indice).ToList();
        LineasRepuesto = conIndice.Select(x => x.linea.Clonar()).ToList();
    }

    public List<RecepcionMercanciaLinea> LineasRepuesto { get; }

    /// <summary>Para no mostrar una sección vacía en recepciones que son solo combustible.</summary>
    public bool HayLineasRepuesto => LineasRepuesto.Count > 0;

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

        for (var i = 0; i < _indicesLineasRepuesto.Count; i++)
            recepcion.Lineas[_indicesLineasRepuesto[i]].UbicacionArticulo = LineasRepuesto[i].UbicacionArticulo;

        return recepcion;
    }
}
