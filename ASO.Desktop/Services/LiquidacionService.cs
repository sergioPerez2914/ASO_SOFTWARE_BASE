using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de la nómina. La liquidación no se captura a mano: se genera cruzando lo que ya está
/// registrado en el sistema — remesas confirmadas, jornadas cerradas y el tarifario vigente — y
/// solo después admite ajustes por conceptos (bonos, anticipos).
///
/// Dos reglas mandan sobre todo lo demás:
/// - Solo cuentan las remesas confirmadas o recibidas: un borrador no se paga y una anulada tampoco.
/// - Una remesa no se liquida dos veces. La lista <c>RemesaIdsIncluidas</c> de las liquidaciones
///   no anuladas es el registro de lo ya pagado, y de ahí sale el descarte.
/// </summary>
public sealed class LiquidacionService
{
    private readonly ILiquidacionDataSource _liquidaciones;
    private readonly IRemesaDataSource _remesas;
    private readonly TarifaService _tarifas;
    private readonly HorarioService _horarios;
    private readonly BancoService _banco;

    /// <summary>
    /// El <see cref="BancoService"/> es obligatorio: la nómina es la salida de caja más grande
    /// del centro y no puede pagarse sin dejar rastro en el libro (ver <see cref="Pagar"/>).
    /// </summary>
    public LiquidacionService(ILiquidacionDataSource liquidaciones,
                              IRemesaDataSource remesas,
                              TarifaService tarifas,
                              HorarioService horarios,
                              BancoService banco)
    {
        _liquidaciones = liquidaciones;
        _remesas = remesas;
        _tarifas = tarifas;
        _horarios = horarios;
        _banco = banco;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeEditarLineas(Liquidacion l) => l.Estado == EstadoLiquidacion.Borrador;

    public bool PuedeEliminar(Liquidacion l) => l.Estado == EstadoLiquidacion.Borrador;

    public bool PuedeCerrar(Liquidacion l) => l.Estado == EstadoLiquidacion.Borrador;

    public bool PuedePagar(Liquidacion l) => l.Estado == EstadoLiquidacion.Cerrada;

    public bool PuedeAnular(Liquidacion l) =>
        l.Estado is EstadoLiquidacion.Borrador or EstadoLiquidacion.Cerrada;

    // --- Generación ---

    /// <summary>
    /// Arma el borrador de un núcleo: una línea por cada servicio que prestó en el período
    /// (corte, alza y empuje, transporte), con las toneladas netas de sus remesas por la tarifa
    /// de pago vigente al cierre del período.
    /// </summary>
    public Liquidacion GenerarParaNucleo(string nucleoCodigo, string nucleoNombre,
                                         DateTime desde, DateTime hasta, int creadoPorId)
    {
        ValidarPeriodo(desde, hasta);

        var yaLiquidadas = RemesasYaLiquidadas(SujetoLiquidacion.Nucleo, nucleoCodigo);

        var candidatas = _remesas.GetAll()
            .Where(r => r.Estado is EstadoRemesa.Confirmada or EstadoRemesa.Recibida)
            .Where(r => EnPeriodo(r, desde, hasta))
            .Where(r => !yaLiquidadas.Contains(r.Id))
            .ToList();

        var liquidacion = new Liquidacion
        {
            SujetoTipo = SujetoLiquidacion.Nucleo,
            SujetoCodigo = nucleoCodigo,
            SujetoNombre = nucleoNombre,
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            Estado = EstadoLiquidacion.Borrador,
            CreadoPorId = creadoPorId,
            FechaCreacion = DateTime.Now
        };

        var incluidas = new HashSet<int>();

        foreach (var (servicio, remesasDelServicio) in AgruparPorServicio(candidatas, nucleoCodigo))
        {
            var toneladas = remesasDelServicio.Sum(r => r.PesoNetoT ?? 0m);
            if (toneladas <= 0)
                continue;

            var tarifa = _tarifas.ExigirVigente(servicio, AmbitoTarifa.PagoDestajo, hasta, UnidadTarifa.Tonelada);

            liquidacion.Lineas.Add(new LiquidacionLinea
            {
                Concepto = tarifa.Concepto,
                Origen = OrigenLinea.Destajo,
                Cantidad = toneladas,
                UnidadTexto = "t",
                TarifaMonto = tarifa.MontoPorUnidad,
                Monto = Math.Round(toneladas * tarifa.MontoPorUnidad, 2)
            });

            foreach (var remesa in remesasDelServicio)
                incluidas.Add(remesa.Id);
        }

        if (liquidacion.Lineas.Count == 0)
            throw new InvalidOperationException(
                $"No hay toneladas pendientes de liquidar para {nucleoCodigo} entre el " +
                $"{desde:dd/MM/yyyy} y el {hasta:dd/MM/yyyy}. " +
                "Las remesas sin pesaje registrado todavía no aportan toneladas.");

        liquidacion.RemesaIdsIncluidas = [.. incluidas];
        return _liquidaciones.Add(liquidacion);
    }

    /// <summary>
    /// Arma el borrador de un empleado: las horas cerradas del período por la tarifa horaria
    /// vigente.
    /// </summary>
    public Liquidacion GenerarParaEmpleado(Empleado empleado, DateTime desde, DateTime hasta, int creadoPorId)
    {
        ValidarPeriodo(desde, hasta);

        var horas = _horarios.HorasEnPeriodo(TipoPersonal.Administrativo, empleado.Id, desde, hasta);
        if (horas <= 0)
            throw new InvalidOperationException(
                $"{empleado.Nombre} no tiene jornadas cerradas entre el {desde:dd/MM/yyyy} y el {hasta:dd/MM/yyyy}.");

        var tarifa = _tarifas.ExigirVigente(ServicioZafra.Otro, AmbitoTarifa.PagoDestajo, hasta, UnidadTarifa.Hora);

        var liquidacion = new Liquidacion
        {
            SujetoTipo = SujetoLiquidacion.Empleado,
            SujetoCodigo = empleado.Id.ToString(),
            SujetoNombre = empleado.Nombre,
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            Estado = EstadoLiquidacion.Borrador,
            CreadoPorId = creadoPorId,
            FechaCreacion = DateTime.Now,
            Lineas =
            [
                new LiquidacionLinea
                {
                    Concepto = tarifa.Concepto,
                    Origen = OrigenLinea.Horas,
                    Cantidad = horas,
                    UnidadTexto = "h",
                    TarifaMonto = tarifa.MontoPorUnidad,
                    Monto = Math.Round(horas * tarifa.MontoPorUnidad, 2)
                }
            ]
        };

        return _liquidaciones.Add(liquidacion);
    }

    // --- Ajustes y transiciones ---

    /// <summary>Agrega un bono o una deducción del catálogo de conceptos.</summary>
    public Liquidacion AgregarLineaConcepto(Liquidacion liquidacion, ConceptoNomina concepto, decimal monto)
    {
        if (!PuedeEditarLineas(liquidacion))
            throw new InvalidOperationException("Solo se pueden agregar conceptos a una liquidación en borrador.");

        if (monto <= 0)
            throw new InvalidOperationException("El monto del concepto debe ser mayor que cero.");

        var copia = liquidacion.Clonar();
        copia.Lineas.Add(new LiquidacionLinea
        {
            Concepto = concepto.Nombre,
            Origen = OrigenLinea.Concepto,
            UnidadTexto = "—",
            Monto = monto,
            EsDeduccion = concepto.Tipo == TipoConcepto.Deduccion
        });

        _liquidaciones.Update(copia);
        return copia;
    }

    /// <summary>
    /// Quita una línea agregada a mano. Las líneas calculadas no se borran: si están mal, lo que
    /// está mal es el dato de origen (la remesa o la jornada), y ahí hay que corregirlo.
    /// </summary>
    public Liquidacion QuitarLinea(Liquidacion liquidacion, LiquidacionLinea linea)
    {
        if (!PuedeEditarLineas(liquidacion))
            throw new InvalidOperationException("Solo se pueden quitar líneas de una liquidación en borrador.");

        if (linea.Origen != OrigenLinea.Concepto)
            throw new InvalidOperationException(
                "Las líneas de destajo y de horas se calculan desde las remesas y las jornadas: " +
                "corrija el documento de origen en vez de la liquidación.");

        var copia = liquidacion.Clonar();
        var indice = liquidacion.Lineas.IndexOf(linea);
        if (indice >= 0)
            copia.Lineas.RemoveAt(indice);

        _liquidaciones.Update(copia);
        return copia;
    }

    /// <summary>Cierra la liquidación: a partir de aquí es inmutable y está lista para pago.</summary>
    public Liquidacion Cerrar(Liquidacion liquidacion)
    {
        if (!PuedeCerrar(liquidacion))
            throw new InvalidOperationException("Solo se puede cerrar una liquidación en borrador.");

        if (liquidacion.Lineas.Count == 0)
            throw new InvalidOperationException("La liquidación no tiene líneas que pagar.");

        if (liquidacion.Neto < 0)
            throw new InvalidOperationException(
                $"El neto sería negativo ({liquidacion.Neto:N2}): las deducciones superan lo devengado. " +
                "Revise los anticipos antes de cerrar.");

        var copia = liquidacion.Clonar();
        copia.Estado = EstadoLiquidacion.Cerrada;
        copia.FechaCierre = DateTime.Now;
        _liquidaciones.Update(copia);
        return copia;
    }

    /// <summary>
    /// Paga la liquidación y anota la salida en el libro de banco, en una sola operación. El
    /// monto que sale de caja es el NETO: lo devengado menos las deducciones, que es lo que de
    /// verdad se entrega.
    ///
    /// El asiento va primero porque es el que puede rechazar; ver
    /// <see cref="FacturaClienteService.RegistrarCobro"/>.
    /// </summary>
    public Liquidacion Pagar(Liquidacion liquidacion, AsientoBanco asiento, int usuarioId)
    {
        if (!PuedePagar(liquidacion))
            throw new InvalidOperationException("Solo se puede pagar una liquidación cerrada.");

        _banco.RegistrarPagoLiquidacion(liquidacion, asiento, usuarioId);

        var copia = liquidacion.Clonar();
        copia.Estado = EstadoLiquidacion.Pagada;
        copia.FechaPago = DateTime.Now;
        _liquidaciones.Update(copia);
        return copia;
    }

    /// <summary>
    /// Anula la liquidación. Sus remesas vuelven a quedar liquidables, porque el descarte mira
    /// solo las liquidaciones no anuladas. Una liquidación pagada no se anula: eso sería revertir
    /// un pago ya hecho.
    /// </summary>
    public Liquidacion Anular(Liquidacion liquidacion, string motivo)
    {
        if (!PuedeAnular(liquidacion))
            throw new InvalidOperationException(
                liquidacion.Estado == EstadoLiquidacion.Pagada
                    ? "Una liquidación pagada no se anula: registre el ajuste en el período siguiente."
                    : "Solo se puede anular una liquidación en borrador o cerrada.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo de la anulación.");

        var copia = liquidacion.Clonar();
        copia.Estado = EstadoLiquidacion.Anulada;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _liquidaciones.Update(copia);
        return copia;
    }

    // --- Auxiliares ---

    /// <summary>
    /// Remesas del período agrupadas por el servicio que el núcleo prestó en cada una. Una misma
    /// remesa puede aportar a los tres servicios si el núcleo cortó, alzó y transportó.
    /// </summary>
    private static IEnumerable<(ServicioZafra Servicio, List<Remesa> Remesas)> AgruparPorServicio(
        List<Remesa> remesas, string nucleoCodigo)
    {
        yield return (ServicioZafra.Corte,
            remesas.Where(r => Coincide(r.NucleoCorteCodigo, nucleoCodigo)).ToList());

        yield return (ServicioZafra.AlzaEmpuje,
            remesas.Where(r => Coincide(r.NucleoAlzaEmpujeCodigo, nucleoCodigo)).ToList());

        yield return (ServicioZafra.Transporte,
            remesas.Where(r => Coincide(r.NucleoTransporteCodigo, nucleoCodigo)).ToList());
    }

    private static bool Coincide(string codigoRemesa, string nucleoCodigo) =>
        string.Equals(codigoRemesa?.Trim(), nucleoCodigo?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>La remesa cae en el período por su confirmación; si no la tiene, por su creación.</summary>
    private static bool EnPeriodo(Remesa remesa, DateTime desde, DateTime hasta)
    {
        var fecha = (remesa.FechaConfirmacion ?? remesa.FechaCreacion).Date;
        return fecha >= desde.Date && fecha <= hasta.Date;
    }

    private HashSet<int> RemesasYaLiquidadas(SujetoLiquidacion tipo, string sujetoCodigo) =>
        [.. _liquidaciones.GetAll()
            .Where(l => l.Estado != EstadoLiquidacion.Anulada
                        && l.SujetoTipo == tipo
                        && string.Equals(l.SujetoCodigo, sujetoCodigo, StringComparison.OrdinalIgnoreCase))
            .SelectMany(l => l.RemesaIdsIncluidas)];

    private static void ValidarPeriodo(DateTime desde, DateTime hasta)
    {
        if (hasta.Date < desde.Date)
            throw new InvalidOperationException("La fecha de fin del período no puede ser anterior a la de inicio.");
    }
}
