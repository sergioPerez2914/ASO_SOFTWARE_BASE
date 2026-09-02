using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Lo que se cobra por un servicio de una remesa: toneladas × tarifa vigente, con el núcleo que
/// lo prestó. El monto va redondeado igual que en la línea de factura, para que comparar el
/// estimado contra la factura emitida no arroje diferencias de céntimos.
/// </summary>
public readonly record struct CobroDeServicio(ServicioZafra Servicio, string NucleoCodigo,
                                              decimal Toneladas, decimal TarifaMonto)
{
    public decimal Monto => Math.Round(Toneladas * TarifaMonto, 2);
}

/// <summary>
/// Reglas del tarifario. Su método importante es <see cref="ObtenerVigente"/>: es la única
/// puerta por la que Liquidaciones y Cuentas por Cobrar averiguan cuánto vale un servicio en
/// una fecha. Ambos copian el monto devuelto dentro del documento; nunca guardan solo el Id,
/// porque el documento debe poder reimprimirse igual dentro de un año.
///
/// No valida ningún permiso: no tiene transiciones mutadoras propias (<see cref="Validar"/>
/// es validación pura de campos, <see cref="ObtenerVigente"/>/<see cref="ExigirVigente"/> son
/// consultas). El alta/edición real de una <see cref="Tarifa"/> pasa por el CRUD genérico de
/// <c>TarifasViewModel</c>, ya protegido por <c>Tarifas.Crear</c>/<c>Tarifas.Editar</c>. Recibe
/// <see cref="ISesionActual"/> igual que el resto de los servicios de dominio, por consistencia
/// del constructor — a propósito no la usa, para que quede escrito por qué en vez de que alguien
/// la agregue "para completar el patrón" sobre una consulta.
/// </summary>
public sealed class TarifaService
{
    private readonly ITarifaDataSource _tarifas;

    public TarifaService(ITarifaDataSource tarifas, ISesionActual sesion) => _tarifas = tarifas;

    /// <summary>
    /// Tarifa que rige para un servicio y ámbito en la fecha dada. Si hay varias (porque el
    /// tarifario se actualizó a mitad de zafra), gana la de vigencia más reciente.
    /// </summary>
    public Tarifa? ObtenerVigente(ServicioZafra servicio, AmbitoTarifa ambito, DateTime fecha,
                                  UnidadTarifa? unidad = null) =>
        _tarifas.GetVigentes(fecha)
            .Where(t => t.Servicio == servicio && t.Ambito == ambito)
            .Where(t => unidad is null || t.Unidad == unidad)
            .OrderByDescending(t => t.VigenteDesde)
            .FirstOrDefault();

    /// <summary>
    /// Igual que <see cref="ObtenerVigente"/> pero exigiendo resultado: los documentos no
    /// pueden inventarse un monto, así que sin tarifa la operación se detiene con un mensaje
    /// que dice exactamente qué falta configurar.
    /// </summary>
    public Tarifa ExigirVigente(ServicioZafra servicio, AmbitoTarifa ambito, DateTime fecha,
                                UnidadTarifa? unidad = null)
    {
        var tarifa = ObtenerVigente(servicio, ambito, fecha, unidad);
        if (tarifa is null)
        {
            var que = ambito == AmbitoTarifa.Cobro ? "cobro" : "pago por destajo";
            throw new InvalidOperationException(
                $"No hay tarifa de {que} vigente para «{TextoServicio(servicio)}» al {fecha:dd/MM/yyyy}. " +
                "Configúrela en Finanzas · Tarifas.");
        }

        return tarifa;
    }

    /// <summary>
    /// Lo que hay que cobrarle al central por una remesa, servicio por servicio, con la tarifa
    /// vigente a la fecha en que se recibió la caña.
    ///
    /// Vive aquí y no en <c>FacturaClienteService</c> porque tiene dos consumidores: la factura,
    /// que lo convierte en líneas, y el boleto del central, que lo compara contra lo que el
    /// central dice que va a pagar. Dos implementaciones darían dos cifras distintas para lo
    /// mismo, y la comparación dejaría de servir para reclamar.
    /// </summary>
    public IReadOnlyList<CobroDeServicio> CalcularCobroPorServicio(Remesa remesa, decimal toneladas,
                                                                  DateTime fecha) =>
        [.. ServiciosDe(remesa).Select(s => new CobroDeServicio(
            s.Servicio,
            s.Nucleo,
            toneladas,
            ExigirVigente(s.Servicio, AmbitoTarifa.Cobro, fecha, UnidadTarifa.Tonelada).MontoPorUnidad))];

    /// <summary>Los tres servicios de una remesa, cada uno con el núcleo que lo prestó.</summary>
    private static IEnumerable<(ServicioZafra Servicio, string Nucleo)> ServiciosDe(Remesa remesa)
    {
        yield return (ServicioZafra.Corte, remesa.NucleoCorteCodigo);
        yield return (ServicioZafra.AlzaEmpuje, remesa.NucleoAlzaEmpujeCodigo);
        yield return (ServicioZafra.Transporte, remesa.NucleoTransporteCodigo);
    }

    /// <summary>
    /// Valida una tarifa antes de guardarla. Dos tarifas activas del mismo servicio, ámbito y
    /// unidad no pueden solaparse en el tiempo: si lo hicieran, no habría forma de saber cuál
    /// aplicar a un documento de esa fecha.
    /// </summary>
    public bool Validar(Tarifa tarifa, out string? error)
    {
        if (string.IsNullOrWhiteSpace(tarifa.Concepto))
        {
            error = "Indique el concepto de la tarifa.";
            return false;
        }

        if (tarifa.MontoPorUnidad <= 0)
        {
            error = "El monto por unidad debe ser mayor que cero.";
            return false;
        }

        if (tarifa.VigenteHasta is { } hasta && hasta.Date < tarifa.VigenteDesde.Date)
        {
            error = "La fecha de fin de vigencia no puede ser anterior a la de inicio.";
            return false;
        }

        if (tarifa.Activa)
        {
            var solapada = _tarifas.GetAll()
                .Where(t => t.Id != tarifa.Id && t.Activa)
                .Where(t => t.Servicio == tarifa.Servicio
                            && t.Ambito == tarifa.Ambito
                            && t.Unidad == tarifa.Unidad)
                .FirstOrDefault(t => SeSolapan(t, tarifa));

            if (solapada is not null)
            {
                error = $"Ya hay una tarifa activa de {tarifa.AmbitoTexto.ToLowerInvariant()} para " +
                        $"«{tarifa.ServicioTexto}» por {tarifa.UnidadTexto.ToLowerInvariant()} " +
                        $"con vigencia {solapada.VigenciaTexto}. Ciérrela o desactívela antes de crear otra.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool SeSolapan(Tarifa a, Tarifa b)
    {
        var finA = a.VigenteHasta ?? DateTime.MaxValue;
        var finB = b.VigenteHasta ?? DateTime.MaxValue;
        return a.VigenteDesde.Date <= finB.Date && b.VigenteDesde.Date <= finA.Date;
    }

    private static string TextoServicio(ServicioZafra servicio) => servicio switch
    {
        ServicioZafra.Corte => "Corte",
        ServicioZafra.AlzaEmpuje => "Alza y empuje",
        ServicioZafra.Transporte => "Transporte",
        _ => "Otro"
    };
}
