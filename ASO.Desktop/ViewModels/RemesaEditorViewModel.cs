using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Alta y edición de la cabecera de una Remesa de caña. Los campos y su obligatoriedad
/// salen del reglamento de remesas del CAM ("Todos los datos deben ser llenados").
///
/// La llegada al central y el pesaje NO se editan aquí: los registra personal del central
/// en el paso de recepción (ver <see cref="RecepcionRemesaEditorViewModel"/>).
/// </summary>
public sealed class RemesaEditorViewModel : CrudEditorViewModelBase<Remesa>
{
    private const string FormatoHora = @"hh\:mm";

    private readonly Remesa _original;
    private readonly bool _esNuevo;

    public RemesaEditorViewModel(Remesa original,
                                 IFincaDataSource fincas,
                                 IPersonalCampoDataSource personal,
                                 IActivoFlotaDataSource flota)
    {
        _original = original;
        _esNuevo = original.Id == 0;

        Fincas = [.. fincas.GetAll()];
        Vehiculos = [.. flota.GetAll().Where(a => a.EsTransporte)];

        var todoElPersonal = personal.GetAll().ToList();
        Operadores = [.. todoElPersonal.Where(p => p.Rol == RolCampo.Operador)];
        Tractoristas = [.. todoElPersonal.Where(p => p.Rol == RolCampo.Tractorista)];
        Choferes = [.. todoElPersonal.Where(p => p.Rol == RolCampo.Chofer)];
        Remeseros = [.. todoElPersonal.Where(p => p.Rol == RolCampo.Remesero)];

        if (!_esNuevo)
            CargarDesde(original);
    }

    public override string Titulo => _esNuevo ? "Nueva remesa de caña" : $"Editar remesa Nº {_original.Id}";

    /// <summary>Son ~18 campos en dos columnas; con 420 px no entran.</summary>
    public override double AnchoEditor => Ancho.Amplio;

    // --- Catálogos ---
    public IReadOnlyList<Finca> Fincas { get; }
    /// <summary>Solo camiones y chutos: el catalogo de flota tambien trae maquinas de campo.</summary>
    public IReadOnlyList<ActivoFlota> Vehiculos { get; }
    public IReadOnlyList<PersonalCampo> Operadores { get; }
    public IReadOnlyList<PersonalCampo> Tractoristas { get; }
    public IReadOnlyList<PersonalCampo> Choferes { get; }
    public IReadOnlyList<PersonalCampo> Remeseros { get; }
    public IReadOnlyList<TipoCosecha> TiposCosecha { get; } = [TipoCosecha.Manual, TipoCosecha.Mecanizada];

    // --- Ubicación (cascada finca → lote → tablón) ---
    public ObservableCollection<Lote> Lotes { get; } = [];
    public ObservableCollection<Tablon> Tablones { get; } = [];

    private Finca? _fincaSeleccionada;
    public Finca? FincaSeleccionada
    {
        get => _fincaSeleccionada;
        set
        {
            if (!SetProperty(ref _fincaSeleccionada, value)) return;

            Lotes.Clear();
            Tablones.Clear();
            LoteSeleccionado = null;

            foreach (var lote in value?.Lotes ?? [])
                Lotes.Add(lote);
        }
    }

    private Lote? _loteSeleccionado;
    public Lote? LoteSeleccionado
    {
        get => _loteSeleccionado;
        set
        {
            if (!SetProperty(ref _loteSeleccionado, value)) return;

            Tablones.Clear();
            TablonSeleccionado = null;

            foreach (var tablon in value?.Tablones ?? [])
                Tablones.Add(tablon);
        }
    }

    private Tablon? _tablonSeleccionado;
    public Tablon? TablonSeleccionado
    {
        get => _tablonSeleccionado;
        set => SetProperty(ref _tablonSeleccionado, value);
    }

    private TipoCosecha _tipoCosecha = TipoCosecha.Mecanizada;
    public TipoCosecha TipoCosechaSeleccionado
    {
        get => _tipoCosecha;
        set => SetProperty(ref _tipoCosecha, value);
    }

