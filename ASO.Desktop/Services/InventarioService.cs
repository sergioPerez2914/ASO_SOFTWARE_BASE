using System;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de la salida de inventario. Mismo contrato que <see cref="RemesaService"/>: los
/// métodos <c>PuedeX</c> alimentan el <c>CanExecute</c> de los botones y las transiciones
/// vuelven a validar antes de aplicar efectos, porque el botón deshabilitado es cortesía
/// visual y la regla la impone el servicio.
///
/// Al confirmar se aplican tres efectos en la misma operación: se descuenta el stock, se fija
/// el costo del artículo como copia dentro del documento, y —si la salida se imputó a un
/// mantenimiento— se recalcula el costo de repuestos de ese mantenimiento. Con eso el registro
/// de taller deja de depender de una cifra escrita a mano.
/// </summary>
public sealed class InventarioService
{
    private readonly ISalidaInventarioDataSource _salidas;
    private readonly IInventoryDataSource _articulos;
    private readonly IMantenimientoRegistroDataSource _mantenimientos;

    public InventarioService(ISalidaInventarioDataSource salidas,
                             IInventoryDataSource articulos,
                             IMantenimientoRegistroDataSource mantenimientos)
    {
        _salidas = salidas;
        _articulos = articulos;
        _mantenimientos = mantenimientos;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeEditar(SalidaInventario s) => s.Estado == EstadoSalida.Borrador;

    public bool PuedeEliminar(SalidaInventario s) => s.Estado == EstadoSalida.Borrador;

    public bool PuedeConfirmar(SalidaInventario s) => s.Estado == EstadoSalida.Borrador;

    public bool PuedeAnular(SalidaInventario s) =>
        s.Estado is EstadoSalida.Borrador or EstadoSalida.Confirmada;

    /// <summary>
    /// ¿Faltan datos para confirmar? Se usa antes de la transición y también desde el editor,
    /// para que el usuario vea qué le falta sin tener que intentar confirmar.
    /// </summary>
    public static bool EstaCompleta(SalidaInventario s, out string? faltantes)
    {
        var falta = new System.Collections.Generic.List<string>();

        if (string.IsNullOrWhiteSpace(s.ArticuloCodigo))
            falta.Add("el artículo");

        if (s.Cantidad <= 0)
            falta.Add("la cantidad");

        if (string.IsNullOrWhiteSpace(s.Motivo) && s.ActivoId is null)
            falta.Add("el motivo o el activo de destino");

        faltantes = falta.Count == 0 ? null : string.Join(", ", falta);
        return falta.Count == 0;
    }

    // --- Transiciones ---

    /// <summary>
    /// Confirma la salida: a partir de aquí es inmutable y el stock ya está descontado.
    /// </summary>
    /// <param name="forzarStock">
    /// Autorización de excepción (solo Admin) para sacar más de lo que hay en existencia.
    /// Queda registrada en el documento, no es un permiso silencioso.
    /// </param>
    public SalidaInventario Confirmar(SalidaInventario salida, bool forzarStock = false)
    {
        if (!PuedeConfirmar(salida))
            throw new InvalidOperationException("Solo se puede confirmar una salida en borrador.");

        if (!EstaCompleta(salida, out var faltantes))
            throw new InvalidOperationException($"Faltan datos para confirmar la salida: {faltantes}.");

        var articulo = _articulos.GetById(salida.ArticuloCodigo)
            ?? throw new InvalidOperationException(
                $"El artículo {salida.ArticuloCodigo} ya no existe en el catálogo de inventario.");

        if (articulo.StockActual < salida.Cantidad && !forzarStock)
            throw new InvalidOperationException(
                $"Existencia insuficiente de {articulo.Nombre}: quedan {articulo.StockActual:N2} {articulo.Unidad} " +
                $"y se piden {salida.Cantidad:N2}. Solo un administrador puede autorizar la salida.");

        // Efecto 1: descontar la existencia.
        var actualizado = articulo.Clonar();
        actualizado.StockActual -= salida.Cantidad;
        _articulos.Update(actualizado);

        // Efecto 2: fijar el valor del documento con el costo del día.
        var copia = salida.Clonar();
        copia.CostoUnitario = articulo.CostoUnitario;
        copia.Unidad = articulo.Unidad;
        copia.ArticuloNombre = articulo.Nombre;
        copia.Estado = EstadoSalida.Confirmada;
        copia.FechaConfirmacion = DateTime.Now;
        copia.StockForzado = forzarStock && articulo.StockActual < salida.Cantidad;
        _salidas.Update(copia);

        // Efecto 3: revalorizar el mantenimiento al que se imputó.
        RecalcularMantenimiento(copia.MantenimientoId);

        return copia;
    }

    /// <summary>
    /// Anula la salida. Si ya estaba confirmada, repone la existencia y vuelve a valorizar el
    /// mantenimiento: el almacén tiene que cuadrar con lo que hay físicamente.
    /// </summary>
    public SalidaInventario Anular(SalidaInventario salida, string motivo)
    {
        if (!PuedeAnular(salida))
            throw new InvalidOperationException("Solo se puede anular una salida en borrador o confirmada.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo de la anulación.");

        if (salida.Estado == EstadoSalida.Confirmada
            && _articulos.GetById(salida.ArticuloCodigo) is { } articulo)
        {
            var repuesto = articulo.Clonar();
            repuesto.StockActual += salida.Cantidad;
            _articulos.Update(repuesto);
        }

        var copia = salida.Clonar();
        copia.Estado = EstadoSalida.Anulada;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _salidas.Update(copia);

        RecalcularMantenimiento(copia.MantenimientoId);

        return copia;
    }

    /// <summary>
    /// Deja el costo de repuestos del mantenimiento igual a la suma de sus salidas confirmadas.
    ///
    /// Resuelve el TODO de <see cref="MantenimientoRegistro"/>: el registro de taller sigue
    /// siendo inmutable desde la UI; es el sistema el que deriva su costo de las salidas
    /// valoradas, no una cifra que alguien teclea.
    /// </summary>
    private void RecalcularMantenimiento(int? mantenimientoId)
    {
        if (mantenimientoId is not { } id)
            return;

        if (_mantenimientos.GetById(id) is not { } registro)
            return;

        var confirmadas = _salidas.GetByMantenimiento(id)
            .Where(s => s.Estado == EstadoSalida.Confirmada)
            .ToList();

        registro.CostoRepuestos = confirmadas.Count == 0 ? null : confirmadas.Sum(s => s.CostoTotal);
        registro.RepuestosUsados = string.Join(Environment.NewLine,
            confirmadas.Select(s => $"{s.ArticuloNombre} × {s.Cantidad:N2} {s.Unidad}"));

        _mantenimientos.Update(registro);
    }

    /// <summary>Costo de repuestos ya valorizado que carga un activo (para su hoja de vida).</summary>
    public decimal CostoRepuestosDeActivo(int activoId) =>
        _salidas.GetByActivo(activoId)
            .Where(s => s.Estado == EstadoSalida.Confirmada)
            .Sum(s => s.CostoTotal);
}
