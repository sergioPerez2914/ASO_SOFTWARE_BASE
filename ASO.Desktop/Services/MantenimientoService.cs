using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de negocio del mantenimiento de flota: registro de trabajos realizados y cálculo de
/// recomendaciones de revisión por intervalos.
///
/// <see cref="Registrar"/> es el ÚNICO punto de escritura (regla de oro): valida, persiste con
/// snapshots, actualiza la lectura del activo y, si el trabajo está vinculado a una remesa,
/// publica el evento de Mantenimiento en su línea de tiempo de Seguimiento — así lo que se
/// registra en Flota se refleja en Operaciones sin tocar ese módulo.
/// </summary>
public sealed class MantenimientoService
{
    /// <summary>"Próximo" = 90 % del intervalo consumido.</summary>
    private const decimal UmbralProximo = 0.90m;

    private readonly IMantenimientoRegistroDataSource _registros;
    private readonly IActivoFlotaDataSource _activos;
    private readonly IReglaMantenimientoDataSource _reglas;
    private readonly IEventoOperacionDataSource _eventos;
    private readonly IRemesaDataSource _remesas;
    private readonly ISesionActual _sesion;

    public MantenimientoService(IMantenimientoRegistroDataSource registros,
                                IActivoFlotaDataSource activos,
                                IReglaMantenimientoDataSource reglas,
                                IEventoOperacionDataSource eventos,
                                IRemesaDataSource remesas,
                                ISesionActual sesion)
    {
        _registros = registros;
        _activos = activos;
        _reglas = reglas;
        _eventos = eventos;
        _remesas = remesas;
        _sesion = sesion;
    }

    public MantenimientoRegistro Registrar(MantenimientoRegistro registro)
    {
        if (!_sesion.Puede(Permisos.Mantenimiento.Registrar))
            throw new InvalidOperationException("No tienes permiso para registrar mantenimientos.");

        var activo = _activos.GetById(registro.ActivoId)
            ?? throw new InvalidOperationException("El activo indicado no existe.");

        if (string.IsNullOrWhiteSpace(registro.Descripcion))
            throw new InvalidOperationException("Describa el trabajo realizado.");

        if (registro.Fecha > DateTime.Now)
            throw new InvalidOperationException("La fecha del mantenimiento no puede ser futura.");

        if (registro.CostoRepuestos is < 0 || registro.CostoManoObra is < 0)
            throw new InvalidOperationException("Los costos no pueden ser negativos.");

        var lecturaActual = activo.EsTransporte ? activo.OdometroKm : activo.HorometroHoras;
        if (registro.LecturaUso is { } lectura && lecturaActual is { } actual && lectura < actual)
            throw new InvalidOperationException(
                $"La lectura ({lectura:N0}) no puede ser menor que la actual del activo ({actual:N0}).");

        if (registro.RemesaId is { } remesaId)
        {
            var remesa = _remesas.GetById(remesaId)
                ?? throw new InvalidOperationException("La remesa vinculada no existe.");

            if (activo.EsTransporte && remesa.VehiculoId != activo.Id)
                throw new InvalidOperationException(
                    $"La remesa Nº {remesaId} no corresponde a la unidad {activo.Etiqueta}.");
        }

        // 1. Persistir la constancia con sus snapshots.
        registro.ActivoCodigo = activo.Codigo;
        registro.ActivoEtiqueta = activo.Etiqueta;
        registro.FechaRegistro = DateTime.Now;
        var guardado = _registros.Add(registro);

        // 2. Actualizar la lectura del activo si el trabajo trajo una.
        if (registro.LecturaUso is { } nuevaLectura)
        {
            var actualizado = activo.Clonar();
            if (activo.EsTransporte)
                actualizado.OdometroKm = nuevaLectura;
            else
                actualizado.HorometroHoras = nuevaLectura;
            _activos.Update(actualizado);
        }

        // 3. Publicar el evento en el seguimiento de la remesa vinculada. Lleva el Id del
        //    registro guardado para que la ficha del evento pueda abrir el trabajo completo.
        if (registro.RemesaId is { } idRemesa)
        {
            _eventos.Add(new EventoOperacion
            {
                RemesaId = idRemesa,
                Tipo = TipoEventoOperacion.Mantenimiento,
                FechaHora = registro.Fecha,
                Descripcion = $"{registro.TipoTexto} de {registro.ActivoEtiqueta}: {registro.Descripcion}",
                Autor = guardado.RealizadoPor,
                OrigenId = guardado.Id
            });
        }

        return guardado;
    }

