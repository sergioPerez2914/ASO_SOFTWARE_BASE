using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas del despacho de combustible. Mismo contrato que <see cref="RemesaService"/>: los
/// <c>PuedeX</c> alimentan el <c>CanExecute</c> y las transiciones revalidan antes de aplicar
/// efectos.
///
/// Confirmar un vale toca tres cosas a la vez: descuenta la cisterna, calcula el rendimiento
/// del período y adelanta la lectura del instrumento del activo. Ese último efecto convierte
/// al vale en la fuente principal del horómetro y el odómetro: hasta ahora solo se actualizaban
/// cuando alguien registraba un mantenimiento, que es mucho menos frecuente que un despacho.
/// </summary>
public sealed class CombustibleService
{
    private readonly IValeCombustibleDataSource _vales;
    private readonly ITanqueCombustibleDataSource _tanques;
    private readonly IRecargaCombustibleDataSource _recargas;
    private readonly IActivoFlotaDataSource _activos;

    public CombustibleService(IValeCombustibleDataSource vales,
                              ITanqueCombustibleDataSource tanques,
                              IRecargaCombustibleDataSource recargas,
                              IActivoFlotaDataSource activos)
    {
        _vales = vales;
        _tanques = tanques;
        _recargas = recargas;
        _activos = activos;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeEditar(ValeCombustible v) => v.Estado == EstadoVale.Borrador;

    public bool PuedeEliminar(ValeCombustible v) => v.Estado == EstadoVale.Borrador;

    public bool PuedeConfirmar(ValeCombustible v) => v.Estado == EstadoVale.Borrador;

    public bool PuedeAnular(ValeCombustible v) =>
        v.Estado is EstadoVale.Borrador or EstadoVale.Confirmado;

    public static bool EstaCompleto(ValeCombustible v, out string? faltantes)
    {
        var falta = new List<string>();

        if (v.TanqueId == 0)
            falta.Add("la cisterna de origen");

        if (v.ActivoId == 0)
            falta.Add("el activo que recibe el combustible");

        if (v.Litros <= 0)
            falta.Add("los litros despachados");

        if (v.Lectura is null)
            falta.Add(v.EsTransporte ? "la lectura del odómetro" : "la lectura del horómetro");

        faltantes = falta.Count == 0 ? null : string.Join(", ", falta);
        return falta.Count == 0;
    }

    // --- Transiciones ---

    /// <summary>
    /// Confirma el despacho: descuenta la cisterna, calcula el rendimiento y adelanta la
    /// lectura del activo. A partir de aquí el vale es inmutable.
    /// </summary>
    public ValeCombustible Confirmar(ValeCombustible vale)
    {
        if (!PuedeConfirmar(vale))
            throw new InvalidOperationException("Solo se puede confirmar un vale en borrador.");

        if (!EstaCompleto(vale, out var faltantes))
            throw new InvalidOperationException($"Faltan datos para confirmar el vale: {faltantes}.");

        var tanque = _tanques.GetById(vale.TanqueId)
            ?? throw new InvalidOperationException("La cisterna indicada ya no existe.");

        if (vale.Litros > tanque.ExistenciaL)
            throw new InvalidOperationException(
                $"La {tanque.Nombre.ToLowerInvariant()} tiene {tanque.ExistenciaL:N2} L y se piden {vale.Litros:N2} L. " +
                "Registre una recarga antes de despachar.");

        var activo = _activos.GetById(vale.ActivoId)
            ?? throw new InvalidOperationException("El activo indicado ya no existe en la flota.");

        var lecturaAnterior = LecturaActual(activo);
        if (vale.Lectura is { } lectura && lecturaAnterior is { } anterior && lectura < anterior)
            throw new InvalidOperationException(
                $"La lectura ({lectura:N0} {vale.UnidadLectura}) es menor que la última registrada del activo " +
                $"({anterior:N0} {vale.UnidadLectura}). Verifique el instrumento.");

        var copia = vale.Clonar();

        // Rendimiento del período: litros entre lo recorrido/trabajado desde el último despacho.
        var referencia = UltimaLecturaConfirmada(vale.ActivoId) ?? lecturaAnterior;
        var recorrido = vale.Lectura - referencia;
        copia.ConsumoPorUnidad = recorrido is > 0 ? vale.Litros / recorrido.Value : null;

        copia.PromedioHistorico = PromedioHistorico(vale.ActivoId);
        copia.AlertaConsumo = copia.ConsumoPorUnidad is { } consumo
                              && copia.PromedioHistorico is { } promedio and > 0
                              && consumo > promedio * (1 + Ajustes.UmbralAlertaConsumoEfectivo);

        copia.Estado = EstadoVale.Confirmado;
        copia.FechaConfirmacion = DateTime.Now;
        _vales.Update(copia);

        // Efecto: descontar la cisterna.
        var tanqueActualizado = tanque.Clonar();
        tanqueActualizado.ExistenciaL -= vale.Litros;
        _tanques.Update(tanqueActualizado);

        // Efecto: adelantar la lectura del instrumento del activo.
        if (copia.Lectura is { } nueva && (lecturaAnterior is null || nueva > lecturaAnterior))
        {
            var activoActualizado = activo.Clonar();
            if (activo.EsTransporte)
                activoActualizado.OdometroKm = nueva;
            else
                activoActualizado.HorometroHoras = nueva;

            _activos.Update(activoActualizado);
        }

        return copia;
    }

    /// <summary>
    /// Anula el vale. Si estaba confirmado repone los litros a la cisterna, pero NO revierte la
    /// lectura del activo: el instrumento marca lo que marca, y anular un papel no deshace las
    /// horas que la máquina trabajó.
    /// </summary>
    public ValeCombustible Anular(ValeCombustible vale, string motivo)
    {
        if (!PuedeAnular(vale))
            throw new InvalidOperationException("Solo se puede anular un vale en borrador o confirmado.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo de la anulación.");

        if (vale.Estado == EstadoVale.Confirmado && _tanques.GetById(vale.TanqueId) is { } tanque)
        {
            var repuesto = tanque.Clonar();
            repuesto.ExistenciaL += vale.Litros;
            _tanques.Update(repuesto);
        }

        var copia = vale.Clonar();
        copia.Estado = EstadoVale.Anulado;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _vales.Update(copia);

        return copia;
    }

    /// <summary>Registra una recarga y suma sus litros a la cisterna en la misma operación.</summary>
    public RecargaCombustible RegistrarRecarga(RecargaCombustible recarga)
    {
        if (recarga.TanqueId == 0)
            throw new InvalidOperationException("Seleccione la cisterna que se recarga.");

        if (recarga.Litros <= 0)
            throw new InvalidOperationException("Los litros de la recarga deben ser mayores que cero.");

        var tanque = _tanques.GetById(recarga.TanqueId)
            ?? throw new InvalidOperationException("La cisterna indicada ya no existe.");

        if (tanque.ExistenciaL + recarga.Litros > tanque.CapacidadL)
            throw new InvalidOperationException(
                $"La {tanque.Nombre.ToLowerInvariant()} tiene {tanque.ExistenciaL:N2} L de {tanque.CapacidadL:N2} L " +
                $"y no admite {recarga.Litros:N2} L más. Verifique la cantidad recibida.");

        recarga.TanqueNombre = tanque.Nombre;
        var agregada = _recargas.Add(recarga);

        var actualizado = tanque.Clonar();
        actualizado.ExistenciaL += recarga.Litros;
        _tanques.Update(actualizado);

        return agregada;
    }

    // --- Consultas de rendimiento ---

    /// <summary>
    /// Litros por tonelada del centro en los últimos días: cruza lo despachado contra la caña
    /// efectivamente recibida en el central.
    ///
    /// PROVISIONAL: es un indicador global. El desglose por frente o por máquina necesita
    /// atribuir cada vale a las remesas de ese activo, que es lo que traerá Telemetría.
    /// </summary>
    public decimal? LitrosPorTonelada(IRemesaDataSource remesas, int dias = 7)
    {
        var desde = DateTime.Today.AddDays(-dias);

        var litros = _vales.GetAll()
            .Where(v => v.Estado == EstadoVale.Confirmado && v.Fecha >= desde)
            .Sum(v => v.Litros);

        var toneladas = remesas.GetAll()
            .Where(r => r.Estado == EstadoRemesa.Recibida && (r.LlegadaCentral ?? r.FechaConfirmacion) >= desde)
            .Sum(r => r.PesoNetoT ?? 0m);

        return toneladas > 0 ? litros / toneladas : null;
    }

    private decimal? LecturaActual(ActivoFlota activo) =>
        activo.EsTransporte ? activo.OdometroKm : activo.HorometroHoras;

    private decimal? UltimaLecturaConfirmada(int activoId) =>
        _vales.GetByActivo(activoId)
            .Where(v => v.Estado == EstadoVale.Confirmado && v.Lectura is not null)
            .OrderByDescending(v => v.Fecha)
            .Select(v => v.Lectura)
            .FirstOrDefault();

    private decimal? PromedioHistorico(int activoId)
    {
        var consumos = _vales.GetByActivo(activoId)
            .Where(v => v.Estado == EstadoVale.Confirmado && v.ConsumoPorUnidad is > 0)
            .Select(v => v.ConsumoPorUnidad!.Value)
            .ToList();

        return consumos.Count == 0 ? null : consumos.Average();
    }
}
