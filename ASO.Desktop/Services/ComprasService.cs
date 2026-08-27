using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de negocio del flujo de compras: <c>Requisicion</c> (Borrador → Enviada → Atendida,
/// rama Anulada), <c>OrdenCompra</c> (Borrador → Aprobada → Cerrada, rama Anulada) y
/// <c>RecepcionMercancia</c> (Borrador → Confirmada, rama Anulada). Mismo contrato que
/// <see cref="RemesaService"/>: los <c>PuedeX</c> alimentan el <c>CanExecute</c> (cortesía
/// visual) y las transiciones vuelven a validar y lanzan si el estado no lo permite (defensa en
/// profundidad).
///
/// Confirmar una recepción es lo que de verdad mueve inventario: suma cada línea al
/// <see cref="StockCombustible"/> (diésel), al <see cref="Lubricante"/> (aceite) o al
/// <see cref="InventoryItem"/> (repuesto) que le corresponde, por la cantidad REALMENTE recibida.
/// El cotejo a tres vías con Cuentas por Pagar (el que cierra la orden a <c>Cerrada</c>) sigue
/// pendiente, en una fase posterior.
/// </summary>
public sealed class ComprasService
{
    private readonly IRequisicionDataSource _requisiciones;
    private readonly ICotizacionProveedorDataSource _cotizaciones;
    private readonly IOrdenCompraDataSource _ordenesCompra;
    private readonly IRecepcionMercanciaDataSource _recepciones;
    private readonly IInventoryDataSource _articulos;
    private readonly IStockCombustibleDataSource _stockCombustible;
    private readonly ILubricanteDataSource _lubricantes;

    public ComprasService(IRequisicionDataSource requisiciones,
                          ICotizacionProveedorDataSource cotizaciones,
                          IOrdenCompraDataSource ordenesCompra,
                          IRecepcionMercanciaDataSource recepciones,
                          IInventoryDataSource articulos,
                          IStockCombustibleDataSource stockCombustible,
                          ILubricanteDataSource lubricantes)
    {
        _requisiciones = requisiciones;
        _cotizaciones = cotizaciones;
        _ordenesCompra = ordenesCompra;
        _recepciones = recepciones;
        _articulos = articulos;
        _stockCombustible = stockCombustible;
        _lubricantes = lubricantes;
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

    /// <summary>
    /// Construye las líneas de arranque para cotizar (precio en cero, marca y presentación
    /// vacíos — a completar por quien cotiza; la clase de lubricante ya viene de la requisición).
    /// Método puro, sin efectos: lo usa <see cref="CompararProveedoresEditorViewModel"/> para
    /// poblar la grilla de detalle de cada proveedor que se cotiza, y no toca la requisición ni
    /// crea nada todavía.
    /// </summary>
    public static List<CotizacionProveedorLinea> ArmarLineasCotizacion(Requisicion requisicion) =>
        requisicion.Lineas.Select(l => new CotizacionProveedorLinea
        {
            TipoInsumo = l.TipoInsumo,
            TipoCombustibleSolicitado = l.TipoCombustibleSolicitado,
            TipoLubricante = l.TipoLubricante,
            ClaseLubricante = l.ClaseLubricante,
            ArticuloCodigo = l.ArticuloCodigo,
            ArticuloNombre = l.ArticuloNombre,
            ActivoId = l.ActivoId,
            ActivoEtiqueta = l.ActivoEtiqueta,
            Cantidad = l.Cantidad,
            UnidadTexto = l.UnidadTexto,
            PrecioUnitario = 0m
        }).ToList();

    /// <summary>Mismo criterio que <see cref="OrdenCompraEstaCompleta"/>: lo usa la pantalla antes
    /// de dejar guardar la cotización de un proveedor.</summary>
    public static bool CotizacionEstaCompleta(List<CotizacionProveedorLinea> lineas, out string? faltantes)
    {
        var pendientes = new List<string>();

        if (lineas.Count == 0)
            pendientes.Add("al menos una línea");

        if (lineas.Any(l => l.PrecioUnitario <= 0))
            pendientes.Add("el precio unitario de cada línea");

        if (lineas.Any(l => l.EsLubricante && l.MarcaLubricanteId is null))
            pendientes.Add("la marca de cada línea de lubricante");

        if (lineas.Any(l => l.EsLubricante && string.IsNullOrWhiteSpace(l.ClaseLubricante)))
            pendientes.Add("la clase (mineral/sintético) de cada línea de lubricante");

        if (lineas.Any(l => l.EsLubricante && string.IsNullOrWhiteSpace(l.Presentacion)))
            pendientes.Add("la presentación de cada línea de lubricante");

        if (lineas.Any(l => l.EsLubricante && l.Unidades <= 0))
            pendientes.Add("las unidades (envases) de cada línea de lubricante");

        faltantes = pendientes.Count == 0 ? null : string.Join(", ", pendientes);
        return pendientes.Count == 0;
    }

    // --- Orden de compra: reglas de transición ---

    public bool PuedeEliminarOrdenCompra(OrdenCompra oc) => oc.Estado == EstadoOrdenCompra.Borrador;

    public bool PuedeAprobarOrdenCompra(OrdenCompra oc) => oc.Estado == EstadoOrdenCompra.Borrador;

    public bool PuedeAnularOrdenCompra(OrdenCompra oc)
        => oc.Estado is EstadoOrdenCompra.Borrador or EstadoOrdenCompra.Aprobada;

    /// <summary>Solo una orden aprobada y sin recepción activa admite registrar una.</summary>
    public bool PuedeRegistrarRecepcion(OrdenCompra oc) =>
        oc.Estado == EstadoOrdenCompra.Aprobada && oc.RecepcionMercanciaId is null;

    public static bool OrdenCompraEstaCompleta(OrdenCompra orden, out string? faltantes)
    {
        var pendientes = new List<string>();

        if (orden.ProveedorId == 0)
            pendientes.Add("el proveedor");

        if (orden.Lineas.Count == 0)
            pendientes.Add("al menos una línea");

        if (orden.Lineas.Any(l => l.PrecioUnitario <= 0))
            pendientes.Add("el precio unitario de cada línea");

        if (orden.Lineas.Any(l => l.EsLubricante && l.MarcaLubricanteId is null))
            pendientes.Add("la marca de cada línea de lubricante");

        if (orden.Lineas.Any(l => l.EsLubricante && string.IsNullOrWhiteSpace(l.ClaseLubricante)))
            pendientes.Add("la clase (mineral/sintético) de cada línea de lubricante");

        if (orden.Lineas.Any(l => l.EsLubricante && string.IsNullOrWhiteSpace(l.Presentacion)))
            pendientes.Add("la presentación de cada línea de lubricante");

        if (orden.Lineas.Any(l => l.EsLubricante && l.Unidades <= 0))
            pendientes.Add("las unidades (envases) de cada línea de lubricante");

        faltantes = pendientes.Count == 0 ? null : string.Join(", ", pendientes);
        return pendientes.Count == 0;
    }

    // --- Orden de compra: transiciones ---

    /// <summary>
    /// Arma la orden de compra copiando 1:1 las líneas ya cargadas en la cotización ganadora
    /// (precio unitario, marca, clase y presentación de cada línea de lubricante ya vienen
    /// completos desde "Comparar proveedores" — no se vuelven a pedir aquí) y marca la
    /// requisición como Atendida: una requisición no alimenta dos órdenes. Vuelve a validar que
    /// las líneas estén completas — la ventana ya lo exige antes de dejar guardar cada cotización,
    /// pero no hay que confiar solo en eso (defensa en profundidad, mismo criterio que <see cref="Aprobar"/>).
    /// </summary>
    public OrdenCompra CrearDesdeRequisicion(Requisicion requisicion, CotizacionProveedor cotizacionGanadora, int creadoPorId)
    {
        if (!PuedeArmarOrdenCompra(requisicion))
            throw new InvalidOperationException(
                $"Solo se arma una orden de compra a partir de una requisición enviada; esta está {requisicion.EstadoTexto}.");

        if (cotizacionGanadora.RequisicionId != requisicion.Id)
            throw new InvalidOperationException("La cotización elegida no pertenece a esta requisición.");

        var lineas = cotizacionGanadora.Lineas.Select(l => new OrdenCompraLinea
        {
            TipoInsumo = l.TipoInsumo,
            TipoCombustibleSolicitado = l.TipoCombustibleSolicitado,
            TipoLubricante = l.TipoLubricante,
            MarcaLubricanteId = l.MarcaLubricanteId,
            MarcaLubricanteNombre = l.MarcaLubricanteNombre,
            ClaseLubricante = l.ClaseLubricante,
            Presentacion = l.Presentacion,
            ArticuloCodigo = l.ArticuloCodigo,
            ArticuloNombre = l.ArticuloNombre,
            ActivoId = l.ActivoId,
            ActivoEtiqueta = l.ActivoEtiqueta,
            Cantidad = l.Cantidad,
            Unidades = l.Unidades,
            UnidadTexto = l.UnidadTexto,
            PrecioUnitario = l.PrecioUnitario
        }).ToList();

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

        if (!OrdenCompraEstaCompleta(orden, out var faltantes))
            throw new InvalidOperationException($"La orden de compra está incompleta. Faltan: {faltantes}.");

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

    // --- Recepción de mercancía: reglas de transición ---

    public bool PuedeEditarRecepcion(RecepcionMercancia r) => r.Estado == EstadoRecepcionMercancia.Borrador;

    public bool PuedeEliminarRecepcion(RecepcionMercancia r) => r.Estado == EstadoRecepcionMercancia.Borrador;

    public bool PuedeConfirmarRecepcion(RecepcionMercancia r) => r.Estado == EstadoRecepcionMercancia.Borrador;

    public bool PuedeAnularRecepcion(RecepcionMercancia r) =>
        r.Estado is EstadoRecepcionMercancia.Borrador or EstadoRecepcionMercancia.Confirmada;

    public static bool RecepcionEstaCompleta(RecepcionMercancia r, out string? faltantes)
    {
        var pendientes = new List<string>();

        if (r.Lineas.Count == 0)
            pendientes.Add("al menos una línea");

        if (r.Lineas.Any(l => l.CantidadRecibida < 0))
            pendientes.Add("una cantidad recibida válida (no negativa) en cada línea");

        if (r.Lineas.All(l => l.CantidadRecibida <= 0))
            pendientes.Add("al menos una cantidad recibida mayor que cero");

        if (r.Lineas.Any(l => l.EsDiesel && l.CantidadRecibida > 0 && l.StockCombustibleId is null))
            pendientes.Add("el stock de combustible al que se suma cada línea de diésel recibida");

        faltantes = pendientes.Count == 0 ? null : string.Join(", ", pendientes);
        return pendientes.Count == 0;
    }

    // --- Recepción de mercancía: transiciones ---

    /// <summary>
    /// Arma la recepción copiando las líneas de la orden de compra (cantidad recibida =
    /// cantidad pedida, a corregir antes de confirmar si hubo faltante o sobrante) y marca la
    /// orden con la recepción activa: una orden aprobada no admite dos recepciones a la vez.
    /// </summary>
    public RecepcionMercancia CrearRecepcionDesdeOrdenCompra(OrdenCompra orden, int creadoPorId)
    {
        if (!PuedeRegistrarRecepcion(orden))
            throw new InvalidOperationException(orden.RecepcionMercanciaId is not null
                ? $"La orden de compra Nº {orden.Id} ya tiene una recepción registrada."
                : $"Solo se registra una recepción para una orden aprobada; esta está {orden.EstadoTexto}.");

        var lineas = orden.Lineas.Select(l => new RecepcionMercanciaLinea
        {
            TipoInsumo = l.TipoInsumo,
            TipoCombustibleSolicitado = l.TipoCombustibleSolicitado,
            TipoLubricante = l.TipoLubricante,
            MarcaLubricanteId = l.MarcaLubricanteId,
            MarcaLubricanteNombre = l.MarcaLubricanteNombre,
            ClaseLubricante = l.ClaseLubricante,
            Presentacion = l.Presentacion,
            ArticuloCodigo = l.ArticuloCodigo,
            ArticuloNombre = l.ArticuloNombre,
            ActivoId = l.ActivoId,
            ActivoEtiqueta = l.ActivoEtiqueta,
            CantidadPedida = l.Cantidad,
            CantidadRecibida = l.Cantidad,      // se asume completa por defecto; se corrige en el editor
            UnidadTexto = l.UnidadTexto
        }).ToList();

        var recepcion = new RecepcionMercancia
        {
            Fecha = DateTime.Today,
            OrdenCompraId = orden.Id,
            ProveedorId = orden.ProveedorId,
            ProveedorNombre = orden.ProveedorNombre,
            Estado = EstadoRecepcionMercancia.Borrador,
            CreadoPorId = creadoPorId,
            FechaCreacion = DateTime.Now,
            Lineas = lineas
        };

        var agregada = _recepciones.Add(recepcion);

        var ordenActualizada = orden.Clonar();
        ordenActualizada.RecepcionMercanciaId = agregada.Id;
        _ordenesCompra.Update(ordenActualizada);

        return agregada;
    }

    /// <summary>
    /// Confirma la recepción: suma cada línea al stock que le corresponde (StockCombustible o
    /// InventoryItem) según lo REALMENTE recibido, no lo pedido. A partir de aquí es inmutable.
    /// Valida todas las líneas ANTES de tocar cualquier stock, para no dejar el movimiento a
    /// medias si una línea falla (mismo criterio que CombustibleService.Confirmar).
    ///
    /// <paramref name="recibidoPor"/> se pide en este mismo paso (ver
    /// <c>ResponsableRecepcionEditorViewModel</c>), no al editar el borrador: es la firma de quien
    /// tuvo la carga enfrente al momento de confirmar, no un dato de las líneas.
    /// </summary>
    public RecepcionMercancia ConfirmarRecepcion(RecepcionMercancia recepcion, string recibidoPor)
    {
        if (!PuedeConfirmarRecepcion(recepcion))
            throw new InvalidOperationException(
                $"Solo se puede confirmar una recepción en borrador; esta está {recepcion.EstadoTexto}.");

        if (string.IsNullOrWhiteSpace(recibidoPor))
            throw new InvalidOperationException("Debe indicar quién recibió la mercancía.");

        if (!RecepcionEstaCompleta(recepcion, out var faltantes))
            throw new InvalidOperationException($"Faltan datos para confirmar la recepción: {faltantes}.");

        var aAplicar = recepcion.Lineas.Where(l => l.CantidadRecibida > 0).ToList();

        // Validación previa: nada se escribe hasta saber que todas las líneas son válidas.
        foreach (var linea in aAplicar)
        {
            if (linea.TipoInsumo == TipoInsumo.Repuesto)
            {
                if (_articulos.GetById(linea.ArticuloCodigo!) is null)
                    throw new InvalidOperationException(
                        $"El artículo {linea.ArticuloCodigo} ya no existe en el catálogo de inventario.");
            }
            else if (linea.EsDiesel)
            {
                var stock = _stockCombustible.GetById(linea.StockCombustibleId!.Value)
                    ?? throw new InvalidOperationException("El stock de combustible indicado ya no existe.");

                if (stock.ExistenciaL + linea.CantidadRecibida > stock.CapacidadL)
                    throw new InvalidOperationException(
                        $"El stock de {stock.Nombre} tiene {stock.ExistenciaL:N2} L de {stock.CapacidadL:N2} L " +
                        $"y no admite {linea.CantidadRecibida:N2} L más. Verifique la cantidad recibida.");
            }
            // La rama de lubricante no valida nada aquí: Marca+Clase+Grado ya se exigieron para
            // aprobar la orden de compra, y ConfirmarRecepcion la busca o la crea sola más abajo
            // — a diferencia de Diésel/Repuesto, esta rama no puede fallar.
        }

        // Efecto: sumar cada línea a su stock.
        var orden = _ordenesCompra.GetById(recepcion.OrdenCompraId);
        foreach (var linea in aAplicar)
        {
            if (linea.TipoInsumo == TipoInsumo.Repuesto)
            {
                var articulo = _articulos.GetById(linea.ArticuloCodigo!)!;
                var actualizado = articulo.Clonar();
                actualizado.StockActual += linea.CantidadRecibida;
                _articulos.Update(actualizado);
            }
            else if (linea.EsDiesel)
            {
                var stock = _stockCombustible.GetById(linea.StockCombustibleId!.Value)!;
                var actualizado = stock.Clonar();
                actualizado.ExistenciaL += linea.CantidadRecibida;
                _stockCombustible.Update(actualizado);
            }
            else
            {
                // Ya no lo elige el almacenista: se busca por Marca+Clase+Grado+Presentación (todo
                // ya fijado desde la orden de compra) y se crea si es la primera vez que llega esa
                // combinación. La presentación entra en la búsqueda porque Unidades solo tiene
                // sentido dentro de un mismo envase — mezclar barriles y galones en una sola fila
                // rompería la cuenta.
                var lubricante = _lubricantes.GetAll().FirstOrDefault(l =>
                    l.MarcaLubricanteId == linea.MarcaLubricanteId
                    && l.Tipo == linea.ClaseLubricante
                    && l.GradoViscosidad == linea.TipoLubricante
                    && l.Presentacion == linea.Presentacion);

                lubricante ??= _lubricantes.Add(new Lubricante
                {
                    MarcaLubricanteId = linea.MarcaLubricanteId!.Value,
                    MarcaLubricanteNombre = linea.MarcaLubricanteNombre,
                    Tipo = linea.ClaseLubricante!,
                    GradoViscosidad = linea.TipoLubricante!,
                    Presentacion = linea.Presentacion!,
                    Unidades = 0,
                    Activo = true
                });

                // CantidadRecibida sigue en litros (como toda la cadena Requisición → Orden de
                // Compra → Recepción); Unidades de Lubricante es la cuenta de envases, así que se
                // convierte dividiendo por los litros que tiene la presentación elegida.
                var litrosPorUnidad = Lubricante.LitrosPorPresentacion.GetValueOrDefault(linea.Presentacion!, 1m);
                var actualizado = lubricante.Clonar();
                actualizado.Unidades += linea.CantidadRecibida / litrosPorUnidad;

                // El precio de la orden ya es por envase (mismo criterio que Unidades, cargado al
                // cotizar): se copia tal cual, sin convertir. Es un snapshot del último precio
                // pagado, no un promedio: la próxima recepción lo vuelve a pisar.
                var ordenLinea = orden?.Lineas.FirstOrDefault(ol =>
                    ol.EsLubricante
                    && ol.MarcaLubricanteId == linea.MarcaLubricanteId
                    && ol.ClaseLubricante == linea.ClaseLubricante
                    && ol.TipoLubricante == linea.TipoLubricante
                    && ol.Presentacion == linea.Presentacion);

                if (ordenLinea is not null)
                    actualizado.CostoUnitario = ordenLinea.PrecioUnitario;

                _lubricantes.Update(actualizado);

                linea.LubricanteId = lubricante.Id;
                linea.LubricanteNombre = lubricante.Etiqueta;
            }
        }

        var copia = recepcion.Clonar();
        copia.RecibidoPor = recibidoPor.Trim();
        copia.Estado = EstadoRecepcionMercancia.Confirmada;
        copia.FechaConfirmacion = DateTime.Now;
        _recepciones.Update(copia);

        return copia;
    }

    /// <summary>
    /// Anula la recepción. Si estaba confirmada, repone (resta) lo que se había sumado a cada
    /// stock — igual criterio que InventarioService.Anular/CombustibleService.Anular: no valida
    /// que el stock quede en cero o negativo, porque puede haberse consumido desde entonces; es
    /// un caso límite conocido y consistente con el resto del sistema. Libera además la orden de
    /// compra (RecepcionMercanciaId vuelve a null) para permitir registrar una nueva recepción
    /// si esta se anuló por un error de captura.
    /// </summary>
    public RecepcionMercancia AnularRecepcion(RecepcionMercancia recepcion, string motivo)
    {
        if (!PuedeAnularRecepcion(recepcion))
            throw new InvalidOperationException(
                $"No se puede anular una recepción en estado {recepcion.EstadoTexto}.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Debe indicar el motivo de la anulación.");

        if (recepcion.Estado == EstadoRecepcionMercancia.Confirmada)
        {
            foreach (var linea in recepcion.Lineas.Where(l => l.CantidadRecibida > 0))
            {
                if (linea.TipoInsumo == TipoInsumo.Repuesto && _articulos.GetById(linea.ArticuloCodigo!) is { } articulo)
                {
                    var actualizado = articulo.Clonar();
                    actualizado.StockActual -= linea.CantidadRecibida;
                    _articulos.Update(actualizado);
                }
                else if (linea.EsDiesel && linea.StockCombustibleId is { } id
                         && _stockCombustible.GetById(id) is { } stock)
                {
                    var actualizado = stock.Clonar();
                    actualizado.ExistenciaL -= linea.CantidadRecibida;
                    _stockCombustible.Update(actualizado);
                }
                else if (linea.EsLubricante && linea.LubricanteId is { } lubricanteId
                         && _lubricantes.GetById(lubricanteId) is { } lubricante)
                {
                    var litrosPorUnidad = Lubricante.LitrosPorPresentacion.GetValueOrDefault(linea.Presentacion!, 1m);
                    var actualizado = lubricante.Clonar();
                    actualizado.Unidades -= linea.CantidadRecibida / litrosPorUnidad;
                    _lubricantes.Update(actualizado);
                }
            }
        }

        var copia = recepcion.Clonar();
        copia.Estado = EstadoRecepcionMercancia.Anulada;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _recepciones.Update(copia);

        if (_ordenesCompra.GetById(recepcion.OrdenCompraId) is { } orden && orden.RecepcionMercanciaId == recepcion.Id)
        {
            var ordenActualizada = orden.Clonar();
            ordenActualizada.RecepcionMercanciaId = null;
            _ordenesCompra.Update(ordenActualizada);
        }

        return copia;
    }
}
