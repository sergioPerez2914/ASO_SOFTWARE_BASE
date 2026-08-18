using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de negocio del catálogo de flota. Regla de oro: las validaciones viven aquí, no en
/// los botones. Las transiciones devuelven una copia actualizada porque los modelos no
/// notifican cambios (la lista reemplaza el elemento).
/// </summary>
public sealed class FlotaService
{
    private readonly IActivoFlotaDataSource _activos;
    private readonly IRemesaDataSource _remesas;
    private readonly IMantenimientoRegistroDataSource _mantenimientos;

    public FlotaService(IActivoFlotaDataSource activos,
                        IRemesaDataSource remesas,
                        IMantenimientoRegistroDataSource mantenimientos)
    {
        _activos = activos;
        _remesas = remesas;
        _mantenimientos = mantenimientos;
    }

    public ActivoFlota Agregar(ActivoFlota activo)
    {
        Validar(activo);
        return _activos.Add(activo);
    }

    public ActivoFlota Actualizar(ActivoFlota activo)
    {
        Validar(activo);
        _activos.Update(activo);
        return activo;
    }

    /// <summary>El estado es situación operativa, no dato de ficha: se cambia por comando.</summary>
    public ActivoFlota CambiarEstado(ActivoFlota activo, EstadoActivo nuevoEstado)
    {
        if (activo.Estado == nuevoEstado)
            throw new InvalidOperationException($"El activo ya está {activo.EstadoTexto.ToLowerInvariant()}.");

        var actualizado = activo.Clonar();
        actualizado.Estado = nuevoEstado;
        _activos.Update(actualizado);
        return actualizado;
    }

    /// <summary>
    /// Historial de uso, descendente por fecha. Transporte: derivado de las remesas de
    /// Operaciones (incluidas las anuladas: también son historia de la unidad). Máquinas:
    /// lecturas de horómetro capturadas con los mantenimientos — el uso real llegará con
    /// Telemetría y los vales de combustible.
    /// </summary>
    public IReadOnlyList<UsoActivoItem> ObtenerHistorialUso(ActivoFlota activo)
    {
        var items = activo.EsTransporte
            ? _remesas.GetAll()
                .Where(r => r.VehiculoId == activo.Id)
                .Select(r => new UsoActivoItem
                {
                    Fecha = r.InicioCarga,
                    Titulo = $"Remesa Nº {r.Id} — {r.FincaNombre}",
                    Detalle = $"Carga {FormatearDuracion(r.FinCarga - r.InicioCarga)}" +
                              (r.PesoNetoT is { } neto ? $" · Neto {neto:N2} t" : string.Empty),
                    Remesa = r
                })
            : _mantenimientos.GetByActivo(activo.Id)
                .Where(m => m.LecturaUso is not null)
                .Select(m => new UsoActivoItem
                {
                    Fecha = m.Fecha,
                    Titulo = "Lectura de horómetro",
                    Detalle = $"{m.LecturaUso:N0} h — registrada con mantenimiento {m.TipoTexto.ToLowerInvariant()}"
                });

        return [.. items.OrderByDescending(i => i.Fecha)];
    }

    private void Validar(ActivoFlota activo)
    {
        if (string.IsNullOrWhiteSpace(activo.Codigo))
            throw new InvalidOperationException("El activo debe tener un código interno.");

        var duplicado = _activos.GetAll().Any(a =>
            a.Id != activo.Id &&
            string.Equals(a.Codigo, activo.Codigo, StringComparison.OrdinalIgnoreCase));
        if (duplicado)
            throw new InvalidOperationException($"Ya existe un activo con el código {activo.Codigo}.");

        if (activo.EsTransporte && string.IsNullOrWhiteSpace(activo.Placa))
            throw new InvalidOperationException("Una unidad de transporte debe tener placa.");

        if (activo.Anio != 0 && (activo.Anio < 1980 || activo.Anio > DateTime.Today.Year + 1))
            throw new InvalidOperationException($"El año debe estar entre 1980 y {DateTime.Today.Year + 1}.");
    }

    private static string FormatearDuracion(TimeSpan duracion)
        => $"{(int)duracion.TotalHours} h {duracion.Minutes:D2} min";
}

/// <summary>Fila del historial de uso de un activo. <see cref="Remesa"/> viene solo en transporte.</summary>
public sealed class UsoActivoItem
{
    public required DateTime Fecha { get; init; }
    public required string Titulo { get; init; }
    public required string Detalle { get; init; }
    public Remesa? Remesa { get; init; }

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
    public bool TieneRemesa => Remesa is not null;
}
