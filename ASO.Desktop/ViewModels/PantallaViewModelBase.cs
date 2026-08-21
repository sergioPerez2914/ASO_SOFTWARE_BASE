using System;
using System.Windows.Input;
using ASO.Desktop.Models;
using ASO.Desktop.Navigation;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Base de toda pantalla que ocupa el area de contenido del shell: sabe donde esta en el
/// catalogo de modulos y sabe volver al resumen.
///
/// Los 17 ViewModels de pantalla repetian este preambulo palabra por palabra. Tenerlo en un
/// solo sitio tiene un segundo efecto, mas util que las lineas ahorradas: como todas las
/// pantallas comparten tipo, <c>MainWindow</c> puede enrutarlas con una tabla en vez de con
/// un switch de catorce casos identicos.
/// </summary>
public abstract class PantallaViewModelBase : ViewModelBase
{
    /// <summary>Se dispara al pedir volver al resumen del modulo; la ventana principal navega.</summary>
    public event EventHandler? VolverSolicitado;

    public Modulo Modulo { get; }

    /// <summary>
    /// Null en las secciones fijas que cuelgan del modulo y no de un submodulo
    /// (Administracion y Peticiones).
    /// </summary>
    public Submodulo? Submodulo { get; }

    public string Ruta => Submodulo is null ? Modulo.Nombre : $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }

    protected PantallaViewModelBase(Modulo modulo, Submodulo? submodulo = null)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
    }
}

/// <summary>
/// Pantalla que ADEMAS es el listado CRUD de un maestro (Tarifas, Repuestos, Liquidaciones…).
///
/// Repite los cuatro miembros de <see cref="PantallaViewModelBase"/> en vez de heredarlos, y no
/// es un descuido: C# no tiene herencia multiple y estas pantallas ya heredan de
/// <see cref="CrudViewModelBase{T, TId}"/>. La alternativa —mover el preambulo a la base CRUD—
/// se lo colgaria tambien a los siete ViewModels de padron que viven DENTRO de una pantalla
/// conmutable (<see cref="FincaCrudViewModel"/> y companneros), que no tienen modulo ninguno.
///
/// Dos copias de diez lineas, no diecisiete. Si un dia se toca una, tocar la otra.
/// </summary>
public abstract class PantallaCrudViewModel<T, TId> : CrudViewModelBase<T, TId>
    where T : IEntidad<TId>
{
    public event EventHandler? VolverSolicitado;

    public Modulo Modulo { get; }
    public Submodulo? Submodulo { get; }

    public string Ruta => Submodulo is null ? Modulo.Nombre : $"{Modulo.Nombre} · {Submodulo.Nombre}";

    public ICommand VolverCommand { get; }

    protected PantallaCrudViewModel(Modulo modulo,
                                    Submodulo? submodulo,
                                    ICrudDataSource<T, TId> source,
                                    IServicioDialogo? dialogo = null,
                                    ISesionActual? sesion = null)
        : base(source, dialogo, sesion)
    {
        Modulo = modulo;
        Submodulo = submodulo;

        VolverCommand = new RelayCommand(() => VolverSolicitado?.Invoke(this, EventArgs.Empty));
    }
}
