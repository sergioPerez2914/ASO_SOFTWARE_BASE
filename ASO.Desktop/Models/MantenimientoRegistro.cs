using System;

namespace ASO.Desktop.Models;

/// <summary>Naturaleza del trabajo: programado por intervalo o reparación de una falla.</summary>
public enum TipoMantenimiento
{
    Preventivo,
    Correctivo
}

/// <summary>
/// Constancia de un mantenimiento realizado a un activo de la flota. Es inmutable una vez
/// registrada (como toda constancia de trabajo hecho): no se edita ni se elimina desde la UI.
///
/// Guarda snapshots del activo (código y etiqueta) por el mismo criterio que la remesa: el
/// registro debe leerse igual aunque el catálogo cambie después.
/// </summary>
public class MantenimientoRegistro : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int ActivoId { get; set; }
    public string ActivoCodigo { get; set; } = string.Empty;    // snapshot
    public string ActivoEtiqueta { get; set; } = string.Empty;  // snapshot

    public DateTime Fecha { get; set; }
    public TipoMantenimiento Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Lectura del instrumento al momento del trabajo: horas (máquinas) o km (transporte).</summary>
    public decimal? LecturaUso { get; set; }

    // TODO Inventario: cada línea pasará a ser una salida de inventario cuando exista el módulo.
    public string RepuestosUsados { get; set; } = string.Empty;

    // TODO Finanzas/Inventario: los costos saldrán de las salidas valoradas, no de captura manual.
    public decimal? CostoRepuestos { get; set; }
    public decimal? CostoManoObra { get; set; }

    // TODO Nómina: pasará a EmpleadoId cuando exista el padrón de taller.
    public string RealizadoPor { get; set; } = string.Empty;

    /// <summary>Remesa vinculada (opcional). Al registrarla se publica el evento en su seguimiento.</summary>
    public int? RemesaId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public decimal CostoTotal => (CostoRepuestos ?? 0m) + (CostoManoObra ?? 0m);

    public string TipoTexto => Tipo == TipoMantenimiento.Preventivo ? "Preventivo" : "Correctivo";

    public string LecturaTexto => LecturaUso is { } lectura ? $"{lectura:N0}" : "—";

    public string CostoTotalTexto => CostoRepuestos is null && CostoManoObra is null
        ? "—"
        : CostoTotal.ToString("N2");

    public string RemesaTexto => RemesaId is { } id ? $"Nº {id}" : "—";
}
