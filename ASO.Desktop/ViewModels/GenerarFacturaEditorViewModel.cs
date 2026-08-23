using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Selección de remesas a facturar. Muestra solo las entregadas al central que aún no están en
/// ninguna factura, con el total estimado actualizándose a medida que se marcan, para que quien
/// factura vea lo que va a cobrar antes de generar el borrador.
/// </summary>
public sealed class GenerarFacturaEditorViewModel : CrudEditorViewModelBase
{
    public GenerarFacturaEditorViewModel(IReadOnlyList<Remesa> facturables, decimal tarifaTotalPorTonelada)
    {
        TarifaTotalPorTonelada = tarifaTotalPorTonelada;

        Remesas = [.. facturables.Select(r =>
        {
            var seleccion = new SeleccionRemesa(r);
            seleccion.PropertyChanged += (_, _) => ActualizarTotales();
            return seleccion;
        })];

        ActualizarTotales();
    }

    public override string Titulo => "Generar factura al ingenio";
    public override string TextoAccion => "Generar la factura";
    public override double AnchoEditor => Ancho.Estandar;

    public ObservableCollection<SeleccionRemesa> Remesas { get; }

    /// <summary>Suma de las tarifas de corte, alza y transporte: lo que rinde cada tonelada.</summary>
    public decimal TarifaTotalPorTonelada { get; }

    private string _totalTexto = string.Empty;
    public string TotalTexto
    {
        get => _totalTexto;
        private set => SetProperty(ref _totalTexto, value);
    }

    public IReadOnlyList<Remesa> Seleccionadas =>
        [.. Remesas.Where(r => r.IsSeleccionada).Select(r => r.Remesa)];

    public bool HayFacturables => Remesas.Count > 0;

    private void ActualizarTotales()
    {
        var toneladas = Remesas.Where(r => r.IsSeleccionada).Sum(r => r.Remesa.PesoNetoT ?? 0m);
        var estimado = toneladas * TarifaTotalPorTonelada;

        TotalTexto = toneladas > 0
            ? $"{Seleccionadas.Count} remesa(s) · {toneladas:N2} t · estimado {estimado:N2}"
            : "Marque las remesas que entran en la factura.";
    }

    protected override bool Validar(out string? error)
    {
        if (!HayFacturables)
        {
            error = "No hay remesas recibidas pendientes de facturar.";
            return false;
        }

        if (Seleccionadas.Count == 0)
        {
            error = "Seleccione al menos una remesa.";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>Fila marcable de la lista de remesas facturables.</summary>
public sealed class SeleccionRemesa : ViewModelBase
{
    public SeleccionRemesa(Remesa remesa) => Remesa = remesa;

    public Remesa Remesa { get; }

    private bool _isSeleccionada;
    public bool IsSeleccionada
    {
        get => _isSeleccionada;
        set => SetProperty(ref _isSeleccionada, value);
    }

    public string Descripcion =>
        $"Nº {Remesa.Id} · {Remesa.FincaNombre} · {Remesa.PesoNetoT:N2} t";

    public string Detalle =>
        $"Recibida el {(Remesa.LlegadaCentral ?? Remesa.FechaConfirmacion):dd/MM/yyyy} · " +
        $"corte {Remesa.NucleoCorteCodigo} · alza {Remesa.NucleoAlzaEmpujeCodigo} · transporte {Remesa.NucleoTransporteCodigo}";
}
