using System;
using ASO.Desktop.Controls;

namespace ASO.Desktop.Models;

/// <summary>Clase de activo. Camión y Chuto son transporte; el resto son máquinas de campo.</summary>
public enum TipoActivo
{
    Cosechadora,
    Tractor,
    Alzadora,
    Camion,
    Chuto
}

/// <summary>Situación operativa del activo. No es dato de ficha: se cambia por comando.</summary>
public enum EstadoActivo
{
    Operativo,
    EnTaller,
    FueraDeServicio
}

/// <summary>
/// Activo de la flota: máquina de campo (cosechadora, tractor, alzadora) o unidad de transporte
/// (camión, chuto). Es el catálogo único: el combo de unidades de la remesa lo lee filtrando
/// por <see cref="EsTransporte"/>.
///
/// El uso se mide con dos instrumentos distintos: horómetro (horas, máquinas) y odómetro
/// (kilómetros, transporte); por eso son dos campos y no uno.
///
/// Modelo de presentación temporal; se alineará con la entidad de dominio cuando exista la BD.
/// </summary>
public class ActivoFlota : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;      // COS-01, TRA-01, CHU-01…
    public TipoActivo Tipo { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }

    /// <summary>Solo transporte; es el dato que identifica la unidad en la remesa.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>En transporte reproduce el texto que ve el combo de remesas (p. ej. "Chuto Mack + batea cañera").</summary>
    public string Descripcion { get; set; } = string.Empty;

    public decimal? HorometroHoras { get; set; }            // máquinas
    public decimal? OdometroKm { get; set; }                // transporte

    public EstadoActivo Estado { get; set; }
    public string Notas { get; set; } = string.Empty;

    public bool EsTransporte => Tipo is TipoActivo.Camion or TipoActivo.Chuto;

    public string TipoTexto => Tipo switch
    {
        TipoActivo.Cosechadora => "Cosechadora",
        TipoActivo.Tractor => "Tractor",
        TipoActivo.Alzadora => "Alzadora",
        TipoActivo.Camion => "Camión",
        _ => "Chuto"
    };

    public string EstadoTexto => Estado switch
    {
        EstadoActivo.Operativo => "Operativo",
        EstadoActivo.EnTaller => "En taller",
        _ => "Fuera de servicio"
    };

    public string Etiqueta => EsTransporte ? $"{Codigo} · {Placa}" : $"{Codigo} · {Marca} {Modelo}";

    public string UsoTexto => this switch
    {
        { EsTransporte: true, OdometroKm: { } km } => $"{km:N0} km",
        { EsTransporte: false, HorometroHoras: { } h } => $"{h:N0} h",
        _ => "—"
    };

    /// <summary>Glifo que representa el tipo. Ver <see cref="Iconos"/>.</summary>
    public string Glifo => Tipo switch
    {
        TipoActivo.Cosechadora => Iconos.Cosechadora,
        TipoActivo.Tractor => Iconos.Tractor,
        TipoActivo.Alzadora => Iconos.Alzadora,
        TipoActivo.Camion => Iconos.Camion,
        _ => Iconos.Vehiculo
    };

    /// <summary>Copia superficial (solo tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public ActivoFlota Clonar() => (ActivoFlota)MemberwiseClone();
}
