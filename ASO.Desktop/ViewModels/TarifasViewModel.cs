using System;
using System.Windows.Input;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Finanzas · Tarifas: el tarifario que alimenta la facturación al ingenio y la nómina por
/// destajo. Es un maestro, no un documento: no tiene máquina de estados, pero sí vigencia.
///
/// Editar o borrar una tarifa no altera lo ya emitido: cada factura y cada liquidación
/// guardan el monto que se les aplicó en el momento (ver <see cref="TarifaService"/>).
/// </summary>
public sealed class TarifasViewModel : PantallaCrudViewModel<Tarifa, int>
{
    private const string FiltroTodas = "Todas";

    private readonly TarifaService _servicio;
    private string _filtroAmbito = FiltroTodas;

    public TarifasViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearTarifas(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private TarifasViewModel(Modulo modulo,
                             Submodulo submodulo,
                             ITarifaDataSource tarifas,
                             IServicioDialogo dialogos,
                             ISesionActual sesion)
        : base(modulo, submodulo, tarifas, dialogos, sesion)
    {
        _servicio = new TarifaService(tarifas, sesion);

        CambiarFiltroAmbitoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroAmbito = filtro;
            ItemsView.Refresh();
        });
    }

    public ICommand CambiarFiltroAmbitoCommand { get; }

    protected override string ModuloPermiso => "Tarifas";

    protected override bool CoincideBusqueda(Tarifa item, string texto) =>
        item.Concepto.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.ServicioTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.UnidadTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.AmbitoTexto.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(Tarifa item) => _filtroAmbito switch
    {
        "Cobro" => item.Ambito == AmbitoTarifa.Cobro,
        "Pago" => item.Ambito == AmbitoTarifa.PagoDestajo,
        "Vigentes" => item.EstadoTexto == "Vigente",
        _ => true
    };

    protected override Tarifa CrearNuevo() => new()
    {
        Activa = true,
        VigenteDesde = DateTime.Today,
        Unidad = UnidadTarifa.Tonelada
    };

    protected override CrudEditorViewModelBase<Tarifa> CrearEditor(Tarifa item) =>
        new TarifaEditorViewModel(item, _servicio);
}
