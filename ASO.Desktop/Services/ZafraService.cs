using System;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de la temporada de cosecha: a lo sumo una <see cref="Zafra"/> <c>Abierta</c> por
/// núcleo a la vez. Mismo contrato que <see cref="RemesaService"/>: los <c>PuedeX</c> alimentan
/// el <c>CanExecute</c> y las transiciones vuelven a validar antes de aplicar efectos.
///
/// Reabrir es deliberadamente conservador: solo la última zafra cerrada, y solo si no hay otra
/// ya abierta después de ella — evita que existan dos "épocas" simultáneas. Es una decisión por
/// defecto, no confirmada con el socio (ver el plan de Zafra activa); si el negocio necesita
/// reabrir una zafra más vieja, esta regla es el sitio a revisar.
/// </summary>
public sealed class ZafraService
{
    private readonly IZafraDataSource _zafras;
    private readonly ISesionActual _sesion;

    public ZafraService(IZafraDataSource zafras, ISesionActual sesion)
    {
        _zafras = zafras;
        _sesion = sesion;
    }

    public bool PuedeCerrar(Zafra zafra) => zafra.Estado == EstadoZafra.Abierta;

    public bool PuedeReabrir(Zafra zafra)
    {
        if (zafra.Estado != EstadoZafra.Cerrada)
            return false;

        if (_zafras.GetAll().Any(z => z.Estado == EstadoZafra.Abierta))
            return false;

        var ultimaCerrada = _zafras.GetAll()
            .Where(z => z.Estado == EstadoZafra.Cerrada)
            .OrderByDescending(z => z.FechaCierre)
            .FirstOrDefault();

        return ultimaCerrada?.Id == zafra.Id;
    }

    /// <summary>Validación de campos, previa a abrir o a corregir una zafra existente.</summary>
    public static bool Validar(Zafra zafra, out string? error)
    {
        if (string.IsNullOrWhiteSpace(zafra.Codigo))
        {
            error = "Indique el código de la zafra.";
            return false;
        }

        if (zafra.FechaFinPrevista is { } fin && fin.Date < zafra.FechaInicio.Date)
        {
            error = "La fecha de fin prevista no puede ser anterior a la de inicio.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Abre una zafra nueva y la fija como activa. Rechaza si ya hay otra Abierta del mismo
    /// núcleo: cerrarla es el paso previo, nunca implícito.
    /// </summary>
    public Zafra Abrir(Zafra nueva)
    {
        if (!_sesion.Puede(Permisos.Zafra.Crear))
            throw new InvalidOperationException("No tienes permiso para abrir una zafra.");

        if (!Validar(nueva, out var error))
            throw new InvalidOperationException(error);

        if (_zafras.GetAll().Any(z => z.Estado == EstadoZafra.Abierta))
            throw new InvalidOperationException(
                "Ya hay una zafra abierta. Ciérrela antes de abrir otra.");

        nueva.Estado = EstadoZafra.Abierta;
        nueva.FechaCierre = null;
        nueva.MotivoCierre = null;

        var agregada = _zafras.Add(nueva);
        ZafraActiva.Fijar(agregada);
        return agregada;
    }

    /// <summary>Cierra la zafra activa. A partir de aquí no queda ninguna zafra fijada hasta
    /// que se abra la siguiente.</summary>
    public Zafra Cerrar(Zafra actual, string motivo)
    {
        if (!PuedeCerrar(actual))
            throw new InvalidOperationException(
                $"No se puede cerrar una zafra en estado {actual.EstadoTexto}.");

        if (!_sesion.Puede(Permisos.Zafra.Cerrar))
            throw new InvalidOperationException("No tienes permiso para cerrar la zafra.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo del cierre.");

        var copia = actual.Clonar();
        copia.Estado = EstadoZafra.Cerrada;
        copia.FechaCierre = DateTime.Now;
        copia.MotivoCierre = motivo.Trim();
        _zafras.Update(copia);

        if (ZafraActiva.ZafraId == copia.Id)
            ZafraActiva.Fijar(null);

        return copia;
    }

    /// <summary>Reabre la última zafra cerrada como excepción. Ver <see cref="PuedeReabrir"/>
    /// para las condiciones.</summary>
    public Zafra Reabrir(Zafra cerrada)
    {
        if (!PuedeReabrir(cerrada))
            throw new InvalidOperationException(
                "Solo se puede reabrir la última zafra cerrada, y solo si no hay otra abierta.");

        if (!_sesion.Puede(Permisos.Zafra.Reabrir))
            throw new InvalidOperationException("No tienes permiso para reabrir una zafra.");

        var copia = cerrada.Clonar();
        copia.Estado = EstadoZafra.Abierta;
        copia.FechaCierre = null;
        copia.MotivoCierre = null;
        _zafras.Update(copia);

        ZafraActiva.Fijar(copia);
        return copia;
    }
}
