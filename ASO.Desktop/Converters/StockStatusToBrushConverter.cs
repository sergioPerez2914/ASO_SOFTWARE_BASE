using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ASO.Desktop.Models;

namespace ASO.Desktop.Converters;

/// <summary>
/// Convierte un <see cref="StockStatus"/> en un pincel para las etiquetas de estado.
/// ConverterParameter="bg" devuelve un fondo tenue; "fg" devuelve el color pleno del texto.
/// </summary>
public class StockStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = value is StockStatus status
            ? status switch
            {
                StockStatus.Agotado => (Color)ColorConverter.ConvertFromString("#DC2626"),
                StockStatus.Bajo => (Color)ColorConverter.ConvertFromString("#F59E0B"),
                _ => (Color)ColorConverter.ConvertFromString("#16A34A")
            }
            : Colors.Gray;

        if (parameter as string == "bg")
            return new SolidColorBrush(color) { Opacity = 0.15 };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
