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
    /// <summary>Tipos de aceite. Re-expuestas desde <see cref="Lubricante"/> (fuente canónica)
    /// para no tocar los bindings <c>x:Static</c> de la vista.</summary>
    public static readonly IReadOnlyList<string> TiposLubricante = Lubricante.Tipos;

    /// <summary>Grados de viscosidad. Ver <see cref="TiposLubricante"/>.</summary>
    public static readonly IReadOnlyList<string> GradosViscosidadLubricante = Lubricante.GradosViscosidad;

    private readonly Requisicion _original;

    public RequisicionEditorViewModel(Requisicion original, IInventoryDataSource articulos)
    {
        _original = original;

        Articulos = articulos.GetAll().OrderBy(a => a.Nombre).ToList();

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
                OnPropertyChanged(nameof(EtiquetaCantidad));
            }
        }
    }

    public bool EsLineaCombustible => TipoLineaSeleccionado == TipoInsumo.Combustible;
    public bool EsLineaRepuesto => TipoLineaSeleccionado == TipoInsumo.Repuesto;

    /// <summary>Aclara que la cantidad de combustible se pide en litros, no en unidades de
    /// envase (eso se decide recién al armar la orden de compra).</summary>
    public string EtiquetaCantidad => EsLineaCombustible ? "Cantidad (litros)" : "Cantidad";

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
            // Lubricante: misma simetría que Diésel arriba — la requisición solo dice tipo y
            // grado, sin decidir marca ni catálogo concreto. Eso se elige recién al Recibir
            // mercancía, cuando se sabe qué trajo el proveedor (ver Lubricante.cs).
            Lineas.Add(new RequisicionLinea
            {
                TipoInsumo = TipoInsumo.Combustible,
                TipoCombustibleSolicitado = TipoCombustible.Lubricante,
                TipoLubricante = GradoViscosidadSeleccionado,
                ClaseLubricante = TipoLubricanteSeleccionado,
                Cantidad = cantidad,
                UnidadTexto = "L"
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
