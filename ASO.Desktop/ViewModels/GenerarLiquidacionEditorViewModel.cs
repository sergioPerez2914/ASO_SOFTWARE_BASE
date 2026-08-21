using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Parámetros para generar una liquidación: a quién y por qué período. El cálculo lo hace
/// <see cref="LiquidacionService"/>; aquí solo se elige el sujeto y el rango.
/// </summary>
public sealed class GenerarLiquidacionEditorViewModel : CrudEditorViewModelBase
{
    public GenerarLiquidacionEditorViewModel(IEmpleadoDataSource empleados)
    {
        Empleados = empleados.GetAll().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();

        // Por defecto, la semana pasada completa: es el corte más habitual al liquidar.
        Hasta = DateTime.Today;
        Desde = DateTime.Today.AddDays(-7);

        EmpleadoSeleccionado = Empleados.FirstOrDefault();

        CambiarSujetoCommand = new RelayCommand<string>(sujeto =>
            SujetoTipo = sujeto == "Empleado" ? SujetoLiquidacion.Empleado : SujetoLiquidacion.Nucleo);
    }

    public override string Titulo => "Generar liquidación";
    public override double AnchoEditor => 480;

    public ICommand CambiarSujetoCommand { get; }

    public IReadOnlyList<Empleado> Empleados { get; }

    /// <summary>El núcleo a liquidar no se elige: es el de la instalación.</summary>
    public string NucleoTexto => Ambito.Actual is { } n ? $"{n.CodigoCam} · {n.Nombre}" : "—";

    private SujetoLiquidacion _sujetoTipo = SujetoLiquidacion.Nucleo;
    public SujetoLiquidacion SujetoTipo
    {
        get => _sujetoTipo;
        set
        {
            if (SetProperty(ref _sujetoTipo, value))
            {
                OnPropertyChanged(nameof(EsNucleo));
                OnPropertyChanged(nameof(EsEmpleado));
                OnPropertyChanged(nameof(AyudaSujeto));
            }
        }
    }

    public bool EsNucleo => SujetoTipo == SujetoLiquidacion.Nucleo;
    public bool EsEmpleado => SujetoTipo == SujetoLiquidacion.Empleado;

    public string AyudaSujeto => EsNucleo
        ? "Se liquidan las toneladas de las remesas confirmadas del período que aún no se hayan liquidado, por servicio (corte, alza y empuje, transporte)."
        : "Se liquidan las horas de las jornadas cerradas del período, según la tarifa horaria vigente.";

    private Empleado? _empleadoSeleccionado;
    public Empleado? EmpleadoSeleccionado
    {
        get => _empleadoSeleccionado;
        set => SetProperty(ref _empleadoSeleccionado, value);
    }

    private DateTime _desde;
    public DateTime Desde
    {
        get => _desde;
        set => SetProperty(ref _desde, value);
    }

    private DateTime _hasta;
    public DateTime Hasta
    {
        get => _hasta;
        set => SetProperty(ref _hasta, value);
    }

    protected override bool Validar(out string? error)
    {
        if (EsEmpleado && EmpleadoSeleccionado is null)
        {
            error = "Seleccione el empleado a liquidar.";
            return false;
        }

        if (Hasta.Date < Desde.Date)
        {
            error = "La fecha de fin no puede ser anterior a la de inicio.";
            return false;
        }

        error = null;
        return true;
    }
}
