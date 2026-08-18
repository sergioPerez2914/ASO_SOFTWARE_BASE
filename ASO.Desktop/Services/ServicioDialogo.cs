using System.Windows;
using ASO.Desktop.ViewModels;
using ASO.Desktop.Views;

namespace ASO.Desktop.Services;

/// <summary>
/// Implementación por defecto de <see cref="IServicioDialogo"/> usando ventanas WPF.
/// </summary>
public class ServicioDialogo : IServicioDialogo
{
    public bool MostrarEditor(CrudEditorViewModelBase editor)
    {
        // El ancho se fija aquí y no por binding: el DataContext se asigna después de que el
        // XAML se parsea, y un formulario largo (la remesa) necesita más de los 420 por defecto.
        var ventana = new CrudEditorWindow { DataContext = editor, Width = editor.AnchoEditor };

        if (Application.Current?.MainWindow is { } owner && owner != ventana)
            ventana.Owner = owner;

        editor.SolicitarCierre += (_, guardo) => ventana.DialogResult = guardo;

        return ventana.ShowDialog() == true;
    }

    public bool Confirmar(string titulo, string mensaje)
    {
        var resultado = MessageBox.Show(mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return resultado == MessageBoxResult.Yes;
    }

    public void Informar(string titulo, string mensaje)
    {
        MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
