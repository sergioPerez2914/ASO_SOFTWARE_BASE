using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un lubricante del catálogo (Marca + Tipo + Grado de viscosidad + Presentación).
/// La marca se elige de <see cref="MarcaLubricante"/> (catálogo propio, no texto libre), con
/// "+ Nuevo" si falta una — mismo patrón que <see cref="StockCombustibleEditorViewModel"/>. La
/// existencia en litros no se captura aquí: se deriva de Presentación × Unidades (ver
/// <see cref="Lubricante.ExistenciaL"/>), así que se muestra de solo lectura.
/// </summary>
public sealed class LubricanteEditorViewModel : CrudEditorViewModelBase<Lubricante>
{
    private readonly Lubricante _original;
    private readonly IMarcaLubricanteDataSource _marcasLubricante;
    private readonly IServicioDialogo _dialogos;

    public LubricanteEditorViewModel(Lubricante original,
                                     IMarcaLubricanteDataSource marcasLubricante,
                                     IServicioDialogo dialogos,
                                     ISesionActual sesion)
    {
        _original = original;
        _marcasLubricante = marcasLubricante;
        _dialogos = dialogos;

        MarcasLubricante = new ObservableCollection<MarcaLubricante>(
            marcasLubricante.GetAll().Where(m => m.Activo).OrderBy(m => m.Nombre));
        MarcaSeleccionada = MarcasLubricante.FirstOrDefault(m => m.Id == original.MarcaLubricanteId)
            ?? MarcasLubricante.FirstOrDefault();

        TipoSeleccionado = string.IsNullOrWhiteSpace(original.Tipo) ? Lubricante.Tipos[0] : original.Tipo;
        GradoSeleccionado = string.IsNullOrWhiteSpace(original.GradoViscosidad) ? Lubricante.GradosViscosidad[0] : original.GradoViscosidad;
        PresentacionSeleccionada = string.IsNullOrWhiteSpace(original.Presentacion) ? Lubricante.Presentaciones[0] : original.Presentacion;
        UnidadesTexto = original.Id == 0 ? "0" : original.Unidades.ToString("0.##");
        Activo = original.Id == 0 || original.Activo;

        NuevoMarcaCommand = new RelayCommand(NuevoMarca, () => sesion.Puede(Permisos.Lubricantes.Crear));
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo lubricante" : $"Editar {_original.Etiqueta}";

    public IReadOnlyList<string> Tipos => Lubricante.Tipos;
    public IReadOnlyList<string> Grados => Lubricante.GradosViscosidad;
    public IReadOnlyList<string> Presentaciones => Lubricante.Presentaciones;

    public ObservableCollection<MarcaLubricante> MarcasLubricante { get; }

    public ICommand NuevoMarcaCommand { get; }

    private void NuevoMarca()
    {
        var editor = new MarcaLubricanteEditorViewModel();
        if (!_dialogos.MostrarEditor(editor))
            return;

        var nueva = _marcasLubricante.Add(editor.ObtenerResultado());
        MarcasLubricante.Add(nueva);
        MarcaSeleccionada = nueva;
    }

    private MarcaLubricante? _marcaSeleccionada;
    public MarcaLubricante? MarcaSeleccionada
    {
        get => _marcaSeleccionada;
        set => SetProperty(ref _marcaSeleccionada, value);
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

    private string _presentacionSeleccionada = string.Empty;
    public string PresentacionSeleccionada
    {
        get => _presentacionSeleccionada;
        set
        {
            if (SetProperty(ref _presentacionSeleccionada, value))
                OnPropertyChanged(nameof(ExistenciaPreviewTexto));
        }
    }

    private string _unidadesTexto = string.Empty;
    public string UnidadesTexto
    {
        get => _unidadesTexto;
        set
        {
            if (SetProperty(ref _unidadesTexto, value))
                OnPropertyChanged(nameof(ExistenciaPreviewTexto));
        }
    }

    /// <summary>Vista previa en litros mientras se edita, misma fórmula que <see cref="Lubricante.ExistenciaL"/>.</summary>
    public string ExistenciaPreviewTexto
    {
        get
        {
            var unidades = decimal.TryParse(UnidadesTexto, out var u) ? u : 0m;
            var litros = Lubricante.LitrosPorPresentacion.TryGetValue(PresentacionSeleccionada, out var l) ? l : 0m;
            return $"{unidades * litros:N0} L";
        }
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (MarcaSeleccionada is null)
        {
            error = "Seleccione la marca del lubricante.";
            return false;
        }

        if (!decimal.TryParse(UnidadesTexto, out var unidades) || unidades < 0)
        {
            error = "Las unidades deben ser un número mayor o igual a cero.";
            return false;
        }

        error = null;
        return true;
    }

    public override Lubricante ObtenerResultado()
    {
        var lubricante = _original.Clonar();
        lubricante.MarcaLubricanteId = MarcaSeleccionada!.Id;
        lubricante.MarcaLubricanteNombre = MarcaSeleccionada.Nombre;
        lubricante.Tipo = TipoSeleccionado;
        lubricante.GradoViscosidad = GradoSeleccionado;
        lubricante.Presentacion = PresentacionSeleccionada;
        lubricante.Unidades = decimal.TryParse(UnidadesTexto, out var unidades) ? unidades : 0m;
        lubricante.Activo = Activo;
        return lubricante;
    }
}
