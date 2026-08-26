using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>Un dato de la ficha de un evento: su rótulo y su valor ya formateado.</summary>
public sealed record DatoEvento(string Etiqueta, string Valor);

/// <summary>
/// Arma la línea de tiempo de una remesa fusionando tres orígenes:
///
/// 1. Eventos <b>derivados de la propia remesa</b> (registro, carga, confirmación, llegada,
///    pesaje, anulación). No se almacenan: se calculan en cada consulta, así que siempre reflejan
///    el documento y una remesa creada o confirmada durante la sesión aparece sin más.
/// 2. Eventos <b>derivados de los documentos que la citan</b>: su factura al ingenio, las
///    liquidaciones que la computaron y las peticiones de cambio que se pidieron sobre ella. Es
///    lo que hace que Finanzas, Nómina y la bandeja de Peticiones se vean aquí sin que ninguno de
///    esos módulos tenga que enterarse de que existe Seguimiento.
/// 3. Eventos <b>almacenados</b> en <see cref="IEventoOperacionDataSource"/>: cambios de turno,
///    mantenimientos, notas, ediciones del borrador y la liberación al anular una factura.
///
/// Los derivados llevan <c>Id = 0</c> y nunca pasan por <c>Update</c>/<c>Delete</c>.
///
/// Este servicio solo LEE. Los tres servicios que publican eventos —mantenimiento, horarios y
/// remesas— escriben directamente contra <see cref="IEventoOperacionDataSource"/>; la única
/// escritura que vive aquí es la nota, porque no pertenece a ningún otro módulo.
///
/// Regla de oro: la validación de la nota vive aquí, no en el botón.
/// </summary>
public sealed class SeguimientoService
{
    private readonly IEventoOperacionDataSource _eventos;
    private readonly IFacturaClienteDataSource _facturas;
    private readonly ILiquidacionDataSource _liquidaciones;
    private readonly IPeticionCambioDataSource _peticiones;
    private readonly IJornadaDataSource _jornadas;
    private readonly IMantenimientoRegistroDataSource _mantenimientos;

    public SeguimientoService(IEventoOperacionDataSource eventos,
                              IFacturaClienteDataSource facturas,
                              ILiquidacionDataSource liquidaciones,
                              IPeticionCambioDataSource peticiones,
                              IJornadaDataSource jornadas,
                              IMantenimientoRegistroDataSource mantenimientos)
    {
        _eventos = eventos;
        _facturas = facturas;
        _liquidaciones = liquidaciones;
        _peticiones = peticiones;
        _jornadas = jornadas;
        _mantenimientos = mantenimientos;
    }

    /// <summary>
    /// Historia completa de la remesa en orden ascendente. Con la misma hora desempata
    /// <see cref="EventoOperacion.OrdenCicloVida"/>, no el valor del enum: los tipos nuevos se
    /// declaran al final para no reinterpretar lo ya guardado, pero ocurren en mitad de la
    /// historia.
    /// </summary>
    public IReadOnlyList<EventoOperacion> ObtenerTimeline(Remesa remesa)
        => DerivarDeLaRemesa(remesa)
            .Concat(DerivarDeLaFactura(remesa))
            .Concat(DerivarDeLasLiquidaciones(remesa))
            .Concat(DerivarDeLasPeticiones(remesa))
            .Concat(_eventos.GetByRemesa(remesa.Id))
            .OrderBy(e => e.FechaHora)
            .ThenBy(e => e.OrdenCicloVida)
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

    // ---- Ficha de un evento ----

    /// <summary>
    /// Todos los datos del documento que originó el evento, listos para pintar.
    ///
    /// Un evento derivado se explica con la propia remesa, que ya viene en la mano. Uno almacenado
    /// se resuelve por <see cref="EventoOperacion.OrigenId"/>; si viene nulo —los anteriores a la
    /// Fase 14— se devuelve lo que el evento sabe de sí mismo, que es mejor que nada y nunca
    /// falla.
    /// </summary>
    public IReadOnlyList<DatoEvento> ObtenerDetalle(EventoOperacion evento, Remesa remesa)
    {
        var datos = new List<DatoEvento>
        {
            new("Cuándo", evento.FechaHora.ToString("dd/MM/yyyy HH:mm")),
            new("Remesa", $"Nº {remesa.Id} · {remesa.FincaNombre} · {remesa.UbicacionTexto}")
        };

        datos.AddRange(evento.Tipo switch
        {
            TipoEventoOperacion.CambioTurno => DetalleDeJornada(evento),
            TipoEventoOperacion.Mantenimiento => DetalleDeMantenimiento(evento),
            TipoEventoOperacion.Facturacion or TipoEventoOperacion.Cobro => DetalleDeFactura(evento),
            TipoEventoOperacion.Liquidacion => DetalleDeLiquidacion(evento),
            TipoEventoOperacion.Peticion => DetalleDePeticion(evento),
            _ => DetalleDeLaRemesa(evento, remesa)
        });

        if (evento.Descripcion.Length > 0)
            datos.Add(new DatoEvento("Detalle", evento.Descripcion));

        if (evento.Autor.Length > 0)
            datos.Add(new DatoEvento("Registrado por", evento.Autor));

        return datos;
    }

