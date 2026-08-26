using System.Collections.Generic;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Ficha de un evento de la línea de tiempo: todo lo que se sabe del documento que lo originó.
///
/// Es la primera ventana de SOLO LECTURA del proyecto. Reusa <see cref="CrudEditorViewModelBase"/>
/// —y por tanto la ventana, la escala y el tema— pero no guarda nada: no valida, oculta Cancelar
/// y su botón de acción solo cierra.
///
/// Quién sabe qué datos tiene cada tipo de evento es <see cref="SeguimientoService.ObtenerDetalle"/>,
/// no este ViewModel: resolver el mantenimiento o la jornada de origen es una regla de dominio, y
/// aquí solo se pinta lo que devuelve.
/// </summary>
public sealed class DetalleEventoViewModel : CrudEditorViewModelBase
{
    private readonly EventoOperacion _evento;

    public DetalleEventoViewModel(EventoOperacion evento, IReadOnlyList<DatoEvento> datos)
    {
        _evento = evento;
        Datos = datos;
    }

    public override string Titulo => _evento.EtiquetaTipo;

    public override string TextoAccion => "Cerrar";

    public override bool MuestraCancelar => false;

    public override double AnchoEditor => Ancho.Estandar;

    public IReadOnlyList<DatoEvento> Datos { get; }

    /// <summary>Glifo del tipo, el mismo que marca el nodo en la línea de tiempo.</summary>
    public string Glifo => _evento.Glifo;

    protected override bool Validar(out string? error)
    {
        // No hay nada que validar: la ficha no escribe.
        error = null;
        return true;
    }
}