    // --- Personal y vehículo ---
    private PersonalCampo? _operador;
    public PersonalCampo? OperadorSeleccionado
    {
        get => _operador;
        set
        {
            if (SetProperty(ref _operador, value))
                OnPropertyChanged(nameof(OperadorNucleoCodigo));
        }
    }

    /// <summary>C.O.D del operador: se deriva del catálogo, no se teclea.</summary>
    public string OperadorNucleoCodigo => OperadorSeleccionado?.NucleoCodigo ?? "—";

    private PersonalCampo? _tractorista;
    public PersonalCampo? TractoristaSeleccionado
    {
        get => _tractorista;
        set
        {
            if (SetProperty(ref _tractorista, value))
                OnPropertyChanged(nameof(TractoristaNucleoCodigo));
        }
    }

    public string TractoristaNucleoCodigo => TractoristaSeleccionado?.NucleoCodigo ?? "—";

    private PersonalCampo? _chofer;
    public PersonalCampo? ChoferSeleccionado
    {
        get => _chofer;
        set => SetProperty(ref _chofer, value);
    }

    private ActivoFlota? _vehiculo;
    public ActivoFlota? VehiculoSeleccionado
    {
        get => _vehiculo;
        set => SetProperty(ref _vehiculo, value);
    }

    private PersonalCampo? _remesero;
    public PersonalCampo? RemeseroSeleccionado
    {
        get => _remesero;
        set => SetProperty(ref _remesero, value);
    }

    /// <summary>
    /// C.O.D del núcleo, solo para mostrarlo. No se elige: una instalación atiende a un solo
    /// núcleo y los tres servicios de la remesa (corte, alza y empuje, transporte) los presta
    /// ese mismo núcleo.
    /// </summary>
    public string NucleoCodigo => Ambito.Actual?.CodigoCam ?? "—";

    public string NucleoNombre => Ambito.Actual?.Nombre ?? string.Empty;

    // --- Tiempos de carga (WPF no trae selector de hora: fecha + "HH:mm") ---
    private DateTime? _inicioCargaFecha = DateTime.Today;
    public DateTime? InicioCargaFecha
    {
        get => _inicioCargaFecha;
        set => SetProperty(ref _inicioCargaFecha, value);
    }

    private string _inicioCargaHora = string.Empty;
    public string InicioCargaHora
    {
        get => _inicioCargaHora;
        set => SetProperty(ref _inicioCargaHora, value);
    }

    private DateTime? _finCargaFecha = DateTime.Today;
    public DateTime? FinCargaFecha
    {
        get => _finCargaFecha;
        set => SetProperty(ref _finCargaFecha, value);
    }

    private string _finCargaHora = string.Empty;
    public string FinCargaHora
    {
        get => _finCargaHora;
        set => SetProperty(ref _finCargaHora, value);
    }

    private void CargarDesde(Remesa remesa)
    {
        FincaSeleccionada = Fincas.FirstOrDefault(f => f.Id == remesa.FincaId);
        LoteSeleccionado = Lotes.FirstOrDefault(l => l.Nombre == remesa.LoteNombre);
        TablonSeleccionado = Tablones.FirstOrDefault(t => t.Nombre == remesa.TablonNombre);
        TipoCosechaSeleccionado = remesa.TipoCosecha;

        OperadorSeleccionado = Operadores.FirstOrDefault(p => p.Id == remesa.OperadorId);
        TractoristaSeleccionado = Tractoristas.FirstOrDefault(p => p.Id == remesa.TractoristaId);
        ChoferSeleccionado = Choferes.FirstOrDefault(p => p.Id == remesa.ChoferId);
        VehiculoSeleccionado = Vehiculos.FirstOrDefault(v => v.Id == remesa.VehiculoId);
        RemeseroSeleccionado = Remeseros.FirstOrDefault(p => p.Id == remesa.RemeseroId);

        InicioCargaFecha = remesa.InicioCarga.Date;
        InicioCargaHora = remesa.InicioCarga.ToString(FormatoHora, CultureInfo.InvariantCulture);
        FinCargaFecha = remesa.FinCarga.Date;
        FinCargaHora = remesa.FinCarga.ToString(FormatoHora, CultureInfo.InvariantCulture);
    }

