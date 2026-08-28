using System;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Captura de una recarga del stock de diésel. No hereda de la base genérica porque no edita
/// una entidad existente: solo recoge los datos con los que el servicio construye y aplica la
/// recarga (el tope de capacidad, si lo hubiera, lo valida <see cref="CombustibleService"/>).
///
/// No hay stock que elegir: la empresa no tiene cisternas, así que la recarga siempre entra al
/// único stock general "Diesel", que este editor resuelve solo (lo busca o lo crea) — mismo
/// criterio que <see cref="ValeCombustibleEditorViewModel"/>.
/// </summary>
public sealed class RecargaEditorViewModel : CrudEditorViewModelBase
{
    public RecargaEditorViewModel(IStockCombustibleDataSource stockCombustible, IServicioDialogo dialogos, ISesionActual sesion)
    {
        Stock = stockCombustible.GetAll()
            .FirstOrDefault(s => s.Nombre.Equals("Diesel", StringComparison.OrdinalIgnoreCase));

        Stock ??= stockCombustible.Add(new StockCombustible
        {
            Nombre = "Diesel",
            CapacidadL = 0,
            ExistenciaL = 0,
            Activo = true
        });
    }

    public override string Titulo => "Registrar recarga de diésel";
    public override double AnchoEditor => Ancho.Estandar;

    /// <summary>El único stock de destino posible; no hay nada que seleccionar.</summary>
    public StockCombustible Stock { get; }

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

    public string EspacioTexto => Stock.CapacidadL > 0
        ? $"Contiene {Stock.ExistenciaTexto}; admite {Stock.CapacidadL - Stock.ExistenciaL:N0} L más."
        : $"Contiene {Stock.ExistenciaL:N0} L; sin tope de capacidad fijo.";

    protected override bool Validar(out string? error)
    {
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
        StockCombustibleId = Stock.Id,
        StockCombustibleNombre = Stock.Nombre,
        Litros = decimal.TryParse(LitrosTexto, out var litros) ? litros : 0m,
        CostoTotal = decimal.TryParse(CostoTexto, out var costo) ? costo : null,
        ProveedorNombre = ProveedorNombre.Trim(),
        Notas = Notas.Trim(),
        CreadoPorId = creadoPorId,
        FechaCreacion = DateTime.Now
    };
}
