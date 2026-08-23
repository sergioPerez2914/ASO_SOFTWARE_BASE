using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ASO.Desktop.Controls;

/// <summary>
/// Caja de búsqueda de un listado: glifo de lupa, texto de sugerencia y botón de limpiar.
///
/// Había catorce repartidas por las pantallas de listado, con tres anchos distintos para el mismo
/// control (260, 240 y 220 px, todos fijos) y sin etiqueta ni texto de sugerencia: la única pista
/// de qué buscaba cada una era un <c>ToolTip</c> que había que descubrir pasando el ratón.
/// </summary>
[TemplatePart(Name = ParteLimpiar, Type = typeof(ButtonBase))]
public class SearchBox : Control
{
    private const string ParteLimpiar = "PART_Limpiar";

    public static readonly DependencyProperty TextoProperty =
        DependencyProperty.Register(nameof(Texto), typeof(string), typeof(SearchBox),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty SugerenciaProperty =
        DependencyProperty.Register(nameof(Sugerencia), typeof(string), typeof(SearchBox),
            new PropertyMetadata("Buscar…"));

    public string Texto
    {
        get => (string)GetValue(TextoProperty);
        set => SetValue(TextoProperty, value);
    }

    /// <summary>Lo que se lee dentro de la caja vacía. Debe decir sobre qué campos busca.</summary>
    public string Sugerencia
    {
        get => (string)GetValue(SugerenciaProperty);
        set => SetValue(SugerenciaProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild(ParteLimpiar) is ButtonBase limpiar)
            limpiar.Click += (_, _) => Texto = string.Empty;
    }
}
