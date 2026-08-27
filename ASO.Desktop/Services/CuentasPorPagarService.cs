using System;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de las cuentas por pagar. La factura de compra se registra tal como la emitió el
/// proveedor, así que aquí no hay generación: hay validación (que el documento no se cargue dos
/// veces) y dos transiciones, pagar y anular.
/// </summary>
public sealed class CuentasPorPagarService
{
    private readonly IFacturaProveedorDataSource _facturas;
    private readonly BancoService _banco;

    /// <summary>
    /// El <see cref="BancoService"/> es obligatorio: pagar la factura y anotar la salida en el
    /// libro son la misma operación (ver <see cref="RegistrarPago"/>).
    /// </summary>
    public CuentasPorPagarService(IFacturaProveedorDataSource facturas, BancoService banco)
    {
        _facturas = facturas;
        _banco = banco;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeEditar(FacturaProveedor f) => f.Estado == EstadoFacturaProveedor.Pendiente;

    public bool PuedeEliminar(FacturaProveedor f) => f.Estado == EstadoFacturaProveedor.Pendiente;

    public bool PuedeRegistrarPago(FacturaProveedor f) => f.Estado == EstadoFacturaProveedor.Pendiente;

    public bool PuedeAnular(FacturaProveedor f) =>
        f.Estado is EstadoFacturaProveedor.Pendiente or EstadoFacturaProveedor.Borrador;

    /// <summary>Solo la factura que generó automáticamente una recepción de mercancía nace en
    /// Borrador: le falta el Nº de documento y el vencimiento que trae el papel del proveedor.</summary>
    public bool PuedeCompletarBorrador(FacturaProveedor f) => f.Estado == EstadoFacturaProveedor.Borrador;

    /// <summary>
    /// Valida antes de guardar. El número de documento no puede repetirse dentro del mismo
    /// proveedor: es el control que evita pagar dos veces la misma factura.
    /// </summary>
    public bool Validar(FacturaProveedor factura, out string? error)
    {
        if (factura.ProveedorId == 0)
        {
            error = "Seleccione el proveedor que emitió la factura.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(factura.NumeroDocumento))
        {
            error = "Indique el número de la factura del proveedor.";
            return false;
        }

        if (factura.Monto <= 0)
        {
            error = "El monto de la factura debe ser mayor que cero.";
            return false;
        }

        if (factura.FechaVencimiento is not { } vencimiento || vencimiento.Date < factura.FechaEmision.Date)
        {
            error = "El vencimiento no puede ser anterior a la fecha de emisión.";
            return false;
        }

        var repetida = _facturas.GetByProveedor(factura.ProveedorId)
            .Where(f => f.Id != factura.Id && f.Estado != EstadoFacturaProveedor.Anulada)
            .Any(f => string.Equals(f.NumeroDocumento.Trim(), factura.NumeroDocumento.Trim(),
                                    StringComparison.OrdinalIgnoreCase));

        if (repetida)
        {
            error = $"El proveedor {factura.ProveedorNombre} ya tiene registrada la factura " +
                    $"Nº {factura.NumeroDocumento.Trim()}.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Da la factura por pagada y anota la salida en el libro de banco, en una sola operación. El
    /// asiento va primero porque es el que puede rechazar; ver
    /// <see cref="FacturaClienteService.RegistrarCobro"/>.
    /// </summary>
    public FacturaProveedor RegistrarPago(FacturaProveedor factura, AsientoBanco asiento, int usuarioId)
    {
        if (!PuedeRegistrarPago(factura))
            throw new InvalidOperationException("Solo se puede pagar una factura pendiente.");

        _banco.RegistrarPagoProveedor(factura, asiento, usuarioId);

        var copia = factura.Clonar();
        copia.Estado = EstadoFacturaProveedor.Pagada;
        copia.FechaPago = DateTime.Today;
        _facturas.Update(copia);
        return copia;
    }

    /// <summary>
    /// Completa el borrador que generó automáticamente una recepción de mercancía con lo único
    /// que el sistema no podía inventar — Nº de documento y vencimiento — y la deja Pendiente,
    /// igual que cualquier otra factura. Mismo patrón que <see cref="RegistrarPago"/>/<see cref="Anular"/>:
    /// el número no puede repetirse dentro del mismo proveedor.
    /// </summary>
    public FacturaProveedor CompletarBorrador(FacturaProveedor factura, string numeroDocumento, DateTime fechaVencimiento)
    {
        if (!PuedeCompletarBorrador(factura))
            throw new InvalidOperationException(
                "Solo se completan facturas generadas automáticamente desde una recepción de mercancía.");

        if (string.IsNullOrWhiteSpace(numeroDocumento))
            throw new InvalidOperationException("Indique el número de la factura del proveedor.");

        if (fechaVencimiento.Date < factura.FechaEmision.Date)
            throw new InvalidOperationException("El vencimiento no puede ser anterior a la fecha de emisión.");

        var repetida = _facturas.GetByProveedor(factura.ProveedorId)
            .Where(f => f.Id != factura.Id && f.Estado != EstadoFacturaProveedor.Anulada)
            .Any(f => string.Equals(f.NumeroDocumento.Trim(), numeroDocumento.Trim(),
                                    StringComparison.OrdinalIgnoreCase));

        if (repetida)
            throw new InvalidOperationException($"El proveedor {factura.ProveedorNombre} ya tiene registrada la factura " +
                                                $"Nº {numeroDocumento.Trim()}.");

        var copia = factura.Clonar();
        copia.NumeroDocumento = numeroDocumento.Trim();
        copia.FechaVencimiento = fechaVencimiento;
        copia.Estado = EstadoFacturaProveedor.Pendiente;
        _facturas.Update(copia);
        return copia;
    }

    public FacturaProveedor Anular(FacturaProveedor factura, string motivo)
    {
        if (!PuedeAnular(factura))
            throw new InvalidOperationException(
                factura.Estado == EstadoFacturaProveedor.Pagada
                    ? "Una factura ya pagada no se anula: registre la nota de crédito del proveedor."
                    : "Solo se puede anular una factura pendiente o generada automáticamente.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo de la anulación.");

        var copia = factura.Clonar();
        copia.Estado = EstadoFacturaProveedor.Anulada;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _facturas.Update(copia);
        return copia;
    }

    /// <summary>Deuda pendiente del centro.</summary>
    public decimal TotalPorPagar() =>
        _facturas.GetAll()
            .Where(f => f.Estado == EstadoFacturaProveedor.Pendiente)
            .Sum(f => f.Monto);

    public decimal TotalVencido() =>
        _facturas.GetAll()
            .Where(f => f.EstaVencida)
            .Sum(f => f.Monto);
}
