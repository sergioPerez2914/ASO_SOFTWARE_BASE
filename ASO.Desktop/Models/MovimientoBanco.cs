using System;

namespace ASO.Desktop.Models;

/// <summary>
/// Hacia dónde va el dinero. El <see cref="MovimientoBanco.Monto"/> es siempre positivo: el
/// signo lo pone esto, para que no haya dos formas de escribir la misma salida.
/// </summary>
public enum TipoMovimientoBanco
{
    Entrada,
    Salida
}

/// <summary>
/// Ciclo de vida del asiento. "Conciliado" quiere decir que el movimiento ya apareció en el
/// extracto que trajo el banco; la diferencia entre el saldo de libro y el conciliado es lo que
/// todavía está en camino (un cheque girado que nadie ha cobrado).
///
/// Anular no borra: el asiento se queda visible, fuera del saldo, con su motivo.
/// </summary>
public enum EstadoMovimientoBanco
{
    Registrado,
    Conciliado,
    Anulado
}

/// <summary>
/// De qué documento nació el asiento, o si lo tecleó alguien.
///
/// <b>Los miembros se añaden SIEMPRE al final:</b> se persisten como <c>int</c>, y declarar uno
/// en medio reinterpretaría las filas ya guardadas. Misma regla que
/// <see cref="TipoEventoOperacion"/>.
/// </summary>
public enum OrigenMovimiento
{
    Manual,
    FacturaCliente,
    FacturaProveedor,
    Liquidacion,
    Transferencia
}

/// <summary>
/// Para qué fue el dinero. Sirve para agrupar el gasto en el listado; no cambia ninguna regla.
///
/// <b>Los miembros se añaden SIEMPRE al final</b> (ver <see cref="OrigenMovimiento"/>).
/// </summary>
public enum CategoriaMovimiento
{
    CobroCliente,
    PagoProveedor,
    Nomina,
    ComisionBancaria,
    Transferencia,
    Impuesto,
    GastoVario,
    AporteCapital,
    Retiro,
    Otro
}