    protected override bool Validar(out string? error)
    {
        var faltantes = new List<string>();

        if (FincaSeleccionada is null) faltantes.Add("finca");
        if (LoteSeleccionado is null) faltantes.Add("lote");
        if (TablonSeleccionado is null) faltantes.Add("tablón");
        if (OperadorSeleccionado is null) faltantes.Add("operador");
        if (TractoristaSeleccionado is null) faltantes.Add("tractorista");
        if (ChoferSeleccionado is null) faltantes.Add("chofer");
        if (VehiculoSeleccionado is null) faltantes.Add("placa");
        if (RemeseroSeleccionado is null) faltantes.Add("remesero");
        if (InicioCargaFecha is null) faltantes.Add("fecha de inicio de carga");
        if (FinCargaFecha is null) faltantes.Add("fecha de fin de carga");

        var inicioValido = TryCombinar(InicioCargaFecha, InicioCargaHora, out var inicio);
        var finValido = TryCombinar(FinCargaFecha, FinCargaHora, out var fin);

        if (!inicioValido && InicioCargaFecha is not null) faltantes.Add("hora de inicio de carga (HH:mm)");
        if (!finValido && FinCargaFecha is not null) faltantes.Add("hora de fin de carga (HH:mm)");

        if (faltantes.Count > 0)
        {
            error = $"Complete los campos: {string.Join(", ", faltantes)}.";
            return false;
        }

        if (fin < inicio)
        {
            error = "El fin de carga no puede ser anterior al inicio de carga.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Combina la fecha del DatePicker con la hora tecleada en formato HH:mm.</summary>
    private static bool TryCombinar(DateTime? fecha, string hora, out DateTime resultado)
    {
        resultado = default;

        if (fecha is null ||
            !TimeSpan.TryParseExact(hora?.Trim(), FormatoHora, CultureInfo.InvariantCulture, out var tiempo))
            return false;

        resultado = fecha.Value.Date + tiempo;
        return true;
    }

    public override Remesa ObtenerResultado()
    {
        // Se parte del original para conservar Id, estado, auditoría y los datos de recepción.
        var remesa = _original.Clonar();

        remesa.FincaId = FincaSeleccionada!.Id;
        remesa.FincaCodigoCam = FincaSeleccionada.CodigoCam;
        remesa.FincaNombre = FincaSeleccionada.Nombre;
        remesa.LoteNombre = LoteSeleccionado!.Nombre;
        remesa.TablonNombre = TablonSeleccionado!.Nombre;
        remesa.TipoCosecha = TipoCosechaSeleccionado;

        remesa.OperadorId = OperadorSeleccionado!.Id;
        remesa.OperadorNombre = OperadorSeleccionado.Nombre;
        remesa.OperadorNucleoCodigo = OperadorSeleccionado.NucleoCodigo;

        remesa.TractoristaId = TractoristaSeleccionado!.Id;
        remesa.TractoristaNombre = TractoristaSeleccionado.Nombre;
        remesa.TractoristaNucleoCodigo = TractoristaSeleccionado.NucleoCodigo;

        remesa.ChoferId = ChoferSeleccionado!.Id;
        remesa.ChoferNombre = ChoferSeleccionado.Nombre;

        remesa.VehiculoId = VehiculoSeleccionado!.Id;
        remesa.VehiculoPlaca = VehiculoSeleccionado.Placa;

        remesa.RemeseroId = RemeseroSeleccionado!.Id;
        remesa.RemeseroNombre = RemeseroSeleccionado.Nombre;

        // Los tres servicios los presta el núcleo de la instalación; se estampan como texto
        // para que la remesa conserve el C.O.D con el que se emitió.
        var codigoCam = Ambito.ExigirCodigoCam();
        remesa.NucleoCorteCodigo = codigoCam;
        remesa.NucleoAlzaEmpujeCodigo = codigoCam;
        remesa.NucleoTransporteCodigo = codigoCam;

        TryCombinar(InicioCargaFecha, InicioCargaHora, out var inicio);
        TryCombinar(FinCargaFecha, FinCargaHora, out var fin);
        remesa.InicioCarga = inicio;
        remesa.FinCarga = fin;

        return remesa;
    }
}
