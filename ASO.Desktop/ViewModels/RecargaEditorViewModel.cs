using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Captura de una recarga de cisterna. No hereda de la base genérica porque no edita una
/// entidad existente: solo recoge los datos con los que el servicio construye y aplica la
/// recarga (el tope de capacidad lo valida <see cref="CombustibleService"/>).
/// </summary>
public sealed class RecargaEditorViewModel : CrudEditorViewModelBase
{
    public RecargaEditorViewModel(ITanqueCombustibleDataSource tanques)
    {
        Tanques = tanques.GetAll().Where(t => t.Activo).ToList();
        TanqueSeleccionado = Tanques.FirstOrDefault();
    }

    public override string Titulo => "Registrar recarga de cisterna";
    public override double AnchoEditor => 460;

    public IReadOnlyList<TanqueCombustible> Tanques { get; }

    private TanqueCombustible? _tanqueSeleccionado;
    public TanqueCombustible? TanqueSeleccionado
    {
        get => _tanqueSeleccionado;
        set
        {
            if (SetProperty(ref _tanqueSeleccionado, value))
                OnPropertyChanged(nameof(EspacioTexto));
        }
    }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private string _litros = string.Empty;
    public string LitrosTexto
    {
        get => _litros;
        set => SetProperty(ref _litros, value);
    }

    private string _costo = string.Empty;
    public string CostoTexto
    {
        get => _costo;
        set => SetProperty(ref _costo, value);
    }

    private string _proveedorNombre = string.Empty;
    public string ProveedorNombre
    {
        get => _proveedorNombre;
        set => SetProperty(ref _proveedorNombre, value);
    }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    public string EspacioTexto => TanqueSeleccionado is { } t
        ? $"Contiene {t.ExistenciaTexto}; admite {t.CapacidadL - t.ExistenciaL:N0} L más."
        : "Seleccione la cisterna que se recarga.";

    protected override bool Validar(out string? error)
    {
        if (TanqueSeleccionado is null)
        {
            error = "Seleccione la cisterna que se recarga.";
            return false;
        }

        if (!decimal.TryParse(LitrosTexto, out var litros) || litros <= 0)
        {
            error = "Los litros recibidos deben ser un número mayor que cero.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(CostoTexto) && !decimal.TryParse(CostoTexto, out _))
        {
            error = "El costo total debe ser un número.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Recarga lista para pasársela a <see cref="CombustibleService.RegistrarRecarga"/>.</summary>
    public RecargaCombustible ObtenerRecarga(int creadoPorId) => new()
    {
        Fecha = Fecha,
        TanqueId = TanqueSeleccionado?.Id ?? 0,
        TanqueNombre = TanqueSeleccionado?.Nombre ?? string.Empty,
        Litros = decimal.TryParse(LitrosTexto, out var litros) ? litros : 0m,
        CostoTotal = decimal.TryParse(CostoTexto, out var costo) ? costo : null,
        ProveedorNombre = ProveedorNombre.Trim(),
        Notas = Notas.Trim(),
        CreadoPorId = creadoPorId,
        FechaCreacion = DateTime.Now
    };
}
