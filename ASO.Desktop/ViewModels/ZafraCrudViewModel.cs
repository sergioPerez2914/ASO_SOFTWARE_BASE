using System;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Catálogo de temporadas de cosecha, pestaña "Zafra" de Administración. "Nueva zafra" no usa el
/// alta genérica de <see cref="CrudViewModelBase{T, TId}"/> (que solo agrega, sin más efectos):
/// abrir una zafra tiene que fijarla como activa y rechazar si ya hay otra abierta, así que pasa
/// por <see cref="ZafraService.Abrir"/> con su propio comando — mismo criterio que
/// <c>RecepcionesCrudViewModel.CrearNuevo</c>, que también deshabilita el alta genérica porque la
/// suya nace de una acción con efectos, no de un formulario suelto.
/// </summary>
public sealed class ZafraCrudViewModel : CrudViewModelBase<Zafra, int>
{
    private readonly ZafraService _servicio;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesion;

    public ZafraCrudViewModel(IZafraDataSource zafras, IServicioDialogo dialogos, ISesionActual sesion)
        : base(zafras, dialogos, sesion)
    {
        _servicio = new ZafraService(zafras, sesion);
        _dialogos = dialogos;
        _sesion = sesion;

        AbrirCommand = new RelayCommand(Abrir, () => _sesion.Puede(Permisos.Zafra.Crear));

        CerrarCommand = new RelayCommand(Cerrar,
            () => SelectedItem is { } z && _servicio.PuedeCerrar(z) && _sesion.Puede(Permisos.Zafra.Cerrar));

        ReabrirCommand = new RelayCommand(Reabrir,
            () => SelectedItem is { } z && _servicio.PuedeReabrir(z) && _sesion.Puede(Permisos.Zafra.Reabrir));
    }

    protected override string ModuloPermiso => "Zafra";

    public ICommand AbrirCommand { get; }
    public ICommand CerrarCommand { get; }
    public ICommand ReabrirCommand { get; }

    protected override bool CoincideBusqueda(Zafra item, string texto) =>
        item.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase);

    /// <summary>No se ofrece: "Nueva zafra" es <see cref="AbrirCommand"/>, no el alta genérica.</summary>
    protected override Zafra CrearNuevo() =>
        throw new NotSupportedException("Una zafra se abre con \"Nueva zafra\", no con el alta genérica.");

    protected override CrudEditorViewModelBase<Zafra> CrearEditor(Zafra item) => new ZafraEditorViewModel(item);

    private void Abrir()
    {
        var editor = new ZafraEditorViewModel(new Zafra { FechaInicio = DateTime.Today });
        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var nueva = _servicio.Abrir(editor.ObtenerResultado());
            SeleccionarTrasRecargar(nueva.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo abrir la zafra", ex.Message);
        }
    }

    private void Cerrar()
    {
        if (SelectedItem is not { } zafra)
            return;

        var editor = new MotivoEditorViewModel(
            $"Cerrar zafra {zafra.Codigo}",
            $"{zafra.Codigo} — {zafra.VigenciaTexto}",
            "Motivo del cierre",
            "Indique el motivo del cierre.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        try
        {
            var cerrada = _servicio.Cerrar(zafra, editor.Motivo);
            SeleccionarTrasRecargar(cerrada.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo cerrar la zafra", ex.Message);
        }
    }

    private void Reabrir()
    {
        if (SelectedItem is not { } zafra)
            return;

        if (!_dialogos.Confirmar("Reabrir zafra",
                $"¿Reabrir la zafra {zafra.Codigo}? Vuelve a quedar como la zafra activa."))
            return;

        try
        {
            var reabierta = _servicio.Reabrir(zafra);
            SeleccionarTrasRecargar(reabierta.Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo reabrir la zafra", ex.Message);
        }
    }
}