    public IReadOnlyList<MantenimientoRegistro> ObtenerPorActivo(int activoId)
        => [.. _registros.GetByActivo(activoId).OrderByDescending(r => r.Fecha)];

    public MantenimientoRegistro? UltimoDe(int activoId)
        => _registros.GetByActivo(activoId).MaxBy(r => r.Fecha);

    /// <summary>Recomendaciones de toda la flota, las más urgentes primero.</summary>
    public IReadOnlyList<RecomendacionMantenimiento> CalcularRecomendaciones()
        => [.. _activos.GetAll()
                .SelectMany(CalcularParaActivo)
                .OrderBy(r => r.Estado)
                .ThenByDescending(r => r.Avance)];

    public IReadOnlyList<RecomendacionMantenimiento> CalcularParaActivo(ActivoFlota activo)
        => [.. _reglas.GetByTipo(activo.Tipo)
                .Select(regla => Evaluar(activo, regla))
                .OrderBy(r => r.Estado)
                .ThenByDescending(r => r.Avance)];

    private RecomendacionMantenimiento Evaluar(ActivoFlota activo, ReglaMantenimiento regla)
    {
        var historial = _registros.GetByActivo(activo.Id).ToList();

        if (historial.Count == 0)
        {
            return new RecomendacionMantenimiento
            {
                Activo = activo, Regla = regla,
                Estado = EstadoRecomendacion.Vencido,
                Detalle = "Sin mantenimientos registrados.",
                Avance = 1m
            };
        }

        // Base de días: el último registro. Base de horas: la última lectura capturada.
        var fechaBase = historial.Max(r => r.Fecha);
        var lecturaBase = historial.Where(r => r.LecturaUso is not null)
                                   .MaxBy(r => r.Fecha)?.LecturaUso;

        decimal? avanceHoras = null;
        string? detalleHoras = null;
        if (regla.IntervaloHoras is { } intervaloHoras && !activo.EsTransporte
            && activo.HorometroHoras is { } horasActuales && lecturaBase is { } lecturaHoras)
        {
            var transcurridas = horasActuales - lecturaHoras;
            avanceHoras = transcurridas / intervaloHoras;
            detalleHoras = $"Hace {transcurridas:N0} h del último registro (intervalo {intervaloHoras:N0} h).";
        }

        decimal? avanceDias = null;
        string? detalleDias = null;
        if (regla.IntervaloDias is { } intervaloDias)
        {
            var dias = (DateTime.Today - fechaBase.Date).Days;
            avanceDias = (decimal)dias / intervaloDias;
            detalleDias = $"Hace {dias} días del último registro (intervalo {intervaloDias} días).";
        }

        // Rige el intervalo que se consuma primero.
        var (avance, detalle) = (avanceHoras ?? -1m) >= (avanceDias ?? -1m)
            ? (avanceHoras, detalleHoras)
            : (avanceDias, detalleDias);

        if (avance is null)
        {
            // La regla no aplica con los datos disponibles (p. ej. horas sin lectura registrada).
            return new RecomendacionMantenimiento
            {
                Activo = activo, Regla = regla,
                Estado = EstadoRecomendacion.Vencido,
                Detalle = "Sin lecturas de uso para evaluar el intervalo.",
                Avance = 1m
            };
        }

        var estado = avance >= 1m ? EstadoRecomendacion.Vencido
                   : avance >= UmbralProximo ? EstadoRecomendacion.Proximo
                   : EstadoRecomendacion.AlDia;

        return new RecomendacionMantenimiento
        {
            Activo = activo, Regla = regla,
            Estado = estado, Detalle = detalle!, Avance = avance.Value
        };
    }
}