    // ---- Derivación ----

    private static IEnumerable<EventoOperacion> DerivarDeLaRemesa(Remesa remesa)
    {
        yield return Crear(remesa.Id, TipoEventoOperacion.Registro, remesa.FechaCreacion,
            $"Remesa Nº {remesa.Id} registrada — {remesa.FincaNombre}, {remesa.UbicacionTexto}.");

        yield return Crear(remesa.Id, TipoEventoOperacion.InicioCarga, remesa.InicioCarga,
            $"Comienza la carga en {remesa.UbicacionTexto} (cosecha {remesa.TipoCosechaTexto.ToLowerInvariant()}). " +
            $"Operador {remesa.OperadorNombre}, tractorista {remesa.TractoristaNombre}.");

        yield return Crear(remesa.Id, TipoEventoOperacion.FinCarga, remesa.FinCarga,
            $"Carga completa en la unidad {remesa.VehiculoPlaca}, a cargo de {remesa.ChoferNombre}.");

        if (remesa.FechaConfirmacion is { } confirmacion)
            yield return Crear(remesa.Id, TipoEventoOperacion.Confirmacion, confirmacion,
                "El documento queda inmutable y cuenta para la liquidación.");

        if (remesa.LlegadaCentral is { } llegada)
        {
            yield return Crear(remesa.Id, TipoEventoOperacion.LlegadaCentral, llegada,
                "Llegada a la Pre-Romana del CAM Las Majaguas.");

            if (remesa.PesoNetoT is { } neto)
                yield return Crear(remesa.Id, TipoEventoOperacion.Pesaje, llegada,
                    $"Bruto {remesa.PesoBrutoT:N2} t − tara {remesa.TaraT:N2} t = neto {neto:N2} t.");
        }

        if (remesa.FechaAnulacion is { } anulacion)
            yield return Crear(remesa.Id, TipoEventoOperacion.Anulacion, anulacion,
                $"Motivo: {remesa.MotivoAnulacion}");
    }

    /// <summary>
    /// Emisión y cobro salen de la factura que la remesa señala con <c>FacturaClienteId</c>.
    ///
    /// Si esa factura se anula, el campo vuelve a nulo y estos dos eventos desaparecen: es
    /// coherente con que un derivado refleje el documento tal como está AHORA. Que la remesa
    /// estuvo facturada y dejó de estarlo lo cuenta el evento almacenado que escribe
    /// <c>FacturaClienteService</c> al liberarla, que es el único de los cuatro que no se puede
    /// deducir de ningún campo.
    /// </summary>
    private IEnumerable<EventoOperacion> DerivarDeLaFactura(Remesa remesa)
    {
        if (remesa.FacturaClienteId is not { } facturaId)
            yield break;

        if (_facturas.GetById(facturaId) is not { } factura)
            yield break;

        if (factura.FechaEmision is { } emision)
            yield return Crear(remesa.Id, TipoEventoOperacion.Facturacion, emision,
                $"Incluida en la factura {factura.NumeroTexto} a {factura.ClienteNombre}, " +
                $"por {factura.TotalTexto}.", factura.Id);

        if (factura.FechaCobro is { } cobro)
            yield return Crear(remesa.Id, TipoEventoOperacion.Cobro, cobro,
                $"Cobrada la factura {factura.NumeroTexto}.", factura.Id);
    }

    /// <summary>
    /// Liquidaciones que computaron esta remesa. El filtrado es en memoria porque
    /// <c>RemesaIdsIncluidas</c> se guarda como texto separado por comas (ver el value converter
    /// de <c>AsoDbContext</c>) y no es consultable en SQL. A la escala de un núcleo sale barato;
    /// si algún día pesa, el sitio es un <c>GetByRemesa</c> en <see cref="ILiquidacionDataSource"/>.
    /// </summary>
    private IEnumerable<EventoOperacion> DerivarDeLasLiquidaciones(Remesa remesa)
    {
        foreach (var liquidacion in _liquidaciones.GetAll()
                     .Where(l => l.RemesaIdsIncluidas.Contains(remesa.Id)))
        {
            yield return Crear(remesa.Id, TipoEventoOperacion.Liquidacion, liquidacion.FechaCreacion,
                $"Computada en la liquidación Nº {liquidacion.Id} de {liquidacion.SujetoTexto} " +
                $"({liquidacion.PeriodoTexto}).", liquidacion.Id);

            if (liquidacion.FechaCierre is { } cierre)
                yield return Crear(remesa.Id, TipoEventoOperacion.Liquidacion, cierre,
                    $"Cerrada la liquidación Nº {liquidacion.Id}, por {liquidacion.NetoTexto}.",
                    liquidacion.Id);

            if (liquidacion.FechaPago is { } pago)
                yield return Crear(remesa.Id, TipoEventoOperacion.Liquidacion, pago,
                    $"Pagada la liquidación Nº {liquidacion.Id}.", liquidacion.Id);

            if (liquidacion.FechaAnulacion is { } anulacion)
                yield return Crear(remesa.Id, TipoEventoOperacion.Liquidacion, anulacion,
                    $"Anulada la liquidación Nº {liquidacion.Id}: la remesa vuelve a ser liquidable.",
                    liquidacion.Id);
        }
    }

