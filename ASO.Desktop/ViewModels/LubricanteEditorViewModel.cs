using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un lubricante del catálogo (Marca + Tipo + Grado de viscosidad). La marca se
/// elige de <see cref="MarcaLubricante"/> (catálogo propio, no texto libre), con "+ Nuevo" si
/// falta una — mismo patrón que <see cref="ProveedorEditorViewModel"/> desde "Comparar
/// proveedores". La existencia se captura directo en litros (<see cref="Lubricante.ExistenciaL"/>,
/// mismo criterio que <see cref="StockCombustible.ExistenciaL"/>) — normalmente la estampa
/// <c>ComprasService.ConfirmarRecepcion</c> sola; este editor sirve para corregirla a mano o para
/// completar filas que ya existían.
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
        ExistenciaLTexto = original.Id == 0 ? "0" : original.ExistenciaL.ToString("0.##");
        CostoUnitarioTexto = original.CostoUnitario.ToString("0.##");
        Activo = original.Id == 0 || original.Activo;

        NuevoMarcaCommand = new RelayCommand(NuevoMarca, () => sesion.Puede(Permisos.Lubricantes.Crear));
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo lubricante" : $"Editar {_original.Etiqueta}";

    public IReadOnlyList<string> Tipos => Lubricante.Tipos;
    public IReadOnlyList<string> Grados => Lubricante.GradosViscosidad;

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

    private string _existenciaLTexto = string.Empty;
    /// <summary>Litros en existencia, capturados directo — sin pasar por ninguna tabla de
    /// conversión de envase.</summary>
    public string ExistenciaLTexto
    {
        get => _existenciaLTexto;
        set
        {
            if (SetProperty(ref _existenciaLTexto, value))
                OnPropertyChanged(nameof(ValorPreviewTexto));
        }
    }

    private string _costoUnitarioTexto = string.Empty;
    /// <summary>Precio por litro. Normalmente lo estampa <c>ComprasService.ConfirmarRecepcion</c>
    /// solo; se edita a mano aquí para completar el costo de las filas que ya existían antes de
    /// que ese campo se agregara.</summary>
    public string CostoUnitarioTexto
    {
        get => _costoUnitarioTexto;
        set
        {
            if (SetProperty(ref _costoUnitarioTexto, value))
                OnPropertyChanged(nameof(ValorPreviewTexto));
        }
    }

    /// <summary>Vista previa del valor a costo mientras se edita, misma fórmula que
    /// <see cref="Lubricante.ValorTotal"/>.</summary>
    public string ValorPreviewTexto
    {
        get
        {
            var existencia = decimal.TryParse(ExistenciaLTexto, out var e) ? e : 0m;
            var costo = decimal.TryParse(CostoUnitarioTexto, out var c) ? c : 0m;
            return (existencia * costo).ToString("N2");
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

        if (!decimal.TryParse(ExistenciaLTexto, out var existencia) || existencia < 0)
        {
            error = "La existencia en litros debe ser un número mayor o igual a cero.";
            return false;
        }

        if (!decimal.TryParse(CostoUnitarioTexto, out var costo) || costo < 0)
        {
            error = "El costo unitario debe ser un número mayor o igual a cero.";
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
        lubricante.ExistenciaL = decimal.TryParse(ExistenciaLTexto, out var existencia) ? existencia : 0m;
        lubricante.CostoUnitario = decimal.TryParse(CostoUnitarioTexto, out var costo) ? costo : 0m;
        lubricante.Activo = Activo;
        return lubricante;
    }
}
