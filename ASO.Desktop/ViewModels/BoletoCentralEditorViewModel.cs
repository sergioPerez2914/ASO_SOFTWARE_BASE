using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>Una fila de la comparación: lo que calcula el tarifario contra lo que dice el boleto.</summary>
public sealed record FilaComparacionBoleto(string Servicio, decimal Esperado, decimal Declarado)
{
    public decimal Diferencia => Declarado - Esperado;

    public string EsperadoTexto => Esperado.ToString("N2", CultureInfo.CurrentCulture);
    public string DeclaradoTexto => Declarado.ToString("N2", CultureInfo.CurrentCulture);
    public string DiferenciaTexto => Diferencia.ToString("+#,##0.00;-#,##0.00;0,00", CultureInfo.CurrentCulture);

    /// <summary>Un céntimo de más o de menos no es un reclamo; se marca solo lo que se separa de verdad.</summary>
    public bool Cuadra => Math.Abs(Diferencia) < 0.01m;
}

/// <summary>
/// Boleto que emite el central al recibir la carga: el paso que cierra la remesa. Reemplaza a la
/// vieja "recepción", que solo pedía llegada, bruto y tara.
///
/// Además de transcribir el papel, muestra en vivo la comparación entre lo que el central
/// reconoce pagar por corte, alza y empuje y transporte, y lo que sale del tarifario de la app.
/// La diferencia AVISA pero no bloquea: es el dato con el que se reclama, no una regla del
/// documento, y por eso vive aquí y no en <see cref="RemesaService"/>.
///
/// Hereda de la base no genérica porque no reconstruye la entidad: expone los valores capturados
/// y la transición la aplica <see cref="RemesaService.RegistrarBoleto"/>.
/// </summary>
public sealed class BoletoCentralEditorViewModel : CrudEditorViewModelBase
{
    private const string FormatoHora = @"hh\:mm";

    private readonly Remesa _remesa;
    private readonly TarifaService _tarifas;

    public BoletoCentralEditorViewModel(Remesa remesa, TarifaService tarifas)
    {
        _remesa = remesa;
        _tarifas = tarifas;
        LlegadaFecha = remesa.FinCarga.Date;
    }

    public override string Titulo => $"Boleto del central · Remesa Nº {_remesa.Id}";
    public override string TextoAccion => "Cerrar la remesa";

    /// <summary>Cuatro secciones y una tabla de comparación: no entra en una columna.</summary>
    public override double AnchoEditor => Ancho.Amplio;

    public string ResumenRemesa =>
        $"{_remesa.FincaCodigoCam} · {_remesa.FincaNombre} — {_remesa.UbicacionTexto} — Placa {_remesa.VehiculoPlaca}";

    public string FinCargaTexto => $"Fin de carga: {_remesa.FinCarga:dd/MM/yyyy HH:mm}";

    // --- Identificación y llegada ---

    private string _numero = string.Empty;
    public string Numero
    {
        get => _numero;
        set => SetProperty(ref _numero, value);
    }

    private DateTime? _llegadaFecha;
    public DateTime? LlegadaFecha
    {
        get => _llegadaFecha;
        set
        {
            if (SetProperty(ref _llegadaFecha, value))
                RecalcularComparacion();
        }
    }

    private string _llegadaHora = string.Empty;
    public string LlegadaHora
    {
        get => _llegadaHora;
        set => SetProperty(ref _llegadaHora, value);
    }

    // --- Pesaje ---

    private string _pesoBrutoTexto = string.Empty;
    public string PesoBrutoTexto
    {
        get => _pesoBrutoTexto;
        set
        {
            if (SetProperty(ref _pesoBrutoTexto, value))
            {
                OnPropertyChanged(nameof(PesoNetoTexto));
                RecalcularComparacion();
            }
        }
    }

    private string _taraTexto = string.Empty;
    public string TaraTexto
    {
        get => _taraTexto;
        set
        {
            if (SetProperty(ref _taraTexto, value))
            {
                OnPropertyChanged(nameof(PesoNetoTexto));
                RecalcularComparacion();
            }
        }
    }

    /// <summary>Neto calculado en vivo mientras se teclea; "—" mientras no haya dos números válidos.</summary>
    public string PesoNetoTexto =>
        TryNumero(PesoBrutoTexto, out var bruto) && TryNumero(TaraTexto, out var tara)
            ? (bruto - tara).ToString("N2", CultureInfo.CurrentCulture)
            : "—";

    // --- Calidad de la caña (informativa) ---

    private string _atrTexto = string.Empty;
    public string AtrTexto { get => _atrTexto; set => SetProperty(ref _atrTexto, value); }

    private string _fibraTexto = string.Empty;
    public string FibraTexto { get => _fibraTexto; set => SetProperty(ref _fibraTexto, value); }

