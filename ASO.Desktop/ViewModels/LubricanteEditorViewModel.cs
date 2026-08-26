using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un lubricante del catálogo (Marca + Tipo + Grado de viscosidad). También se
/// invoca como alta rápida desde el botón "+ Nuevo" al registrar una Recepción de mercancía,
/// mismo patrón que <see cref="StockCombustibleEditorViewModel"/>.
/// </summary>
public sealed class LubricanteEditorViewModel : CrudEditorViewModelBase<Lubricante>
{
    private readonly Lubricante _original;

    public LubricanteEditorViewModel() : this(new Lubricante { Activo = true })
    {
    }

    public LubricanteEditorViewModel(Lubricante original)
    {
        _original = original;
        Marca = original.Marca;
        TipoSeleccionado = string.IsNullOrWhiteSpace(original.Tipo) ? Lubricante.Tipos[0] : original.Tipo;
        GradoSeleccionado = string.IsNullOrWhiteSpace(original.GradoViscosidad) ? Lubricante.GradosViscosidad[0] : original.GradoViscosidad;
        ExistenciaTexto = original.Id == 0 ? "0" : original.ExistenciaL.ToString("0.##");
        Activo = original.Id == 0 || original.Activo;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo lubricante" : $"Editar {_original.Etiqueta}";

    public IReadOnlyList<string> Tipos => Lubricante.Tipos;
    public IReadOnlyList<string> Grados => Lubricante.GradosViscosidad;

    private string _marca = string.Empty;
    public string Marca
    {
        get => _marca;
        set => SetProperty(ref _marca, value);
    }

    private string _tipoSeleccionado = string.Empty;
    public string TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set => SetProperty(ref _tipoSeleccionado, value);
    }

    private string _gradoSeleccionado = string.Empty;
    public string GradoSeleccionado
    {
        get => _gradoSeleccionado;
        set => SetProperty(ref _gradoSeleccionado, value);
    }

    private string _existenciaTexto = string.Empty;
    public string ExistenciaTexto
    {
        get => _existenciaTexto;
        set => SetProperty(ref _existenciaTexto, value);
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Marca))
        {
            error = "Indique la marca del lubricante.";
            return false;
        }

        if (!decimal.TryParse(ExistenciaTexto, out var existencia) || existencia < 0)
        {
            error = "La existencia debe ser un número mayor o igual a cero.";
            return false;
        }

        error = null;
        return true;
    }

    public override Lubricante ObtenerResultado()
    {
        var lubricante = _original.Clonar();
        lubricante.Marca = Marca.Trim();
        lubricante.Tipo = TipoSeleccionado;
        lubricante.GradoViscosidad = GradoSeleccionado;
        lubricante.ExistenciaL = decimal.TryParse(ExistenciaTexto, out var existencia) ? existencia : 0m;
        lubricante.Activo = Activo;
        return lubricante;
    }
}
