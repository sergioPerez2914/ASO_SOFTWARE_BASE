using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Arma la línea de tiempo de una remesa fusionando dos orígenes:
///
/// 1. Eventos <b>derivados</b> de los campos de la propia remesa (registro, carga, confirmación,
///    llegada, pesaje, anulación). No se almacenan: se calculan en cada consulta, así que siempre
///    reflejan el documento y una remesa creada o confirmada durante la sesión aparece sin más.
///    Llevan <c>Id = 0</c> y nunca pasan por <c>Update</c>/<c>Delete</c>.
/// 2. Eventos <b>almacenados</b> en <see cref="IEventoOperacionDataSource"/>: cambios de turno,
///    mantenimientos y notas, que no viven en el documento.
///
/// Regla de oro: la validación de la nota vive aquí, no en el botón.
/// </summary>
public sealed class SeguimientoService
{
    private readonly IEventoOperacionDataSource _eventos;

    public SeguimientoService(IEventoOperacionDataSource eventos) => _eventos = eventos;

    /// <summary>
    /// Historia completa de la remesa en orden ascendente. Con la misma hora desempata el tipo,
    /// declarado en orden de ciclo de vida (la llegada al central precede a su pesaje).
    /// </summary>
    public IReadOnlyList<EventoOperacion> ObtenerTimeline(Remesa remesa)
        => DerivarEventosDocumento(remesa)
            .Concat(_eventos.GetByRemesa(remesa.Id))
            .OrderBy(e => e.FechaHora)
            .ThenBy(e => (int)e.Tipo)
            .ToList();

    /// <summary>Único evento que el usuario crea a mano; el resto los publica el sistema.</summary>
    public EventoOperacion AgregarNota(Remesa remesa, string texto, string autor)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new InvalidOperationException("La nota no puede estar vacía.");

        return _eventos.Add(new EventoOperacion
        {
            RemesaId = remesa.Id,
            Tipo = TipoEventoOperacion.Nota,
            FechaHora = DateTime.Now,
            Descripcion = texto.Trim(),
            Autor = autor
        });
    }

    private static IEnumerable<EventoOperacion> DerivarEventosDocumento(Remesa remesa)
    {
        yield return Crear(remesa, TipoEventoOperacion.Registro, remesa.FechaCreacion,
            $"Remesa Nº {remesa.Id} registrada — {remesa.FincaNombre}, {remesa.UbicacionTexto}.");

        yield return Crear(remesa, TipoEventoOperacion.InicioCarga, remesa.InicioCarga,
            $"Comienza la carga en {remesa.UbicacionTexto} (cosecha {remesa.TipoCosechaTexto.ToLowerInvariant()}). " +
            $"Operador {remesa.OperadorNombre}, tractorista {remesa.TractoristaNombre}.");

        yield return Crear(remesa, TipoEventoOperacion.FinCarga, remesa.FinCarga,
            $"Carga completa en la unidad {remesa.VehiculoPlaca}, a cargo de {remesa.ChoferNombre}.");

        if (remesa.FechaConfirmacion is { } confirmacion)
            yield return Crear(remesa, TipoEventoOperacion.Confirmacion, confirmacion,
                "El documento queda inmutable y cuenta para la liquidación.");

        if (remesa.LlegadaCentral is { } llegada)
        {
            yield return Crear(remesa, TipoEventoOperacion.LlegadaCentral, llegada,
                "Llegada a la Pre-Romana del CAM Las Majaguas.");

            if (remesa.PesoNetoT is { } neto)
                yield return Crear(remesa, TipoEventoOperacion.Pesaje, llegada,
                    $"Bruto {remesa.PesoBrutoT:N2} t − tara {remesa.TaraT:N2} t = neto {neto:N2} t.");
        }

        if (remesa.FechaAnulacion is { } anulacion)
            yield return Crear(remesa, TipoEventoOperacion.Anulacion, anulacion,
                $"Motivo: {remesa.MotivoAnulacion}");
    }

    private static EventoOperacion Crear(Remesa remesa, TipoEventoOperacion tipo, DateTime fechaHora, string descripcion)
        => new()
        {
            RemesaId = remesa.Id,
            Tipo = tipo,
            FechaHora = fechaHora,
            Descripcion = descripcion
        };
}