    private string _purezaTexto = string.Empty;
    public string PurezaTexto { get => _purezaTexto; set => SetProperty(ref _purezaTexto, value); }

    private string _trashMineralTexto = string.Empty;
    public string TrashMineralTexto { get => _trashMineralTexto; set => SetProperty(ref _trashMineralTexto, value); }

    private string _trashVegetalTexto = string.Empty;
    public string TrashVegetalTexto { get => _trashVegetalTexto; set => SetProperty(ref _trashVegetalTexto, value); }

    // --- Montos que declara el central ---

    private string _canaEntregadaTexto = string.Empty;
    public string CanaEntregadaTexto { get => _canaEntregadaTexto; set => SetProperty(ref _canaEntregadaTexto, value); }

    private string _descuentoCorteTexto = string.Empty;
    public string DescuentoCorteTexto
    {
        get => _descuentoCorteTexto;
        set { if (SetProperty(ref _descuentoCorteTexto, value)) RecalcularComparacion(); }
    }

    private string _descuentoAlzaEmpujeTexto = string.Empty;
    public string DescuentoAlzaEmpujeTexto
    {
        get => _descuentoAlzaEmpujeTexto;
        set { if (SetProperty(ref _descuentoAlzaEmpujeTexto, value)) RecalcularComparacion(); }
    }

    private string _descuentoTransporteTexto = string.Empty;
    public string DescuentoTransporteTexto
    {
        get => _descuentoTransporteTexto;
        set { if (SetProperty(ref _descuentoTransporteTexto, value)) RecalcularComparacion(); }
    }

    private string _descuentoAdministracionTexto = string.Empty;
    public string DescuentoAdministracionTexto
    {
        get => _descuentoAdministracionTexto;
        set => SetProperty(ref _descuentoAdministracionTexto, value);
    }

    private string _descuentoRuralTexto = string.Empty;
    public string DescuentoRuralTexto { get => _descuentoRuralTexto; set => SetProperty(ref _descuentoRuralTexto, value); }

    private string _descuentoInvestigacionTexto = string.Empty;
    public string DescuentoInvestigacionTexto
    {
        get => _descuentoInvestigacionTexto;
        set => SetProperty(ref _descuentoInvestigacionTexto, value);
    }

    private string _valorLiquidoTexto = string.Empty;
    public string ValorLiquidoTexto { get => _valorLiquidoTexto; set => SetProperty(ref _valorLiquidoTexto, value); }

    // --- Comparación con el tarifario ---

    private IReadOnlyList<FilaComparacionBoleto> _comparacion = [];
    public IReadOnlyList<FilaComparacionBoleto> Comparacion
    {
        get => _comparacion;
        private set => SetProperty(ref _comparacion, value);
    }

    private string _avisoComparacion = "Cargue el pesaje para comparar con el tarifario.";
    /// <summary>Qué decir cuando no hay comparación que enseñar, o cuando la hay y no cuadra.</summary>
    public string AvisoComparacion
    {
        get => _avisoComparacion;
        private set => SetProperty(ref _avisoComparacion, value);
    }

    private bool _hayDiferencia;
    public bool HayDiferencia
    {
        get => _hayDiferencia;
        private set => SetProperty(ref _hayDiferencia, value);
    }

    private void RecalcularComparacion()
    {
        if (!TryNumero(PesoBrutoTexto, out var bruto) || !TryNumero(TaraTexto, out var tara)
            || bruto - tara <= 0)
        {
            Comparacion = [];
            HayDiferencia = false;
            AvisoComparacion = "Cargue el pesaje para comparar con el tarifario.";
            return;
        }

        var fecha = LlegadaFecha?.Date ?? DateTime.Today;

        IReadOnlyList<CobroDeServicio> cobros;
        try
        {
            cobros = _tarifas.CalcularCobroPorServicio(_remesa, bruto - tara, fecha);
        }
        catch (InvalidOperationException ex)
        {
            // Sin tarifario cargado no hay con qué comparar, pero el boleto se sigue pudiendo
            // registrar: la falta de tarifas no es motivo para dejar la remesa abierta.
            Comparacion = [];
            HayDiferencia = false;
            AvisoComparacion = ex.Message;
            return;
        }

        var declarados = new Dictionary<ServicioZafra, decimal>
        {
            [ServicioZafra.Corte] = Monto(DescuentoCorteTexto),
            [ServicioZafra.AlzaEmpuje] = Monto(DescuentoAlzaEmpujeTexto),
            [ServicioZafra.Transporte] = Monto(DescuentoTransporteTexto)
        };

        Comparacion = [.. cobros.Select(c => new FilaComparacionBoleto(
            TextoServicio(c.Servicio), c.Monto, declarados.GetValueOrDefault(c.Servicio)))];

        var diferencia = Comparacion.Sum(f => f.Diferencia);
        HayDiferencia = Comparacion.Any(f => !f.Cuadra);
        AvisoComparacion = HayDiferencia
            ? $"El boleto se separa del tarifario en {diferencia:N2}. Se puede cerrar igual: queda registrado para reclamar."
            : "El boleto coincide con el tarifario.";
    }

