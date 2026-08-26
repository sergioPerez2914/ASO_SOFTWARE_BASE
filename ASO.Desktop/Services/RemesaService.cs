using System;
using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Reglas de negocio del documento Remesa: máquina de estados
/// <c>Borrador → Confirmada → Recibida</c>, con rama <c>Anulada</c>.
///
/// Regla de oro: las transiciones se validan aquí, no en el evento del botón. Los métodos
/// <c>PuedeX</c> alimentan los <c>CanExecute</c> (cortesía visual) y las transiciones vuelven
/// a validar y lanzan si el estado no lo permite (defensa en profundidad).
///
/// Cada transición devuelve una copia actualizada: los modelos no implementan INotifyPropertyChanged,
/// así que la lista debe reemplazar el elemento para que la grilla se entere.
/// </summary>
public sealed class RemesaService
{
    private readonly IRemesaDataSource _source;
    private readonly IEventoOperacionDataSource _eventos;

    public RemesaService(IRemesaDataSource source, IEventoOperacionDataSource eventos)
    {
        _source = source;
        _eventos = eventos;
    }

    public bool PuedeEditar(Remesa remesa) => remesa.Estado == EstadoRemesa.Borrador;

    public bool PuedeEliminar(Remesa remesa) => remesa.Estado == EstadoRemesa.Borrador;

    public bool PuedeConfirmar(Remesa remesa) => remesa.Estado == EstadoRemesa.Borrador;

    public bool PuedeAnular(Remesa remesa)
        => remesa.Estado is EstadoRemesa.Borrador or EstadoRemesa.Confirmada;

    public bool PuedeRegistrarRecepcion(Remesa remesa) => remesa.Estado == EstadoRemesa.Confirmada;

    /// <summary>
    /// Guarda una edición del borrador dejando constancia de QUÉ cambió.
    ///
    /// Hasta ahora editar se escapaba por el CRUD genérico y no dejaba rastro ninguno: la remesa
    /// no guarda fecha de modificación ni quién la tocó, así que sin este evento no había forma
    /// de saber que el documento que se confirmó no era el que se registró.
    /// </summary>
    public Remesa Editar(Remesa original, Remesa editada, string autor)
    {
        if (!PuedeEditar(original))
            throw new InvalidOperationException($"No se puede editar una remesa en estado {original.EstadoTexto}.");

        _source.Update(editada);

        var cambios = Comparar(original, editada);
        if (cambios.Count > 0)
        {
            _eventos.Add(new EventoOperacion
            {
                RemesaId = editada.Id,
                Tipo = TipoEventoOperacion.Edicion,
                FechaHora = DateTime.Now,
                Descripcion = string.Join(" · ", cambios),
                Autor = autor,
                OrigenId = editada.Id
            });
        }

        return editada;
    }

    /// <summary>
    /// Borra un borrador y barre sus eventos.
    ///
    /// No se publica un evento de borrado a propósito: Seguimiento se recorre por remesa, así que
    /// una remesa que ya no existe no tiene línea de tiempo donde enseñarlo. Lo que sí hace falta
    /// es la limpieza — las tablas son planas y no hay cascada, así que sin esto quedarían notas
    /// y cambios de turno apuntando a un Id inexistente.
    /// </summary>
    public void Eliminar(Remesa remesa)
    {
        if (!PuedeEliminar(remesa))
            throw new InvalidOperationException($"No se puede eliminar una remesa en estado {remesa.EstadoTexto}.");

        _source.Delete(remesa.Id);
        _eventos.EliminarDeRemesa(remesa.Id);
    }

    /// <summary>
    /// Campos cuyo cambio merece contarse. Solo los que el editor deja tocar y significan algo
    /// para quien lea la historia después; los Id van con su texto, que es lo legible.
    /// </summary>
    private static List<string> Comparar(Remesa antes, Remesa ahora)
    {
        var campos = new (string Etiqueta, string Antes, string Ahora)[]
        {
            ("Finca", antes.FincaNombre, ahora.FincaNombre),
            ("Lote", antes.LoteNombre, ahora.LoteNombre),
            ("Tablón", antes.TablonNombre, ahora.TablonNombre),
            ("Cosecha", antes.TipoCosechaTexto, ahora.TipoCosechaTexto),
            ("Operador", antes.OperadorNombre, ahora.OperadorNombre),
            ("Tractorista", antes.TractoristaNombre, ahora.TractoristaNombre),
            ("Chofer", antes.ChoferNombre, ahora.ChoferNombre),
            ("Unidad", antes.VehiculoPlaca, ahora.VehiculoPlaca),
            ("Remesero", antes.RemeseroNombre, ahora.RemeseroNombre),
            ("Núcleo de corte", antes.NucleoCorteCodigo, ahora.NucleoCorteCodigo),
            ("Núcleo de alza y empuje", antes.NucleoAlzaEmpujeCodigo, ahora.NucleoAlzaEmpujeCodigo),
            ("Núcleo de transporte", antes.NucleoTransporteCodigo, ahora.NucleoTransporteCodigo),
            ("Inicio de carga", Momento(antes.InicioCarga), Momento(ahora.InicioCarga)),
            ("Fin de carga", Momento(antes.FinCarga), Momento(ahora.FinCarga))
        };

        return [.. campos
            .Where(c => c.Antes != c.Ahora)
            .Select(c => $"{c.Etiqueta}: {Vacio(c.Antes)} → {Vacio(c.Ahora)}")];
    }

