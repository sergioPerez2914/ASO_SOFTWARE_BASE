using System.Windows;
using System.Windows.Controls;

namespace ASO.Desktop.Views;

/// <summary>
/// Vista reutilizable para secciones aún no implementadas. Mantiene el mismo
/// patrón visual (encabezado + tarjeta) que los módulos reales.
/// </summary>
public partial class PlaceholderView : UserControl
{
    public PlaceholderView(string titulo, string subtitulo, string fase = "", string icono = "")
    {
        InitializeComponent();

        TituloText.Text = titulo;
        SubtituloText.Text = subtitulo;
        IconoText.Text = icono;

        if (string.IsNullOrEmpty(fase))
            FaseBadge.Visibility = Visibility.Collapsed;
        else
            FaseText.Text = fase;
    }
}
