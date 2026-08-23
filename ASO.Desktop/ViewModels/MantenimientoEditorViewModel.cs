using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>Opción del combo de remesa vinculada; la primera es "Sin vínculo" (Remesa null).</summary>
public sealed class OpcionRemesa
{
    public OpcionRemesa(Remesa? remesa) => Remesa = remesa;

    public Remesa? Remesa { get; }

    public string Etiqueta => Remesa is { } r
        ? $"Nº {r.Id} · {r.InicioCarga:dd/MM} · {r.FincaNombre} ({r.VehiculoPlaca})"
        : "Sin vínculo";
}

/// <summary>
/// Registro de un mantenimiento realizado. La entidad resultante pasa por
/// <see cref="MantenimientoService.Registrar"/>, que aplica las reglas de negocio; aquí solo se
/// valida el formulario.
/// </summary>
public sealed class MantenimientoEditorViewModel : CrudEditorViewModelBase<MantenimientoRegistro>
{
    private const string FormatoHora = @"hh\:mm";

    private readonly MantenimientoRegistro _original;
    private readonly IRemesaDataSource _remesas;

    public MantenimientoEditorViewModel(MantenimientoRegistro original,
                                        IReadOnlyList<ActivoFlota> activos,
                                        IRemesaDataSource remesas,
                                        ActivoFlota? preseleccionado = null,
                                        string? descripcionSugerida = null)
    {
        _original = original;
        _remesas = remesas;

        Activos = activos;
        _fecha = DateTime.Today;
        _hora = DateTime.Now.ToString(FormatoHora, CultureInfo.InvariantCulture);

        if (descripcionSugerida is not null)
        {
            _descripcion = descripcionSugerida;
            _tipoSeleccionado = TipoMantenimiento.Preventivo;
        }

        if (preseleccionado is not null)
            ActivoSeleccionado = activos.FirstOrDefault(a => a.Id == preseleccionado.Id);
    }

    public override string Titulo => "Registrar mantenimiento";

    public override double AnchoEditor => Ancho.Estandar;

    public IReadOnlyList<ActivoFlota> Activos { get; }
    public IReadOnlyList<TipoMantenimiento> Tipos { get; } =
        [TipoMantenimiento.Preventivo, TipoMantenimiento.Correctivo];

    public List<OpcionRemesa> RemesasVinculables { get; private set; } = [new OpcionRemesa(null)];

    private ActivoFlota? _activoSeleccionado;
    public ActivoFlota? ActivoSeleccionado
    {
        get => _activoSeleccionado;
        set
        {
            if (!SetProperty(ref _activoSeleccionado, value)) return;

            OnPropertyChanged(nameof(EtiquetaLectura));
            RecargarRemesasVinculables();
        }
    }

    public string EtiquetaLectura => ActivoSeleccionado is { EsTransporte: true }
        ? "Odómetro (km)" : "Horómetro (h)";

    private TipoMantenimiento _tipoSeleccionado = TipoMantenimiento.Correctivo;
    public TipoMantenimiento TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set => SetProperty(ref _tipoSeleccionado, value);
    }

    private DateTime? _fecha;
    public DateTime? Fecha { get => _fecha; set => SetProperty(ref _fecha, value); }

    private string _hora;
    public string Hora { get => _hora; set => SetProperty(ref _hora, value); }

    private string _descripcion = string.Empty;
    public string Descripcion { get => _descripcion; set => SetProperty(ref _descripcion, value); }

    private string _lecturaTexto = string.Empty;
    public string LecturaTexto { get => _lecturaTexto; set => SetProperty(ref _lecturaTexto, value); }

    private string _repuestosUsados = string.Empty;
    public string RepuestosUsados { get => _repuestosUsados; set => SetProperty(ref _repuestosUsados, value); }

    private string _costoRepuestosTexto = string.Empty;
    public string CostoRepuestosTexto { get => _costoRepuestosTexto; set => SetProperty(ref _costoRepuestosTexto, value); }

    private string _costoManoObraTexto = string.Empty;
    public string CostoManoObraTexto { get => _costoManoObraTexto; set => SetProperty(ref _costoManoObraTexto, value); }

    private string _realizadoPor = string.Empty;
    public string RealizadoPor { get => _realizadoPor; set => SetProperty(ref _realizadoPor, value); }

    private OpcionRemesa? _opcionRemesaSeleccionada;
    public OpcionRemesa? OpcionRemesaSeleccionada
    {
        get => _opcionRemesaSeleccionada;
        set => SetProperty(ref _opcionRemesaSeleccionada, value);
    }

    /// <summary>Las 15 remesas más recientes; si el activo es de transporte, solo las suyas.</summary>
    private void RecargarRemesasVinculables()
    {
        var remesas = _remesas.GetAll().AsEnumerable();

        if (ActivoSeleccionado is { EsTransporte: true } transporte)
            remesas = remesas.Where(r => r.VehiculoId == transporte.Id);

        RemesasVinculables =
        [
            new OpcionRemesa(null),
            .. remesas.OrderByDescending(r => r.InicioCarga).Take(15).Select(r => new OpcionRemesa(r))
        ];

        OnPropertyChanged(nameof(RemesasVinculables));
        OpcionRemesaSeleccionada = RemesasVinculables[0];
    }

    protected override bool Validar(out string? error)
    {
        var faltantes = new List<string>();

        if (ActivoSeleccionado is null) faltantes.Add("activo");
        if (string.IsNullOrWhiteSpace(Descripcion)) faltantes.Add("descripción del trabajo");
        if (Fecha is null) faltantes.Add("fecha");

        if (Fecha is not null &&
            !TimeSpan.TryParseExact(Hora?.Trim(), FormatoHora, CultureInfo.InvariantCulture, out _))
            faltantes.Add("hora (HH:mm)");

        if (faltantes.Count > 0)
        {
            error = $"Complete los campos: {string.Join(", ", faltantes)}.";
            return false;
        }

        if (!EsNumeroOpcional(LecturaTexto) || !EsNumeroOpcional(CostoRepuestosTexto) || !EsNumeroOpcional(CostoManoObraTexto))
        {
            error = "La lectura y los costos deben ser números.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool EsNumeroOpcional(string texto)
        => texto.Trim().Length == 0
           || decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out _);

    private static decimal? ParseOpcional(string texto)
        => decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var valor)
            ? valor : null;

    public override MantenimientoRegistro ObtenerResultado()
    {
        TimeSpan.TryParseExact(Hora.Trim(), FormatoHora, CultureInfo.InvariantCulture, out var hora);

        var registro = new MantenimientoRegistro
        {
            Id = _original.Id,
            ActivoId = ActivoSeleccionado!.Id,
            Fecha = Fecha!.Value.Date + hora,
            Tipo = TipoSeleccionado,
            Descripcion = Descripcion.Trim(),
            LecturaUso = ParseOpcional(LecturaTexto),
            RepuestosUsados = RepuestosUsados.Trim(),
            CostoRepuestos = ParseOpcional(CostoRepuestosTexto),
            CostoManoObra = ParseOpcional(CostoManoObraTexto),
            RealizadoPor = RealizadoPor.Trim(),
            RemesaId = OpcionRemesaSeleccionada?.Remesa?.Id
        };

        return registro;
    }
}
