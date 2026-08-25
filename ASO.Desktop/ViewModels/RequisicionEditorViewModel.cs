using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta/edición de una requisición: solo las líneas de lo que hace falta, no hay cabecera propia
/// que llenar. Las líneas se arman en memoria con los campos de abajo y solo se fijan en
/// <see cref="Lineas"/> al pulsar "Agregar línea" — no hay edición de una línea ya agregada,
/// se quita y se vuelve a agregar (igual de simple que forzar a reabrir un formulario aparte).
/// </summary>
public sealed class RequisicionEditorViewModel : CrudEditorViewModelBase<Requisicion>
{
    /// <summary>Categoría del catálogo de Inventario que agrupa los lubricantes — permite
    /// distinguirlos del resto de los repuestos sin un campo nuevo en el modelo.</summary>
    public const string CategoriaLubricantes = "Lubricantes";

    /// <summary>Tipos de aceite. Lista cerrada: son los que hay, no texto libre.</summary>
    public static readonly IReadOnlyList<string> TiposLubricante = ["Mineral", "Sintético", "Semi-sintético"];

    /// <summary>Grados de viscosidad habituales en equipo diésel agrícola pesado.</summary>
    public static readonly IReadOnlyList<string> GradosViscosidadLubricante =
        ["15W40", "20W50", "10W40", "20W40", "15W30", "SAE 30", "SAE 40"];

    private readonly Requisicion _original;
    private readonly IInventoryDataSource _articulos;

    public RequisicionEditorViewModel(Requisicion original, IInventoryDataSource articulos)
    {
        _original = original;
        _articulos = articulos;

        Articulos = articulos.GetAll().Where(a => !EsLubricante(a)).OrderBy(a => a.Nombre).ToList();

        Lineas = new ObservableCollection<RequisicionLinea>(original.Lineas.Select(l => l.Clonar()));

        ArticuloLineaSeleccionado = Articulos.FirstOrDefault();
        TipoLubricanteSeleccionado = TiposLubricante[0];
        GradoViscosidadSeleccionado = GradosViscosidadLubricante[0];

        AgregarLineaCommand = new RelayCommand(AgregarLinea);
        QuitarLineaCommand = new RelayCommand<RequisicionLinea>(linea =>
        {
            if (linea is not null)
                Lineas.Remove(linea);
        });

        CambiarTipoLineaCommand = new RelayCommand<string>(tipo =>
            TipoLineaSeleccionado = tipo == "Repuesto" ? TipoInsumo.Repuesto : TipoInsumo.Combustible);

        CambiarTipoCombustibleCommand = new RelayCommand<string>(tipo =>
            TipoCombustibleLineaSeleccionado = tipo == "Lubricante" ? TipoCombustible.Lubricante : TipoCombustible.Diesel);
    }

    private static bool EsLubricante(InventoryItem articulo) =>
        string.Equals(articulo.Categoria, CategoriaLubricantes, System.StringComparison.OrdinalIgnoreCase);

    public override string Titulo =>
        _original.Id == 0 ? "Nueva requisición" : $"Editar requisición Nº {_original.Id}";

    public override double AnchoEditor => Ancho.Amplio;

    public IReadOnlyList<InventoryItem> Articulos { get; }

    public ObservableCollection<RequisicionLinea> Lineas { get; }

    public ICommand AgregarLineaCommand { get; }
    public ICommand QuitarLineaCommand { get; }
    public ICommand CambiarTipoLineaCommand { get; }
    public ICommand CambiarTipoCombustibleCommand { get; }

    private TipoInsumo _tipoLineaSeleccionado = TipoInsumo.Combustible;
    public TipoInsumo TipoLineaSeleccionado
    {
        get => _tipoLineaSeleccionado;
        set
        {
            if (SetProperty(ref _tipoLineaSeleccionado, value))
            {
                OnPropertyChanged(nameof(EsLineaCombustible));
                OnPropertyChanged(nameof(EsLineaRepuesto));
            }
        }
    }

    public bool EsLineaCombustible => TipoLineaSeleccionado == TipoInsumo.Combustible;
    public bool EsLineaRepuesto => TipoLineaSeleccionado == TipoInsumo.Repuesto;

    private TipoCombustible _tipoCombustibleLineaSeleccionado = TipoCombustible.Diesel;
    public TipoCombustible TipoCombustibleLineaSeleccionado
    {
        get => _tipoCombustibleLineaSeleccionado;
        set
        {
            if (SetProperty(ref _tipoCombustibleLineaSeleccionado, value))
            {
                OnPropertyChanged(nameof(EsLineaDiesel));
                OnPropertyChanged(nameof(EsLineaLubricante));
            }
        }
    }

    public bool EsLineaDiesel => TipoCombustibleLineaSeleccionado == TipoCombustible.Diesel;
    public bool EsLineaLubricante => TipoCombustibleLineaSeleccionado == TipoCombustible.Lubricante;