    private static string TextoServicio(ServicioZafra servicio) => servicio switch
    {
        ServicioZafra.Corte => "Corte",
        ServicioZafra.AlzaEmpuje => "Alza y empuje",
        ServicioZafra.Transporte => "Transporte",
        _ => "Otro"
    };

    // Valores ya parseados, para que el ViewModel de la lista no vuelva a interpretar texto.
    public DateTime Llegada { get; private set; }
    public decimal PesoBrutoT { get; private set; }
    public decimal TaraT { get; private set; }
    public BoletoCentral Boleto { get; private set; } = new();

    private static bool TryNumero(string texto, out decimal valor)
        => decimal.TryParse(texto?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out valor);

    /// <summary>Un campo de monto vacío es un cero: el boleto no siempre trae todos los conceptos.</summary>
    private static decimal Monto(string texto) => TryNumero(texto, out var valor) ? valor : 0m;

    /// <summary>Un campo de calidad vacío es "no lo dice el boleto", que no es lo mismo que cero.</summary>
    private static decimal? Calidad(string texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : TryNumero(texto, out var valor) ? valor : null;

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Numero))
        {
            error = "Indique el número del boleto que emitió el central.";
            return false;
        }

        if (LlegadaFecha is null ||
            !TimeSpan.TryParseExact(LlegadaHora?.Trim(), FormatoHora, CultureInfo.InvariantCulture, out var hora))
        {
            error = "Indique la fecha y la hora de llegada al central (formato HH:mm).";
            return false;
        }

        var llegada = LlegadaFecha.Value.Date + hora;
        if (llegada < _remesa.FinCarga)
        {
            error = "La llegada al central no puede ser anterior al fin de carga.";
            return false;
        }

        if (!TryNumero(PesoBrutoTexto, out var bruto) || !TryNumero(TaraTexto, out var tara))
        {
            error = "El peso bruto y la tara deben ser números en toneladas.";
            return false;
        }

        if (tara <= 0)
        {
            error = "La tara debe ser mayor que cero.";
            return false;
        }

        if (bruto <= tara)
        {
            error = "El peso bruto debe ser mayor que la tara.";
            return false;
        }

        if (!SonNumeros(out var campoInvalido))
        {
            error = $"«{campoInvalido}» debe ser un número.";
            return false;
        }

        Llegada = llegada;
        PesoBrutoT = bruto;
        TaraT = tara;
        Boleto = new BoletoCentral
        {
            Numero = Numero.Trim(),
            Atr = Calidad(AtrTexto),
            Fibra = Calidad(FibraTexto),
            Pureza = Calidad(PurezaTexto),
            TrashMineral = Calidad(TrashMineralTexto),
            TrashVegetal = Calidad(TrashVegetalTexto),
            MontoCanaEntregada = Monto(CanaEntregadaTexto),
            DescuentoCorte = Monto(DescuentoCorteTexto),
            DescuentoAlzaEmpuje = Monto(DescuentoAlzaEmpujeTexto),
            DescuentoTransporte = Monto(DescuentoTransporteTexto),
            DescuentoAdministracion = Monto(DescuentoAdministracionTexto),
            DescuentoRural = Monto(DescuentoRuralTexto),
            DescuentoInvestigacion = Monto(DescuentoInvestigacionTexto),
            ValorLiquido = Monto(ValorLiquidoTexto)
        };

        error = null;
        return true;
    }

    /// <summary>
    /// Un campo con texto que no es número se guardaría como cero sin decir nada, y ese cero
    /// acabaría en la comparación como una diferencia que nadie escribió.
    /// </summary>
    private bool SonNumeros(out string campo)
    {
        var campos = new (string Etiqueta, string Texto)[]
        {
            ("ATR", AtrTexto), ("Fibra", FibraTexto), ("Pureza", PurezaTexto),
            ("Trash mineral", TrashMineralTexto), ("Trash vegetal", TrashVegetalTexto),
            ("Caña entregada", CanaEntregadaTexto), ("Corte", DescuentoCorteTexto),
            ("Alza y empuje", DescuentoAlzaEmpujeTexto), ("Transporte", DescuentoTransporteTexto),
            ("Administración", DescuentoAdministracionTexto), ("Rural", DescuentoRuralTexto),
            ("Investigación", DescuentoInvestigacionTexto), ("Valor líquido", ValorLiquidoTexto)
        };

        var invalido = campos.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Texto)
                                                  && !TryNumero(c.Texto, out _));
        campo = invalido.Etiqueta ?? string.Empty;
        return invalido.Etiqueta is null;
    }
}
