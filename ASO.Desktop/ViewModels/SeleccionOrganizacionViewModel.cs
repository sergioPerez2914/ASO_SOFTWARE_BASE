using System.Collections.Generic;
using System.Linq;
using ASO.Desktop.Configuration;
using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Selector de núcleo del Desarrollador. Es el único punto donde alguien elige sobre qué
/// organización trabaja; para todos los demás el ámbito lo fija su usuario y no se toca.
/// </summary>
public sealed class SeleccionOrganizacionViewModel : ViewModelBase
{
    private readonly ISesionActual _sesion;

    public SeleccionOrganizacionViewModel()
        : this(DataSourceFactory.CrearOrganizaciones(), SesionActual.Instancia)
    {
    }

    public SeleccionOrganizacionViewModel(IOrganizacionDataSource fuente, ISesionActual sesion)
    {
        _sesion = sesion;

        // La tabla de organizaciones no lleva filtro de ámbito — es la que lo define — así que
        // el permiso es toda la barrera. Sin él no se abre esta pantalla (lo comprueba MainWindow).
        Organizaciones = [.. fuente.GetAll().Where(o => o.Activa)];
        Seleccionada = Organizaciones.FirstOrDefault(o => o.Id == Ambito.OrganizacionId)
                       ?? Organizaciones.FirstOrDefault();
    }

    public IReadOnlyList<Organizacion> Organizaciones { get; }

    private Organizacion? _seleccionada;
    public Organizacion? Seleccionada
    {
        get => _seleccionada;
        set => SetProperty(ref _seleccionada, value);
    }

    public bool PuedeCambiar => _sesion.Puede(Permisos.Organizaciones.Cambiar);

    public string Mensaje => Organizaciones.Count == 0
        ? "No hay organizaciones activas registradas."
        : "Elige el núcleo sobre el que quieres trabajar. Verás exactamente lo mismo que vería su administrador.";

    /// <summary>Aplica el cambio de ámbito.</summary>
    /// <returns><c>true</c> si se cambió; <c>false</c> si no había nada que elegir o falta el permiso.</returns>
    public bool Aplicar()
    {
        if (!PuedeCambiar || Seleccionada is not { } organizacion)
            return false;

        Ambito.Cambiar(organizacion.Id);
        return true;
    }
}