    private static string Momento(DateTime valor)
        => valor == default ? string.Empty : valor.ToString("dd/MM/yyyy HH:mm");

    private static string Vacio(string valor) => valor.Length == 0 ? "(sin dato)" : valor;

    /// <summary>Confirma la remesa: a partir de aquí es inmutable y cuenta para la liquidación.</summary>
    public Remesa Confirmar(Remesa remesa)
    {
        if (!PuedeConfirmar(remesa))
            throw new InvalidOperationException($"No se puede confirmar una remesa en estado {remesa.EstadoTexto}.");

        if (!EstaCompleta(remesa, out var faltantes))
            throw new InvalidOperationException($"La remesa está incompleta. Faltan: {faltantes}.");

        var actualizada = remesa.Clonar();
        actualizada.Estado = EstadoRemesa.Confirmada;
        actualizada.FechaConfirmacion = DateTime.Now;
        _source.Update(actualizada);
        return actualizada;
    }

    /// <summary>Anula la remesa dejando constancia del motivo (toda firma lleva comentario).</summary>
    public Remesa Anular(Remesa remesa, string motivo)
    {
        if (!PuedeAnular(remesa))
            throw new InvalidOperationException($"No se puede anular una remesa en estado {remesa.EstadoTexto}.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Debe indicar el motivo de la anulación.");

        var actualizada = remesa.Clonar();
        actualizada.Estado = EstadoRemesa.Anulada;
        actualizada.MotivoAnulacion = motivo.Trim();
        actualizada.FechaAnulacion = DateTime.Now;
        _source.Update(actualizada);
        return actualizada;
    }

    /// <summary>
    /// Registra la llegada al central y el pesaje en la romana. Es el paso que hace personal
    /// del CAM en la Pre-Romana, no quien registró la remesa en el campo.
    /// </summary>
    public Remesa RegistrarRecepcion(Remesa remesa, DateTime llegada, decimal pesoBrutoT, decimal taraT)
    {
        if (!PuedeRegistrarRecepcion(remesa))
            throw new InvalidOperationException($"Solo se registra la recepción de una remesa confirmada; esta está {remesa.EstadoTexto}.");

        if (llegada < remesa.FinCarga)
            throw new InvalidOperationException("La llegada al central no puede ser anterior al fin de carga.");

        if (taraT <= 0)
            throw new InvalidOperationException("La tara debe ser mayor que cero.");

        if (pesoBrutoT <= taraT)
            throw new InvalidOperationException("El peso bruto debe ser mayor que la tara.");

        var actualizada = remesa.Clonar();
        actualizada.LlegadaCentral = llegada;
        actualizada.PesoBrutoT = pesoBrutoT;
        actualizada.TaraT = taraT;
        actualizada.Estado = EstadoRemesa.Recibida;
        _source.Update(actualizada);
        return actualizada;
    }

    /// <summary>
    /// Campos que la normativa exige tener llenos ("Todos los datos de la Remesa de caña deben ser
    /// llenados"), sin contar llegada al central y pesaje, que se registran en la recepción.
    /// La comparte el editor para no tener dos versiones de la misma regla.
    /// </summary>
    public static bool EstaCompleta(Remesa remesa, out string? faltantes)
    {
        var pendientes = new List<string>();

        if (remesa.FincaId == 0) pendientes.Add("finca");
        if (string.IsNullOrWhiteSpace(remesa.LoteNombre)) pendientes.Add("lote");
        if (string.IsNullOrWhiteSpace(remesa.TablonNombre)) pendientes.Add("tablón");
        if (remesa.OperadorId == 0) pendientes.Add("operador");
        if (remesa.TractoristaId == 0) pendientes.Add("tractorista");
        if (remesa.ChoferId == 0) pendientes.Add("chofer");
        if (remesa.VehiculoId == 0) pendientes.Add("placa");
        if (remesa.RemeseroId == 0) pendientes.Add("remesero");
        if (string.IsNullOrWhiteSpace(remesa.NucleoCorteCodigo)) pendientes.Add("núcleo de corte");
        if (string.IsNullOrWhiteSpace(remesa.NucleoAlzaEmpujeCodigo)) pendientes.Add("núcleo de alza y empuje");
        if (string.IsNullOrWhiteSpace(remesa.NucleoTransporteCodigo)) pendientes.Add("núcleo de transporte");
        if (remesa.InicioCarga == default) pendientes.Add("inicio de carga");
        if (remesa.FinCarga == default) pendientes.Add("fin de carga");

        faltantes = pendientes.Count == 0 ? null : string.Join(", ", pendientes);
        return pendientes.Count == 0;
    }
}