/// <summary>
/// Un asiento del libro de banco: dinero que entró o salió de una <see cref="CuentaBancaria"/>.
/// Documento plano, sin líneas.
///
/// <b>Se escribe, no se deriva</b>, y va contra el instinto que deja el resto del código —la
/// línea de tiempo de Seguimiento deriva sus eventos de los documentos y no los guarda. Aquí no
/// se puede, por dos razones:
///
/// 1. Un asiento necesita datos que el documento <b>no tiene</b>: a qué cuenta entró el dinero,
///    con qué fecha valor, con qué referencia de transferencia o cheque, y si ya apareció en el
///    extracto. Derivar no puede inventar nada de eso.
/// 2. El precedente de dinero en este código es <see cref="Tarifa"/>: los documentos copian el
///    monto y nunca guardan solo el Id, porque una factura reimpresa no puede cambiar de
///    importe. Un asiento de banco es el mismo caso, más fuerte: si mañana alguien corrige la
///    factura, el libro no puede moverse solo.
///
/// Lo que sí se hereda de Seguimiento es que <b>el usuario nunca lo teclea dos veces</b>: cuando
/// el asiento nace de un documento lo escribe el servicio de dominio, en la misma operación que
/// marca el cobro o el pago.
///
/// Un asiento con <see cref="Origen"/> distinto de <see cref="OrigenMovimiento.Manual"/> no se
/// edita ni se borra desde Banco: su verdad está en el documento. Solo admite conciliarse y
/// anularse con motivo.
/// </summary>
public class MovimientoBanco : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Nucleo (organizacion) duenno de la fila; lo estampa AsoDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int CuentaId { get; set; }

    /// <summary>Snapshot: renombrar la cuenta no reescribe los asientos viejos.</summary>
    public string CuentaNombre { get; set; } = string.Empty;

    /// <summary>
    /// Fecha valor: el día que el dinero se movió de verdad, no el día que alguien lo capturó.
    /// La elige el usuario, y es la que ordena el libro y calcula el saldo corrido.
    ///
    /// Los tres documentos que originan asientos guardan además su propia fecha
    /// (<c>FechaCobro</c>, <c>FechaPago</c>) y la conservan sin cambios: el documento dice
    /// cuándo se dio por cobrado, el asiento dice cuándo se movió el dinero.
    /// </summary>
    public DateTime Fecha { get; set; }

    public TipoMovimientoBanco Tipo { get; set; }

    /// <summary>Siempre positivo; el signo lo da <see cref="Tipo"/>.</summary>
    public decimal Monto { get; set; }

    /// <summary>Qué fue: "Comisión bancaria", "Cobro factura FC-0012".</summary>
    public string Concepto { get; set; } = string.Empty;

    /// <summary>Número de transferencia, cheque o depósito. Es por lo que se busca al conciliar.</summary>
    public string Referencia { get; set; } = string.Empty;

    public CategoriaMovimiento Categoria { get; set; }

    public OrigenMovimiento Origen { get; set; }

    /// <summary>
    /// Id del documento que originó el asiento; null si es manual. No hace falta guardar el tipo
    /// aparte: <see cref="Origen"/> ya lo dice, igual que en
    /// <see cref="EventoOperacion.OrigenId"/>.
    /// </summary>
    public int? OrigenId { get; set; }

    /// <summary>
    /// El otro asiento del par, en una transferencia entre cuentas: una salida y una entrada que
    /// nacen juntas. Guardarlo enlazado deja anular las dos a la vez y evita que quede media
    /// transferencia en el libro.
    /// </summary>
    public int? ContraparteId { get; set; }

    public EstadoMovimientoBanco Estado { get; set; }

    public DateTime? FechaConciliacion { get; set; }
    public int? ConciliadoPorId { get; set; }

    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    // TODO: ZafraId cuando exista el maestro de zafras

    /// <summary>Lo que este asiento le suma (o resta) al saldo. Un anulado no cuenta.</summary>
    public decimal Efecto => Estado == EstadoMovimientoBanco.Anulado
        ? 0m
        : Tipo == TipoMovimientoBanco.Entrada ? Monto : -Monto;

    /// <summary>Nació de un documento, así que Banco no lo edita ni lo borra.</summary>
    public bool EsDerivado => Origen != OrigenMovimiento.Manual;

    public string TipoTexto => Tipo == TipoMovimientoBanco.Entrada ? "Entrada" : "Salida";

    public string MontoTexto => Monto.ToString("N2");

    /// <summary>Solo la columna que le toca; la otra queda vacía.</summary>
    public string EntradaTexto => Tipo == TipoMovimientoBanco.Entrada ? Monto.ToString("N2") : string.Empty;

    public string SalidaTexto => Tipo == TipoMovimientoBanco.Salida ? Monto.ToString("N2") : string.Empty;

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");

    /// <summary>
    /// Saldo de la cuenta después de este asiento. NO se persiste (va con Ignore en el
    /// DbContext) y no es una propiedad derivada como las demás: la rellena la pantalla al armar
    /// el extracto, porque depende de TODA la historia anterior de la cuenta y un asiento suelto
    /// no puede calcularlo por su cuenta.
    ///
    /// Guardarlo en la tabla habría sido peor: un asiento anulado o corregido dejaría desfasados
    /// todos los saldos posteriores, y habría que reescribir la columna entera hacia abajo.
    /// </summary>
    public decimal SaldoCorrido { get; set; }

    public string SaldoCorridoTexto => SaldoCorrido.ToString("N2");

    public string EstadoTexto => Estado switch
    {
        EstadoMovimientoBanco.Registrado => "Registrado",
        EstadoMovimientoBanco.Conciliado => "Conciliado",
        EstadoMovimientoBanco.Anulado => "Anulado",
        _ => Estado.ToString()
    };

    public string CategoriaTexto => Categoria switch
    {
        CategoriaMovimiento.CobroCliente => "Cobro a cliente",
        CategoriaMovimiento.PagoProveedor => "Pago a proveedor",
        CategoriaMovimiento.Nomina => "Nómina",
        CategoriaMovimiento.ComisionBancaria => "Comisión bancaria",
        CategoriaMovimiento.Transferencia => "Transferencia",
        CategoriaMovimiento.Impuesto => "Impuesto",
        CategoriaMovimiento.GastoVario => "Gasto vario",
        CategoriaMovimiento.AporteCapital => "Aporte de capital",
        CategoriaMovimiento.Retiro => "Retiro",
        CategoriaMovimiento.Otro => "Otro",
        _ => Categoria.ToString()
    };

    /// <summary>De dónde salió el asiento, para que se vea que no lo tecleó nadie.</summary>
    public string OrigenTexto => Origen switch
    {
        OrigenMovimiento.Manual => "Manual",
        OrigenMovimiento.FacturaCliente => "Factura al ingenio",
        OrigenMovimiento.FacturaProveedor => "Factura de proveedor",
        OrigenMovimiento.Liquidacion => "Liquidación",
        OrigenMovimiento.Transferencia => "Transferencia",
        _ => Origen.ToString()
    };

    /// <summary>El documento citado, con el mismo formato que usa cada módulo.</summary>
    public string DocumentoTexto => (Origen, OrigenId) switch
    {
        (OrigenMovimiento.FacturaCliente, { } id) => $"FC-{id:D4}",
        (OrigenMovimiento.FacturaProveedor, { } id) => $"Factura Nº {id}",
        (OrigenMovimiento.Liquidacion, { } id) => $"Liquidación Nº {id}",
        (OrigenMovimiento.Transferencia, _) => "Entre cuentas",
        _ => "—"
    };

    public string ConciliacionTexto => FechaConciliacion is { } fecha
        ? fecha.ToString("dd/MM/yyyy")
        : "—";

    /// <summary>Copia superficial (solo tipos de valor y cadenas): este documento no lleva líneas.</summary>
    public MovimientoBanco Clonar() => (MovimientoBanco)MemberwiseClone();
}
