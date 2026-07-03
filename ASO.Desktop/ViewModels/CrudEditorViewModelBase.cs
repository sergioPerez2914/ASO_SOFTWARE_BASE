using System;
using System.Windows.Input;

namespace ASO.Desktop.ViewModels;

/// <summary>
/// Base no genérica del editor modal de alta/edición, para que <see cref="Services.IServicioDialogo"/>
/// y <c>CrudEditorWindow</c> puedan trabajar con cualquier editor sin conocer la entidad concreta.
/// </summary>
public abstract class CrudEditorViewModelBase : ViewModelBase
{
    public abstract string Titulo { get; }

    private string? _errorValidacion;
    public string? ErrorValidacion
    {
        get => _errorValidacion;
        protected set => SetProperty(ref _errorValidacion, value);
    }

    /// <summary>Se dispara cuando el editor quiere cerrarse: <c>true</c> si guardó, <c>false</c> si canceló.</summary>
    public event EventHandler<bool>? SolicitarCierre;

    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    protected CrudEditorViewModelBase()
    {
        GuardarCommand = new RelayCommand(Guardar);
        CancelarCommand = new RelayCommand(() => SolicitarCierre?.Invoke(this, false));
    }

    private void Guardar()
    {
        if (!Validar(out var error))
        {
            ErrorValidacion = error;
            return;
        }

        ErrorValidacion = null;
        SolicitarCierre?.Invoke(this, true);
    }

    /// <summary>Valida los campos editables. Si devuelve <c>false</c>, <paramref name="error"/> se muestra al usuario.</summary>
    protected abstract bool Validar(out string? error);
}

/// <summary>
/// Editor de alta/edición para una entidad concreta. Trabaja siempre sobre una copia
/// editable; la entidad original de la lista no se modifica hasta que la fuente de
/// datos confirma el guardado.
/// </summary>
public abstract class CrudEditorViewModelBase<T> : CrudEditorViewModelBase
{
    protected CrudEditorViewModelBase(T original)
    {
    }

    /// <summary>Reconstruye la entidad con los valores editados (conservando el <c>Id</c> original).</summary>
    public abstract T ObtenerResultado();
}