    private InventoryItem? _articuloLineaSeleccionado;
    public InventoryItem? ArticuloLineaSeleccionado
    {
        get => _articuloLineaSeleccionado;
        set => SetProperty(ref _articuloLineaSeleccionado, value);
    }

    private string _tipoLubricanteSeleccionado = string.Empty;
    public string TipoLubricanteSeleccionado
    {
        get => _tipoLubricanteSeleccionado;
        set => SetProperty(ref _tipoLubricanteSeleccionado, value);
    }

    private string _gradoViscosidadSeleccionado = string.Empty;
    public string GradoViscosidadSeleccionado
    {
        get => _gradoViscosidadSeleccionado;
        set => SetProperty(ref _gradoViscosidadSeleccionado, value);
    }

    /// <summary>
    /// Cada combinación Tipo × Grado es un artículo propio del catálogo, con su propio stock — un
    /// 15W40 es distinto de un 20W50. El código sale de la combinación, no lo teclea nadie, así
    /// que no hace falta pedir permiso para crearlo: no es un artículo de nombre libre, es una
    /// de las ~20 combinaciones posibles de una lista cerrada.
    /// </summary>
    private InventoryItem ObtenerOCrearArticuloLubricante(string tipo, string grado)
    {
        var prefijoTipo = tipo switch
        {
            "Mineral" => "MIN",
            "Sintético" => "SIN",
            "Semi-sintético" => "SEMI",
            _ => "OTR"
        };

        var codigo = $"LUB-{prefijoTipo}-{grado.Replace(" ", "")}";

        if (_articulos.GetById(codigo) is { } existente)
            return existente;

        return _articulos.Add(new InventoryItem
        {
            Codigo = codigo,
            Nombre = $"Aceite {tipo} {grado}",
            Categoria = CategoriaLubricantes,
            Unidad = "L"
        });
    }

    private string _cantidadLineaTexto = string.Empty;
    public string CantidadLineaTexto
    {
        get => _cantidadLineaTexto;
        set => SetProperty(ref _cantidadLineaTexto, value);
    }

    private void AgregarLinea()
    {
        if (!decimal.TryParse(CantidadLineaTexto, out var cantidad) || cantidad <= 0)
        {
            ErrorValidacion = "La cantidad de la línea debe ser un número mayor que cero.";
            return;
        }

        if (TipoLineaSeleccionado == TipoInsumo.Combustible && TipoCombustibleLineaSeleccionado == TipoCombustible.Diesel)
        {
            Lineas.Add(new RequisicionLinea
            {
                TipoInsumo = TipoInsumo.Combustible,
                TipoCombustibleSolicitado = TipoCombustible.Diesel,
                Cantidad = cantidad,
                UnidadTexto = "L"
            });
        }
        else if (TipoLineaSeleccionado == TipoInsumo.Combustible)
        {
            // Lubricante: ya no es un tipo de línea aparte — es un artículo real del catálogo de
            // Inventario (ver CategoriaLubricantes), así que arma una línea de Repuesto como
            // cualquier otra. Recepción de mercancía mueve su stock sin necesitar un tanque.
            //
            // TODO: por eso mismo hoy aparece como "Repuesto" en la columna Tipo, y su artículo
            // (LUB-...) cae dentro de Inventario · Repuestos junto a correas y rodamientos — es
            // lo único que ya sabía mover stock sin pedir un tanque. Si el negocio quiere que el
            // lubricante se vea/organice aparte de los repuestos mecánicos, hay que revisarlo
            // (¿una categoría propia en la pantalla de Repuestos? ¿un TipoInsumo.Lubricante nuevo
            // que reutilice el mismo movimiento de stock?).
            var lubricante = ObtenerOCrearArticuloLubricante(TipoLubricanteSeleccionado, GradoViscosidadSeleccionado);

            Lineas.Add(new RequisicionLinea
            {
                TipoInsumo = TipoInsumo.Repuesto,
                ArticuloCodigo = lubricante.Codigo,
                ArticuloNombre = lubricante.Nombre,
                Cantidad = cantidad,
                UnidadTexto = lubricante.Unidad
            });
        }
        else
        {
            if (ArticuloLineaSeleccionado is not { } articulo)
            {
                ErrorValidacion = "Seleccione el artículo de la línea.";
                return;
            }

            Lineas.Add(new RequisicionLinea
            {
                TipoInsumo = TipoInsumo.Repuesto,
                ArticuloCodigo = articulo.Codigo,
                ArticuloNombre = articulo.Nombre,
                Cantidad = cantidad,
                UnidadTexto = articulo.Unidad
            });
        }

        ErrorValidacion = null;
        CantidadLineaTexto = string.Empty;
    }

    protected override bool Validar(out string? error)
    {
        if (Lineas.Count == 0)
        {
            error = "Agregue al menos una línea a la requisición.";
            return false;
        }

        error = null;
        return true;
    }

    public override Requisicion ObtenerResultado()
    {
        var requisicion = _original.Clonar();
        requisicion.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return requisicion;
    }
}
