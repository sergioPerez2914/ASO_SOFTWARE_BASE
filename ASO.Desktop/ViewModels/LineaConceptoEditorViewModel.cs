using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Agrega un concepto de nómina (bono o deducción) a una liquidación en borrador.
/// El signo lo decide el concepto, no quien captura: un anticipo siempre resta.
/// </summary>
public sealed class LineaConceptoEditorViewModel : CrudEditorViewModelBase
{
    public LineaConceptoEditorViewModel(Liquidacion liquidacion, IConceptoNominaDataSource conceptos)
    {
        Resumen = $"{liquidacion.SujetoTexto} · {liquidacion.PeriodoTexto} · neto actual {liquidacion.NetoTexto}";
        Conceptos = conceptos.GetAll().Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
        ConceptoSeleccionado = Conceptos.FirstOrDefault();
    }

    public override string Titulo => "Agregar concepto";

    public string Resumen { get; }
    public IReadOnlyList<ConceptoNomina> Conceptos { get; }

    private ConceptoNomina? _conceptoSeleccionado;
    public ConceptoNomina? ConceptoSeleccionado
    {
        get => _conceptoSeleccionado;
        set
        {
            if (SetProperty(ref _conceptoSeleccionado, value))
                OnPropertyChanged(nameof(EfectoTexto));
        }
    }

    private string _monto = string.Empty;
    public string Monto
    {
        get => _monto;
        set => SetProperty(ref _monto, value);
    }

    public string EfectoTexto => ConceptoSeleccionado is { } c
        ? c.Tipo == TipoConcepto.Deduccion
            ? "Es una deducción: se resta del neto a pagar."
            : "Es un devengo: se suma al neto a pagar."
        : "Seleccione el concepto.";

    /// <summary>Monto ya convertido, para pasárselo al servicio.</summary>
    public decimal MontoValor => decimal.TryParse(Monto, out var monto) ? monto : 0m;

    protected override bool Validar(out string? error)
    {
        if (ConceptoSeleccionado is null)
        {
            error = "Seleccione el concepto a agregar.";
            return false;
        }

        if (!decimal.TryParse(Monto, out var monto) || monto <= 0)
        {
            error = "El monto debe ser un número mayor que cero.";
            return false;
        }

        error = null;
        return true;
    }
}