    /// <summary>
    /// Lo que alguien pidió hacer con esta remesa sin tener permiso, y cómo se resolvió.
    /// <c>PeticionCambio</c> guarda el tipo de entidad y su Id como texto, así que la remesa se
    /// reconoce sin depender de cómo esté redactada la descripción.
    /// </summary>
    private IEnumerable<EventoOperacion> DerivarDeLasPeticiones(Remesa remesa)
    {
        var suyas = _peticiones.GetAll()
            .Where(p => p.TipoEntidad == nameof(Remesa) && p.EntidadId == remesa.Id.ToString());

        foreach (var peticion in suyas)
        {
            yield return Crear(remesa.Id, TipoEventoOperacion.Peticion, peticion.SolicitadoEn,
                $"{peticion.SolicitadoPorNombre} solicita: {peticion.Accion.ToLowerInvariant()}. " +
                $"Motivo: {peticion.Motivo}", peticion.Id);

            if (peticion.ResueltoEn is { } resuelto)
                yield return Crear(remesa.Id, TipoEventoOperacion.Peticion, resuelto,
                    $"{peticion.ResueltoPorNombre} la resuelve como {peticion.EstadoTexto.ToLowerInvariant()}.",
                    peticion.Id);
        }
    }

    private static EventoOperacion Crear(int remesaId,
                                         TipoEventoOperacion tipo,
                                         DateTime fechaHora,
                                         string descripcion,
                                         int? origenId = null)
        => new()
        {
            RemesaId = remesaId,
            Tipo = tipo,
            FechaHora = fechaHora,
            Descripcion = descripcion,
            OrigenId = origenId
        };

    // ---- Resolución de la ficha ----

    private IEnumerable<DatoEvento> DetalleDeJornada(EventoOperacion evento)
    {
        if (evento.OrigenId is not { } id || _jornadas.GetById(id) is not { } jornada)
            yield break;

        yield return new DatoEvento("Persona", jornada.PersonaNombre);
        yield return new DatoEvento("Cargo o rol", jornada.CargoORol);
        yield return new DatoEvento("Padrón", jornada.TipoPersonalTexto);
        yield return new DatoEvento("Núcleo", jornada.NucleoCodigo);
        yield return new DatoEvento("Turno", jornada.TurnoTexto);
        yield return new DatoEvento("Entrada", jornada.EntradaTexto);
        yield return new DatoEvento("Salida", jornada.SalidaTexto);
        yield return new DatoEvento("Horas trabajadas", jornada.HorasTexto);

        if (jornada.Observacion.Length > 0)
            yield return new DatoEvento("Observación", jornada.Observacion);
    }

    private IEnumerable<DatoEvento> DetalleDeMantenimiento(EventoOperacion evento)
    {
        if (evento.OrigenId is not { } id || _mantenimientos.GetById(id) is not { } registro)
            yield break;

        yield return new DatoEvento("Activo", registro.ActivoEtiqueta);
        yield return new DatoEvento("Código", registro.ActivoCodigo);
        yield return new DatoEvento("Tipo", registro.TipoTexto);
        yield return new DatoEvento("Trabajo", registro.Descripcion);

        if (registro.RepuestosUsados.Length > 0)
            yield return new DatoEvento("Repuestos", registro.RepuestosUsados);

        yield return new DatoEvento("Realizado por", registro.RealizadoPor);
        yield return new DatoEvento("Lectura de uso", registro.LecturaTexto);
        yield return new DatoEvento("Costo total", registro.CostoTotalTexto);
    }

    private IEnumerable<DatoEvento> DetalleDeFactura(EventoOperacion evento)
    {
        if (evento.OrigenId is not { } id || _facturas.GetById(id) is not { } factura)
            yield break;

        yield return new DatoEvento("Factura", factura.NumeroTexto);
        yield return new DatoEvento("Cliente", factura.ClienteNombre);
        yield return new DatoEvento("Estado", factura.EstadoTexto);
        yield return new DatoEvento("Emisión", factura.EmisionTexto);
        yield return new DatoEvento("Vencimiento", factura.VencimientoTexto);
        yield return new DatoEvento("Remesas incluidas", factura.RemesasTexto);
        yield return new DatoEvento("Total", factura.TotalTexto);
    }

