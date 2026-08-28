using System;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de los datos de una zafra (código y vigencia). Abrir/cerrar/reabrir NO pasan por
/// aquí: son transiciones de <see cref="ZafraService"/> con su propio comando en
/// <see cref="ZafraCrudViewModel"/>, no un formulario que se guarda.
/// </summary>
public sealed class ZafraEditorViewModel : CrudEditorViewModelBase<Zafra>
{
    private readonly Zafra _original;

    public ZafraEditorViewModel(Zafra original)
    {
        _original = original;

        Codigo = original.Codigo;
        FechaInicio = original.FechaInicio == default ? DateTime.Today : original.FechaInicio;
        FechaFinPrevista = original.FechaFinPrevista;
        Notas = original.Notas;
    }

    public override string Titulo => _original.Id == 0 ? "Nueva zafra" : $"Editar zafra {_original.Codigo}";
    public override double AnchoEditor => Ancho.Estandar;

    private string _codigo = string.Empty;
    public string Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    private DateTime _fechaInicio = DateTime.Today;
    public DateTime FechaInicio
    {
        get => _fechaInicio;
        set => SetProperty(ref _fechaInicio, value);
    }

    private DateTime? _fechaFinPrevista;
    public DateTime? FechaFinPrevista
    {
        get => _fechaFinPrevista;
        set => SetProperty(ref _fechaFinPrevista, value);
    }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    protected override bool Validar(out string? error) => ZafraService.Validar(Construir(), out error);

    public override Zafra ObtenerResultado() => Construir();

    private Zafra Construir()
    {
        var zafra = _original.Clonar();
        zafra.Codigo = Codigo.Trim();
        zafra.FechaInicio = FechaInicio;
        zafra.FechaFinPrevista = FechaFinPrevista;
        zafra.Notas = Notas.Trim();
        return zafra;
    }
}
