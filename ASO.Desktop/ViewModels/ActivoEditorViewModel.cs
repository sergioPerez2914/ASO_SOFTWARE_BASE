using System;
using System.Collections.Generic;
using System.Globalization;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta y edición de la ficha de un activo de flota. El tipo solo se elige en el alta: cambiar
/// el tipo de un activo ya referenciado por remesas rompería su historial. El estado operativo
/// tampoco se edita aquí: se cambia por comando en la pantalla de gestión.
/// </summary>
public sealed class ActivoEditorViewModel : CrudEditorViewModelBase<ActivoFlota>
{
    private readonly ActivoFlota _original;
    private readonly bool _esNuevo;

    public ActivoEditorViewModel(ActivoFlota original)
    {
        _original = original;
        _esNuevo = original.Id == 0;

        if (!_esNuevo)
        {
            _tipoSeleccionado = original.Tipo;
            Codigo = original.Codigo;
            Marca = original.Marca;
            Modelo = original.Modelo;
            AnioTexto = original.Anio == 0 ? string.Empty : original.Anio.ToString();
            Placa = original.Placa;
            Descripcion = original.Descripcion;
            Notas = original.Notas;
            LecturaTexto = (original.EsTransporte ? original.OdometroKm : original.HorometroHoras)
                ?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        }
    }

    public override string Titulo => _esNuevo ? "Nuevo activo de flota" : $"Editar {_original.Codigo}";

    public override double AnchoEditor => Ancho.Estandar;

    public IReadOnlyList<TipoActivo> Tipos { get; } =
        [TipoActivo.Cosechadora, TipoActivo.Tractor, TipoActivo.Alzadora, TipoActivo.Camion, TipoActivo.Chuto];

    /// <summary>El tipo queda fijo tras el alta.</summary>
    public bool PuedeCambiarTipo => _esNuevo;

    private TipoActivo _tipoSeleccionado = TipoActivo.Cosechadora;
    public TipoActivo TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set
        {
            if (!SetProperty(ref _tipoSeleccionado, value)) return;
            OnPropertyChanged(nameof(EsTransporte));
            OnPropertyChanged(nameof(EtiquetaLectura));
        }
    }

    public bool EsTransporte => TipoSeleccionado is TipoActivo.Camion or TipoActivo.Chuto;

    public string EtiquetaLectura => EsTransporte ? "Odómetro (km)" : "Horómetro (h)";

    private string _codigo = string.Empty;
    public string Codigo { get => _codigo; set => SetProperty(ref _codigo, value); }

    private string _marca = string.Empty;
    public string Marca { get => _marca; set => SetProperty(ref _marca, value); }

    private string _modelo = string.Empty;
    public string Modelo { get => _modelo; set => SetProperty(ref _modelo, value); }

    private string _anioTexto = string.Empty;
    public string AnioTexto { get => _anioTexto; set => SetProperty(ref _anioTexto, value); }

    private string _placa = string.Empty;
    public string Placa { get => _placa; set => SetProperty(ref _placa, value); }

    private string _descripcion = string.Empty;
    public string Descripcion { get => _descripcion; set => SetProperty(ref _descripcion, value); }

    private string _lecturaTexto = string.Empty;
    public string LecturaTexto { get => _lecturaTexto; set => SetProperty(ref _lecturaTexto, value); }

    private string _notas = string.Empty;
    public string Notas { get => _notas; set => SetProperty(ref _notas, value); }

    protected override bool Validar(out string? error)
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(Codigo)) faltantes.Add("código");
        if (string.IsNullOrWhiteSpace(Marca)) faltantes.Add("marca");
        if (string.IsNullOrWhiteSpace(Modelo)) faltantes.Add("modelo");
        if (EsTransporte && string.IsNullOrWhiteSpace(Placa)) faltantes.Add("placa");

        if (faltantes.Count > 0)
        {
            error = $"Complete los campos: {string.Join(", ", faltantes)}.";
            return false;
        }

        if (AnioTexto.Trim().Length > 0 && !int.TryParse(AnioTexto.Trim(), out _))
        {
            error = "El año debe ser un número.";
            return false;
        }

        if (LecturaTexto.Trim().Length > 0 &&
            !decimal.TryParse(LecturaTexto.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out _))
        {
            error = $"El valor de \"{EtiquetaLectura}\" debe ser un número.";
            return false;
        }

        error = null;
        return true;
    }

    public override ActivoFlota ObtenerResultado()
    {
        var activo = _original.Clonar();

        activo.Codigo = Codigo.Trim();
        activo.Tipo = TipoSeleccionado;
        activo.Marca = Marca.Trim();
        activo.Modelo = Modelo.Trim();
        activo.Anio = int.TryParse(AnioTexto.Trim(), out var anio) ? anio : 0;
        activo.Placa = EsTransporte ? Placa.Trim().ToUpperInvariant() : string.Empty;
        activo.Descripcion = Descripcion.Trim();
        activo.Notas = Notas.Trim();

        if (decimal.TryParse(LecturaTexto.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var lectura))
        {
            if (EsTransporte) { activo.OdometroKm = lectura; activo.HorometroHoras = null; }
            else { activo.HorometroHoras = lectura; activo.OdometroKm = null; }
        }

        return activo;
    }
}
