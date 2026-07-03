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
        var ventana = new CrudEditorWindow { DataContext = editor };

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
}
