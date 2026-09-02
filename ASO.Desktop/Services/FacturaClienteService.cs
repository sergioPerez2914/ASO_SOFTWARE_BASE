using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de la facturación al ingenio.
///
/// La regla que manda sobre todas es que una remesa recibida se factura una sola vez. Se
/// aplica dos veces a propósito: al listar las facturables (para que no se ofrezcan) y otra vez
/// al emitir (por si otra factura las tomó mientras el borrador estaba abierto). Esa segunda
/// comprobación es la que de verdad protege el cobro; la primera es comodidad.
/// </summary>
public sealed class FacturaClienteService
{
    private readonly IFacturaClienteDataSource _facturas;
    private readonly IRemesaDataSource _remesas;
    private readonly TarifaService _tarifas;
    private readonly IEventoOperacionDataSource _eventos;
    private readonly BancoService _banco;
    private readonly ISesionActual _sesion;

    /// <summary>
    /// Recibe el <see cref="BancoService"/> como parámetro obligatorio, igual que ya recibía el
    /// tarifario: cobrar una factura y anotar la entrada en el libro son la misma operación, y un
    /// parámetro opcional dejaría un hueco silencioso por el que el dinero entraría sin aparecer
    /// en ninguna cuenta.
    /// </summary>
    public FacturaClienteService(IFacturaClienteDataSource facturas,
                                 IRemesaDataSource remesas,
                                 TarifaService tarifas,
                                 IEventoOperacionDataSource eventos,
                                 BancoService banco,
                                 ISesionActual sesion)
    {
        _facturas = facturas;
        _remesas = remesas;
        _tarifas = tarifas;
        _eventos = eventos;
        _banco = banco;
        _sesion = sesion;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeEmitir(FacturaCliente f) => f.Estado == EstadoFacturaCliente.Borrador;

    public bool PuedeRegistrarCobro(FacturaCliente f) => f.Estado == EstadoFacturaCliente.Emitida;

    public bool PuedeAnular(FacturaCliente f) =>
        f.Estado is EstadoFacturaCliente.Borrador or EstadoFacturaCliente.Emitida;

    public bool PuedeEliminar(FacturaCliente f) => f.Estado == EstadoFacturaCliente.Borrador;

    /// <summary>Remesas entregadas al central que todavía no están en ninguna factura.</summary>
    public IReadOnlyList<Remesa> RemesasFacturables() =>
        [.. _remesas.GetAll()
            .Where(r => r.Estado == EstadoRemesa.Recibida && r.FacturaClienteId is null)
            .OrderBy(r => r.LlegadaCentral ?? r.FechaConfirmacion)];

    /// <summary>
    /// Arma el borrador con tres líneas por remesa (corte, alza y empuje, transporte), cada una
    /// con la tarifa de cobro vigente a la fecha en que se recibió la caña.
    /// </summary>
    public FacturaCliente GenerarBorrador(IReadOnlyList<Remesa> seleccionadas, int creadoPorId)
    {
        if (!_sesion.Puede(Permisos.Finanzas.Facturar))
            throw new InvalidOperationException("No tienes permiso para facturar.");

        if (seleccionadas.Count == 0)
            throw new InvalidOperationException("Seleccione al menos una remesa para facturar.");

        var yaFacturada = seleccionadas.FirstOrDefault(r => r.FacturaClienteId is not null);
        if (yaFacturada is not null)
            throw new InvalidOperationException(
                $"La remesa Nº {yaFacturada.Id} ya está en la factura {yaFacturada.FacturadaTexto}.");

        var factura = new FacturaCliente
        {
            Estado = EstadoFacturaCliente.Borrador,
            CreadoPorId = creadoPorId,
            FechaCreacion = DateTime.Now
        };

        foreach (var remesa in seleccionadas.OrderBy(r => r.Id))
        {
            var fecha = remesa.LlegadaCentral ?? remesa.FechaConfirmacion ?? DateTime.Today;
            var toneladas = remesa.PesoNetoT ?? 0m;

            if (toneladas <= 0)
                throw new InvalidOperationException(
                    $"La remesa Nº {remesa.Id} no tiene pesaje registrado: no hay toneladas que facturar.");

            foreach (var cobro in _tarifas.CalcularCobroPorServicio(remesa, toneladas, fecha))
            {
                factura.Lineas.Add(new FacturaClienteLinea
                {
                    RemesaId = remesa.Id,
                    FincaNombre = remesa.FincaNombre,
                    FechaRecepcion = fecha,
                    Servicio = cobro.Servicio,
                    NucleoCodigo = cobro.NucleoCodigo,
                    Toneladas = cobro.Toneladas,
                    TarifaMonto = cobro.TarifaMonto
                });
            }
        }

        return _facturas.Add(factura);
    }

    /// <summary>
    /// Emite la factura: fija fechas, marca cada remesa con el número de factura y la deja
    /// inmutable. Vuelve a comprobar que ninguna remesa se coló en otra factura mientras tanto.
    /// </summary>
    public FacturaCliente Emitir(FacturaCliente factura)
    {
        if (!PuedeEmitir(factura))
            throw new InvalidOperationException("Solo se puede emitir una factura en borrador.");

        if (!_sesion.Puede(Permisos.Finanzas.Facturar))
            throw new InvalidOperationException("No tienes permiso para facturar.");

        if (factura.Lineas.Count == 0)
            throw new InvalidOperationException("La factura no tiene líneas que cobrar.");

        var remesaIds = factura.Lineas.Select(l => l.RemesaId).Distinct().ToList();

        foreach (var id in remesaIds)
        {
            var remesa = _remesas.GetById(id)
                ?? throw new InvalidOperationException($"La remesa Nº {id} ya no existe.");

            if (remesa.FacturaClienteId is { } otra && otra != factura.Id)
                throw new InvalidOperationException(
                    $"La remesa Nº {id} fue facturada mientras tanto en la factura FC-{otra:D4}. " +
                    "Elimine este borrador y genérelo de nuevo.");
        }

        var copia = factura.Clonar();
        copia.Estado = EstadoFacturaCliente.Emitida;
        copia.FechaEmision = DateTime.Today;
        copia.FechaVencimiento = DateTime.Today.AddDays(factura.DiasCredito);
        _facturas.Update(copia);

        foreach (var id in remesaIds)
        {
            if (_remesas.GetById(id) is not { } remesa)
                continue;

            var actualizada = remesa.Clonar();
            actualizada.FacturaClienteId = copia.Id;
            _remesas.Update(actualizada);
        }

        return copia;
    }

    /// <summary>
    /// Da la factura por cobrada y anota la entrada en el libro de banco, en una sola operación.
    ///
    /// El asiento va PRIMERO a propósito: es el que puede rechazar (cuenta cerrada, documento ya
    /// asentado). Si fallara después de marcar la factura, quedaría una factura cobrada sin
    /// rastro del dinero, que es justo lo que este módulo viene a evitar.
    ///
    /// <see cref="FacturaCliente.FechaCobro"/> conserva su significado de siempre —el día en que
    /// se dio por cobrada—; la fecha valor del movimiento, que puede ser otra, vive en el asiento.
    /// </summary>
    public FacturaCliente RegistrarCobro(FacturaCliente factura, AsientoBanco asiento, int usuarioId)
    {
        if (!PuedeRegistrarCobro(factura))
            throw new InvalidOperationException("Solo se puede registrar el cobro de una factura emitida.");

        if (!_sesion.Puede(Permisos.Finanzas.RegistrarCobro))
            throw new InvalidOperationException("No tienes permiso para registrar cobros.");

        _banco.RegistrarCobroCliente(factura, asiento, usuarioId);

        var copia = factura.Clonar();
        copia.Estado = EstadoFacturaCliente.Cobrada;
        copia.FechaCobro = DateTime.Today;
        _facturas.Update(copia);
        return copia;
    }

    /// <summary>
    /// Anula la factura y libera sus remesas, que vuelven a quedar facturables. Una factura ya
    /// cobrada no se anula: el dinero entró y eso se corrige con una nota de crédito, no
    /// borrando el documento.
    /// </summary>
    public FacturaCliente Anular(FacturaCliente factura, string motivo)
    {
        if (!PuedeAnular(factura))
            throw new InvalidOperationException(
                factura.Estado == EstadoFacturaCliente.Cobrada
                    // PROVISIONAL: el reverso de cobros y las notas de crédito están pendientes
                    // de definición del socio.
                    ? "Una factura cobrada no se anula: requiere una nota de crédito."
                    : "Solo se puede anular una factura en borrador o emitida.");

        if (!_sesion.Puede(Permisos.Finanzas.Anular))
            throw new InvalidOperationException("No tienes permiso para anular facturas.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo de la anulación.");

        if (factura.Estado == EstadoFacturaCliente.Emitida)
            LiberarRemesas(factura);

        var copia = factura.Clonar();
        copia.Estado = EstadoFacturaCliente.Anulada;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _facturas.Update(copia);
        return copia;
    }

    /// <summary>Suma emitida y todavía no cobrada: es el saldo por cobrar del centro.</summary>
    public decimal TotalPorCobrar() =>
        _facturas.GetAll()
            .Where(f => f.Estado == EstadoFacturaCliente.Emitida)
            .Sum(f => f.Total);

    public decimal TotalVencido() =>
        _facturas.GetAll()
            .Where(f => f.EstaVencida)
            .Sum(f => f.Total);

    /// <summary>
    /// Devuelve las remesas de la factura al estado de facturables.
    ///
    /// Este es el único hecho de facturación que hay que ESCRIBIR en el seguimiento. Los otros
    /// —emisión y cobro— se deducen de la factura a la que apunta la remesa, pero aquí ese
    /// puntero se pone a nulo: sin el evento, que la remesa estuvo facturada y dejó de estarlo no
    /// quedaría en ninguna parte.
    /// </summary>
    private void LiberarRemesas(FacturaCliente factura)
    {
        foreach (var id in factura.Lineas.Select(l => l.RemesaId).Distinct())
        {
            if (_remesas.GetById(id) is not { } remesa || remesa.FacturaClienteId != factura.Id)
                continue;

            var liberada = remesa.Clonar();
            liberada.FacturaClienteId = null;
            _remesas.Update(liberada);

            _eventos.Add(new EventoOperacion
            {
                RemesaId = remesa.Id,
                Tipo = TipoEventoOperacion.Facturacion,
                FechaHora = DateTime.Now,
                Descripcion = $"Anulada la factura {factura.NumeroTexto}: la remesa vuelve a estar " +
                              "pendiente de facturar.",
                OrigenId = factura.Id
            });
        }
    }
}
