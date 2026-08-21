using System;
using System.Collections.Generic;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de una tarifa. La validación de fondo (monto, vigencia, solapes) la hace
/// <see cref="TarifaService"/>: el editor solo se la pide y muestra el mensaje que devuelva.
/// </summary>
public sealed class TarifaEditorViewModel : CrudEditorViewModelBase<Tarifa>
{
    private readonly Tarifa _original;
    private readonly TarifaService _servicio;

    public TarifaEditorViewModel(Tarifa original, TarifaService servicio)
    {
        _original = original;
        _servicio = servicio;

        Concepto = original.Concepto;
        Servicio = original.Servicio;
        Ambito = original.Ambito;
        Unidad = original.Unidad;
        Monto = original.MontoPorUnidad == 0 ? string.Empty : original.MontoPorUnidad.ToString("0.##");
        VigenteDesde = original.VigenteDesde;
        VigenteHasta = original.VigenteHasta;
        Activa = original.Activa;
        Notas = original.Notas;
    }

    public override string Titulo => _original.Id == 0 ? "Nueva tarifa" : $"Editar tarifa Nº {_original.Id}";
    public override double AnchoEditor => 480;

    public IReadOnlyList<ServicioZafra> Servicios { get; } = Enum.GetValues<ServicioZafra>();
    public IReadOnlyList<AmbitoTarifa> Ambitos { get; } = Enum.GetValues<AmbitoTarifa>();
    public IReadOnlyList<UnidadTarifa> Unidades { get; } = Enum.GetValues<UnidadTarifa>();

    private string _concepto = string.Empty;
    public string Concepto
    {
        get => _concepto;
        set => SetProperty(ref _concepto, value);
    }

    private ServicioZafra _servicioZafra;
    public ServicioZafra Servicio
    {
        get => _servicioZafra;
        set => SetProperty(ref _servicioZafra, value);
    }

    private AmbitoTarifa _ambito;
    public AmbitoTarifa Ambito
    {
        get => _ambito;
        set => SetProperty(ref _ambito, value);
    }

    private UnidadTarifa _unidad;
    public UnidadTarifa Unidad
    {
        get => _unidad;
        set => SetProperty(ref _unidad, value);
    }

    private string _monto = string.Empty;
    public string Monto
    {
        get => _monto;
        set => SetProperty(ref _monto, value);
    }

    private DateTime _vigenteDesde = DateTime.Today;
    public DateTime VigenteDesde
    {
        get => _vigenteDesde;
        set => SetProperty(ref _vigenteDesde, value);
    }

    private DateTime? _vigenteHasta;
    public DateTime? VigenteHasta
    {
        get => _vigenteHasta;
        set => SetProperty(ref _vigenteHasta, value);
    }

    private bool _activa = true;
    public bool Activa
    {
        get => _activa;
        set => SetProperty(ref _activa, value);
    }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    protected override bool Validar(out string? error)
    {
        if (!decimal.TryParse(Monto, out var monto))
        {
            error = "El monto por unidad debe ser un número.";
            return false;
        }

        var candidata = Construir(monto);
        return _servicio.Validar(candidata, out error);
    }

    public override Tarifa ObtenerResultado() =>
        Construir(decimal.TryParse(Monto, out var monto) ? monto : 0m);

    private Tarifa Construir(decimal monto)
    {
        var tarifa = _original.Clonar();
        tarifa.Concepto = Concepto.Trim();
        tarifa.Servicio = Servicio;
        tarifa.Ambito = Ambito;
        tarifa.Unidad = Unidad;
        tarifa.MontoPorUnidad = monto;
        tarifa.VigenteDesde = VigenteDesde;
        tarifa.VigenteHasta = VigenteHasta;
        tarifa.Activa = Activa;
        tarifa.Notas = Notas.Trim();
        return tarifa;
    }
}
