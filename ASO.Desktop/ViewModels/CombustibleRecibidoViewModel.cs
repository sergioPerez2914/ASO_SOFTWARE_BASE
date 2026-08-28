using System;
using System.Collections.ObjectModel;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Pestaña "Combustible" de Inventario · Combustible: cuánto diésel tiene la empresa y el
/// historial de litros recibidos por Compras, con su presentación — mismo espíritu que
/// "Lubricantes" (envases sin cisterna común), pero de solo lectura: no hay nada que dar de alta
/// aquí, todo nace de una recepción de mercancía confirmada
/// (<see cref="Services.ComprasService.ConfirmarRecepcion"/>).
///
/// La tarjeta usa el mismo <see cref="StockCombustible"/> "Diesel" del que despachan los vales,
/// así que el total ya refleja lo consumido — no es una simple suma de compras. No lleva barra de
/// capacidad ni porcentaje: ese stock no representa una cisterna con tope, es solo un acumulador
/// en litros (ver <see cref="StockCombustible.TieneCapacidadFija"/>).
/// </summary>
public sealed class CombustibleRecibidoViewModel : ViewModelBase
{
    private readonly IStockCombustibleDataSource _stockCombustible;
    private readonly IRecepcionMercanciaDataSource _recepciones;

    public CombustibleRecibidoViewModel(IStockCombustibleDataSource stockCombustible,
                                        IRecepcionMercanciaDataSource recepciones)
    {
        _stockCombustible = stockCombustible;
        _recepciones = recepciones;

        Recibos = new ObservableCollection<RenglonCombustibleRecibido>();
        Recargar();
    }

    public ObservableCollection<RenglonCombustibleRecibido> Recibos { get; }

    private decimal _existenciaTotalL;
    public decimal ExistenciaTotalL
    {
        get => _existenciaTotalL;
        private set => SetProperty(ref _existenciaTotalL, value);
    }

    public string ExistenciaTotalTexto => $"{ExistenciaTotalL:N0} L";

    public void Recargar()
    {
        ExistenciaTotalL = _stockCombustible.GetAll()
            .Where(s => s.Nombre.Equals("Diesel", StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.ExistenciaL);
        OnPropertyChanged(nameof(ExistenciaTotalTexto));

        Recibos.Clear();
        var renglones = _recepciones.GetAll()
            .Where(r => r.Estado == EstadoRecepcionMercancia.Confirmada)
            .SelectMany(r => r.Lineas
                .Where(l => l.EsDiesel && l.CantidadRecibida > 0)
                .Select(l => new RenglonCombustibleRecibido(
                    r.FechaConfirmacion ?? r.Fecha,
                    l.Presentacion ?? "—",
                    l.CantidadRecibida,
                    r.ProveedorNombre,
                    r.OrdenCompraId)))
            .OrderByDescending(x => x.Fecha);

        foreach (var renglon in renglones)
            Recibos.Add(renglon);
    }
}

/// <summary>Fila de solo lectura para la tabla de recepciones de diésel.</summary>
public sealed record RenglonCombustibleRecibido(
    DateTime Fecha, string Presentacion, decimal Litros, string ProveedorNombre, int OrdenCompraId)
{
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");

    public string LitrosTexto => $"{Litros:N2} L";

    public string OrdenTexto => $"OC Nº {OrdenCompraId}";
}
