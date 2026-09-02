using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Selección de remesas a facturar. Muestra solo las entregadas al central que aún no están en
/// ninguna factura, con el total estimado actualizándose a medida que se marcan, para que quien
/// factura vea lo que va a cobrar antes de generar el borrador.
/// </summary>
public sealed class GenerarFacturaEditorViewModel : CrudEditorViewModelBase
{
    public GenerarFacturaEditorViewModel(IReadOnlyList<Remesa> facturables, decimal tarifaTotalPorTonelada)
    {
        TarifaTotalPorTonelada = tarifaTotalPorTonelada;

        Remesas = [.. facturables.Select(r =>
        {
            var seleccion = new SeleccionRemesa(r);
            seleccion.PropertyChanged += (_, _) => ActualizarTotales();
            return seleccion;
        })];

        // Solo alcanzan a las visibles: con un rango puesto, "marcar todas" significa las de ese
        // rango, que es justo para lo que se puso el rango.
        MarcarTodasCommand = new RelayCommand(() => Marcar(true));
        QuitarSeleccionCommand = new RelayCommand(() => Marcar(false));

        ActualizarTotales();
    }

    private void Marcar(bool marcadas)
    {
        foreach (var fila in Remesas.Where(r => r.Visible))
            fila.IsSeleccionada = marcadas;
    }

    public override string Titulo => "Generar factura al ingenio";
    public override string TextoAccion => "Generar la factura";
    public override double AnchoEditor => Ancho.Estandar;

    public ObservableCollection<SeleccionRemesa> Remesas { get; }

    /// <summary>
    /// Marcar y desmarcar en bloque. Lo normal es facturar de una vez toda la semana —cinco o
    /// seis remesas—, y marcarlas una a una es teclear lo mismo seis veces; acotar antes por
    /// fecha deja "todas" queriendo decir "todas las de esta semana".
    /// </summary>
    public ICommand MarcarTodasCommand { get; }
    public ICommand QuitarSeleccionCommand { get; }

    private DateTime? _desde;
    public DateTime? Desde
    {
        get => _desde;
        set { if (SetProperty(ref _desde, value)) AplicarFiltro(); }
    }

    private DateTime? _hasta;
    public DateTime? Hasta
    {
        get => _hasta;
        set { if (SetProperty(ref _hasta, value)) AplicarFiltro(); }
    }

    /// <summary>
    /// El filtro solo esconde filas, no las desmarca: acotar la vista para marcar cómodo no
    /// puede sacar de la factura una remesa que ya se había marcado sin que nadie lo pida.
    /// </summary>
    private void AplicarFiltro()
    {
        foreach (var fila in Remesas)
            fila.Visible = EstaEnRango(fila.Remesa);

        ActualizarTotales();
    }

    private bool EstaEnRango(Remesa remesa)
    {
        var fecha = (remesa.LlegadaCentral ?? remesa.FechaConfirmacion)?.Date;
        if (fecha is null)
            return true;

        return (Desde is not { } desde || fecha >= desde.Date)
               && (Hasta is not { } hasta || fecha <= hasta.Date);
    }

    /// <summary>Suma de las tarifas de corte, alza y transporte: lo que rinde cada tonelada.</summary>
    public decimal TarifaTotalPorTonelada { get; }

    private string _totalTexto = string.Empty;
    public string TotalTexto
    {
        get => _totalTexto;
        private set => SetProperty(ref _totalTexto, value);
    }

    public IReadOnlyList<Remesa> Seleccionadas =>
        [.. Remesas.Where(r => r.IsSeleccionada).Select(r => r.Remesa)];

    public bool HayFacturables => Remesas.Count > 0;

    private void ActualizarTotales()
    {
        var toneladas = Remesas.Where(r => r.IsSeleccionada).Sum(r => r.Remesa.PesoNetoT ?? 0m);
        var estimado = toneladas * TarifaTotalPorTonelada;

        // Una remesa marcada que el rango dejó fuera de la vista SIGUE entrando en la factura. Se
        // dice en voz alta en vez de descontarla sola: acotar la vista no es quitar de la factura,
        // pero tampoco puede facturarse a espaldas de quien mira la lista.
        var ocultasMarcadas = Remesas.Count(r => r.IsSeleccionada && !r.Visible);
        var aviso = ocultasMarcadas > 0 ? $" ({ocultasMarcadas} fuera del rango)" : string.Empty;

        TotalTexto = toneladas > 0
            ? $"{Seleccionadas.Count} remesa(s) · {toneladas:N2} t · estimado {estimado:N2}{aviso}"
            : "Marque las remesas que entran en la factura.";
    }

    protected override bool Validar(out string? error)
    {
        if (!HayFacturables)
        {
            error = "No hay remesas recibidas pendientes de facturar.";
            return false;
        }

        if (Seleccionadas.Count == 0)
        {
            error = "Seleccione al menos una remesa.";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>Fila marcable de la lista de remesas facturables.</summary>
public sealed class SeleccionRemesa : ViewModelBase
{
    public SeleccionRemesa(Remesa remesa) => Remesa = remesa;

    public Remesa Remesa { get; }

    private bool _isSeleccionada;
    public bool IsSeleccionada
    {
        get => _isSeleccionada;
        set => SetProperty(ref _isSeleccionada, value);
    }

    /// <summary>La esconde el filtro por fechas; marcada sigue entrando en la factura.</summary>
    private bool _visible = true;
    public bool Visible
    {
        get => _visible;
        set => SetProperty(ref _visible, value);
    }

    public string Descripcion =>
        $"Nº {Remesa.Id} · {Remesa.FincaNombre} · {Remesa.PesoNetoT:N2} t";

    public string Detalle =>
        $"Recibida el {(Remesa.LlegadaCentral ?? Remesa.FechaConfirmacion):dd/MM/yyyy} · " +
        $"corte {Remesa.NucleoCorteCodigo} · alza {Remesa.NucleoAlzaEmpujeCodigo} · transporte {Remesa.NucleoTransporteCodigo}";
}
