using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de negocio del flujo de compras: <c>Requisicion</c> (Borrador → Enviada → Atendida,
/// rama Anulada) y <c>OrdenCompra</c> (Borrador → Aprobada → Cerrada, rama Anulada). Mismo
/// contrato que <see cref="RemesaService"/>: los <c>PuedeX</c> alimentan el <c>CanExecute</c>
/// (cortesía visual) y las transiciones vuelven a validar y lanzan si el estado no lo permite
/// (defensa en profundidad).
///
/// La Recepción de mercancía y el cotejo a tres vías con Cuentas por Pagar (que cierra la orden)
/// llegan en una fase posterior; este servicio hoy solo cubre el papeleo hasta la aprobación del
/// gasto, sin tocar todavía ningún inventario real.
/// </summary>
public sealed class ComprasService
{
    private readonly IRequisicionDataSource _requisiciones;
    private readonly ICotizacionProveedorDataSource _cotizaciones;
    private readonly IOrdenCompraDataSource _ordenesCompra;

    public ComprasService(IRequisicionDataSource requisiciones,
                          ICotizacionProveedorDataSource cotizaciones,
                          IOrdenCompraDataSource ordenesCompra)
    {
        _requisiciones = requisiciones;
        _cotizaciones = cotizaciones;
        _ordenesCompra = ordenesCompra;
    }

    // --- Requisición: reglas de transición ---

    public bool PuedeEditarRequisicion(Requisicion r) => r.Estado == EstadoRequisicion.Borrador;

    public bool PuedeEliminarRequisicion(Requisicion r) => r.Estado == EstadoRequisicion.Borrador;

    public bool PuedeEnviarRequisicion(Requisicion r) => r.Estado == EstadoRequisicion.Borrador;

    public bool PuedeAnularRequisicion(Requisicion r)
        => r.Estado is EstadoRequisicion.Borrador or EstadoRequisicion.Enviada;

    /// <summary>Solo una requisición Enviada puede alimentar una orden de compra.</summary>
    public bool PuedeArmarOrdenCompra(Requisicion r) => r.Estado == EstadoRequisicion.Enviada;

    public static bool RequisicionEstaCompleta(Requisicion requisicion, out string? faltantes)
    {
        var pendientes = new List<string>();

        if (requisicion.Lineas.Count == 0)
            pendientes.Add("al menos una línea");

        if (requisicion.Lineas.Any(l => l.Cantidad <= 0))
            pendientes.Add("la cantidad de cada línea");

        if (requisicion.Lineas.Any(l => l.TipoInsumo == TipoInsumo.Combustible && l.TipoCombustibleSolicitado is null))
            pendientes.Add("el tipo de combustible de cada línea de combustible");

        if (requisicion.Lineas.Any(l => l.TipoInsumo == TipoInsumo.Combustible
                                         && l.TipoCombustibleSolicitado == TipoCombustible.Lubricante
                                         && string.IsNullOrWhiteSpace(l.TipoLubricante)))
            pendientes.Add("el grado del lubricante de cada línea de lubricante");

        if (requisicion.Lineas.Any(l => l.TipoInsumo == TipoInsumo.Repuesto && string.IsNullOrWhiteSpace(l.ArticuloCodigo)))
            pendientes.Add("el artículo de cada línea de repuesto");

        faltantes = pendientes.Count == 0 ? null : string.Join(", ", pendientes);
        return pendientes.Count == 0;
    }

    // --- Requisición: transiciones ---

    /// <summary>Envía la requisición: a partir de aquí es inmutable y puede armar una orden de compra.</summary>
    public Requisicion EnviarRequisicion(Requisicion requisicion)
    {
        if (!PuedeEnviarRequisicion(requisicion))
            throw new InvalidOperationException(
                $"No se puede enviar una requisición en estado {requisicion.EstadoTexto}.");

        if (!RequisicionEstaCompleta(requisicion, out var faltantes))
            throw new InvalidOperationException($"La requisición está incompleta. Faltan: {faltantes}.");

        var actualizada = requisicion.Clonar();
        actualizada.Estado = EstadoRequisicion.Enviada;
        actualizada.FechaEnvio = DateTime.Now;
        _requisiciones.Update(actualizada);
        return actualizada;
    }

    public Requisicion AnularRequisicion(Requisicion requisicion, string motivo)
    {
        if (!PuedeAnularRequisicion(requisicion))
            throw new InvalidOperationException(
                $"No se puede anular una requisición en estado {requisicion.EstadoTexto}.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Debe indicar el motivo de la anulación.");

        var actualizada = requisicion.Clonar();
        actualizada.Estado = EstadoRequisicion.Anulada;
        actualizada.MotivoAnulacion = motivo.Trim();
        actualizada.FechaAnulacion = DateTime.Now;
        _requisiciones.Update(actualizada);
        return actualizada;
    }

    // --- Cotización ---

    /// <summary>Cotizaciones capturadas para comparar precios antes de armar la orden de compra.</summary>
    public IEnumerable<CotizacionProveedor> CotizacionesDe(int requisicionId) =>
        _cotizaciones.GetByRequisicion(requisicionId);

    // --- Orden de compra: reglas de transición ---

    public bool PuedeEditarOrdenCompra(OrdenCompra oc) => oc.Estado == EstadoOrdenCompra.Borrador;

    public bool PuedeEliminarOrdenCompra(OrdenCompra oc) => oc.Estado == EstadoOrdenCompra.Borrador;

    public bool PuedeAprobarOrdenCompra(OrdenCompra oc) => oc.Estado == EstadoOrdenCompra.Borrador;

    public bool PuedeAnularOrdenCompra(OrdenCompra oc)
        => oc.Estado is EstadoOrdenCompra.Borrador or EstadoOrdenCompra.Aprobada;

    public static bool OrdenCompraEstaCompleta(OrdenCompra orden, out string? faltantes)
    {
        var pendientes = new List<string>();

        if (orden.ProveedorId == 0)
            pendientes.Add("el proveedor");

        if (orden.Lineas.Count == 0)
            pendientes.Add("al menos una línea");

        if (orden.Lineas.Any(l => l.PrecioUnitario <= 0))
            pendientes.Add("el precio unitario de cada línea");

        faltantes = pendientes.Count == 0 ? null : string.Join(", ", pendientes);
        return pendientes.Count == 0;
    }

    // --- Orden de compra: transiciones ---

    /// <summary>
    /// Arma la orden de compra copiando las líneas de la requisición (con precio en cero, a
    /// completar antes de aprobar) y marca la requisición como Atendida: una requisición no
    /// alimenta dos órdenes.
    /// </summary>
    public OrdenCompra CrearDesdeRequisicion(Requisicion requisicion, CotizacionProveedor cotizacionGanadora, int creadoPorId)
    {
        if (!PuedeArmarOrdenCompra(requisicion))
            throw new InvalidOperationException(
                $"Solo se arma una orden de compra a partir de una requisición enviada; esta está {requisicion.EstadoTexto}.");

        if (cotizacionGanadora.RequisicionId != requisicion.Id)
            throw new InvalidOperationException("La cotización elegida no pertenece a esta requisición.");

        var lineas = requisicion.Lineas.Select(l => new OrdenCompraLinea
        {
            TipoInsumo = l.TipoInsumo,
            TipoCombustibleSolicitado = l.TipoCombustibleSolicitado,
            TipoLubricante = l.TipoLubricante,
            ArticuloCodigo = l.ArticuloCodigo,
            ArticuloNombre = l.ArticuloNombre,
            ActivoId = l.ActivoId,
            ActivoEtiqueta = l.ActivoEtiqueta,
            Cantidad = l.Cantidad,
            UnidadTexto = l.UnidadTexto,
            PrecioUnitario = 0m
        }).ToList();

        // Con una sola línea el precio unitario se deduce solo del monto cotizado, así el monto
        // de la orden nace ya sincronizado con lo que se comparó. Con varias líneas no hay forma
        // segura de repartir un total entre ellas sin inventar un criterio: quedan en 0 y el
        // monto cotizado se muestra aparte, como referencia, al completarlas a mano.
        if (lineas.Count == 1)
            lineas[0].PrecioUnitario = Math.Round(cotizacionGanadora.MontoTotal / lineas[0].Cantidad, 2);

        var orden = new OrdenCompra
        {
            Fecha = DateTime.Today,
            RequisicionId = requisicion.Id,
            ProveedorId = cotizacionGanadora.ProveedorId,
            ProveedorNombre = cotizacionGanadora.ProveedorNombre,
            CotizacionSeleccionadaId = cotizacionGanadora.Id,
            MontoCotizado = cotizacionGanadora.MontoTotal,
            Estado = EstadoOrdenCompra.Borrador,
            CreadoPorId = creadoPorId,
            FechaCreacion = DateTime.Now,
            Lineas = lineas
        };

        var agregada = _ordenesCompra.Add(orden);

        var requisicionAtendida = requisicion.Clonar();
        requisicionAtendida.Estado = EstadoRequisicion.Atendida;
        _requisiciones.Update(requisicionAtendida);

        return agregada;
    }

    /// <summary>Aprueba el gasto. Es también el momento en que la orden queda emitida al proveedor.</summary>
    public OrdenCompra Aprobar(OrdenCompra orden, int aprobadoPorId)
    {
        if (!PuedeAprobarOrdenCompra(orden))
            throw new InvalidOperationException(
                $"No se puede aprobar una orden de compra en estado {orden.EstadoTexto}.");

        if (!OrdenCompraEstaCompleta(orden, out var faltantes))
            throw new InvalidOperationException($"La orden de compra está incompleta. Faltan: {faltantes}.");

        var actualizada = orden.Clonar();
        actualizada.Estado = EstadoOrdenCompra.Aprobada;
        actualizada.AprobadoPorId = aprobadoPorId;
        actualizada.FechaAprobacion = DateTime.Now;
        _ordenesCompra.Update(actualizada);
        return actualizada;
    }

    public OrdenCompra AnularOrdenCompra(OrdenCompra orden, string motivo)
    {
        if (!PuedeAnularOrdenCompra(orden))
            throw new InvalidOperationException(
                $"No se puede anular una orden de compra en estado {orden.EstadoTexto}.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Debe indicar el motivo de la anulación.");

        var actualizada = orden.Clonar();
        actualizada.Estado = EstadoOrdenCompra.Anulada;
        actualizada.MotivoAnulacion = motivo.Trim();
        actualizada.FechaAnulacion = DateTime.Now;
        _ordenesCompra.Update(actualizada);
        return actualizada;
    }
}