    private IEnumerable<DatoEvento> DetalleDeLiquidacion(EventoOperacion evento)
    {
        if (evento.OrigenId is not { } id || _liquidaciones.GetById(id) is not { } liquidacion)
            yield break;

        yield return new DatoEvento("Liquidación", $"Nº {liquidacion.Id}");
        yield return new DatoEvento("Sujeto", liquidacion.SujetoTexto);
        yield return new DatoEvento("Período", liquidacion.PeriodoTexto);
        yield return new DatoEvento("Estado", liquidacion.EstadoTexto);
        yield return new DatoEvento("Remesas computadas", liquidacion.RemesaIdsIncluidas.Count.ToString());
        yield return new DatoEvento("Neto", liquidacion.NetoTexto);
    }

    private IEnumerable<DatoEvento> DetalleDePeticion(EventoOperacion evento)
    {
        if (evento.OrigenId is not { } id || _peticiones.GetById(id) is not { } peticion)
            yield break;

        yield return new DatoEvento("Acción pedida", peticion.Accion);
        yield return new DatoEvento("Permiso", peticion.Permiso);
        yield return new DatoEvento("Solicitada por", peticion.SolicitadoPorNombre);
        yield return new DatoEvento("Motivo", peticion.Motivo);
        yield return new DatoEvento("Estado", peticion.EstadoTexto);

        if (peticion.ResueltoEn is { } resuelto)
        {
            yield return new DatoEvento("Resuelta por", peticion.ResueltoPorNombre);
            yield return new DatoEvento("Resuelta el", resuelto.ToString("dd/MM/yyyy HH:mm"));

            if (peticion.ComentarioResolucion.Length > 0)
                yield return new DatoEvento("Comentario", peticion.ComentarioResolucion);
        }
    }

    /// <summary>
    /// Los eventos del propio documento se explican con la remesa. Se enseña el bloque que cada
    /// uno protagoniza, no la ficha entera: en la carga interesa quién la hizo, en el pesaje
    /// interesan los kilos.
    /// </summary>
    private static IEnumerable<DatoEvento> DetalleDeLaRemesa(EventoOperacion evento, Remesa remesa)
    {
        yield return new DatoEvento("Estado actual", remesa.EstadoTexto);
        yield return new DatoEvento("Finca", $"{remesa.FincaCodigoCam} · {remesa.FincaNombre}");
        yield return new DatoEvento("Ubicación", remesa.UbicacionTexto);

        switch (evento.Tipo)
        {
            case TipoEventoOperacion.InicioCarga:
            case TipoEventoOperacion.FinCarga:
                yield return new DatoEvento("Tipo de cosecha", remesa.TipoCosechaTexto);
                yield return new DatoEvento("Operador", remesa.OperadorNombre);
                yield return new DatoEvento("Tractorista", remesa.TractoristaNombre);
                yield return new DatoEvento("Chofer", remesa.ChoferNombre);
                yield return new DatoEvento("Unidad", remesa.VehiculoPlaca);
                yield return new DatoEvento("Inicio de carga", remesa.InicioCarga.ToString("dd/MM/yyyy HH:mm"));
                yield return new DatoEvento("Fin de carga", remesa.FinCarga.ToString("dd/MM/yyyy HH:mm"));
                break;

            case TipoEventoOperacion.LlegadaCentral:
            case TipoEventoOperacion.Pesaje:
                yield return new DatoEvento("Unidad", remesa.VehiculoPlaca);
                yield return new DatoEvento("Chofer", remesa.ChoferNombre);
                yield return new DatoEvento("Peso bruto", Toneladas(remesa.PesoBrutoT));
                yield return new DatoEvento("Tara", Toneladas(remesa.TaraT));
                yield return new DatoEvento("Peso neto", Toneladas(remesa.PesoNetoT));
                break;

            case TipoEventoOperacion.Anulacion:
                yield return new DatoEvento("Motivo de la anulación", remesa.MotivoAnulacion ?? "—");
                break;

            default:
                yield return new DatoEvento("Remesero", remesa.RemeseroNombre);
                yield return new DatoEvento("Núcleo de corte", remesa.NucleoCorteCodigo);
                yield return new DatoEvento("Núcleo de alza y empuje", remesa.NucleoAlzaEmpujeCodigo);
                yield return new DatoEvento("Núcleo de transporte", remesa.NucleoTransporteCodigo);
                yield return new DatoEvento("Facturación", remesa.FacturadaTexto);
                break;
        }
    }

    private static string Toneladas(decimal? valor) => valor is { } v ? $"{v:N2} t" : "—";
}
