using System;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Completa el borrador de factura que generó automáticamente una recepción de mercancía
/// (ver <see cref="Services.ComprasService.ConfirmarRecepcion"/>): pide lo único que el sistema
/// no podía inventar — el Nº de documento y el vencimiento que trae el papel del proveedor — y
/// deja el resto (proveedor, líneas, monto) de solo lectura, tal como lo copió de la orden de
/// compra. La validación de fondo (duplicado, fecha) la hace
/// <see cref="Services.CuentasPorPagarService.CompletarBorrador"/>.
/// </summary>
public sealed class CompletarFacturaProveedorEditorViewModel : CrudEditorViewModelBase
{
    public CompletarFacturaProveedorEditorViewModel(FacturaProveedor factura)
    {
        Resumen = $"{factura.ProveedorNombre} — {factura.Descripcion} — {factura.MontoTexto}";
        FechaVencimiento = DateTime.Today.AddDays(30);
    }

    public override string Titulo => "Completar factura de proveedor";

    public override double AnchoEditor => Ancho.Compacto;

    public override string TextoAccion => "Completar";

    public string Resumen { get; }

    private string _numeroDocumento = string.Empty;
    public string NumeroDocumento
    {
        get => _numeroDocumento;
        set => SetProperty(ref _numeroDocumento, value);
    }

    private DateTime _fechaVencimiento;
    public DateTime FechaVencimiento
    {
        get => _fechaVencimiento;
        set => SetProperty(ref _fechaVencimiento, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(NumeroDocumento))
        {
            error = "Indique el número de la factura del proveedor.";
            return false;
        }

        error = null;
        return true;
    }
}
