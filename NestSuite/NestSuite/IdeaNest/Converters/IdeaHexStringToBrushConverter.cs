using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NestSuite.IdeaNest.Converters;

public class IdeaHexStringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(s);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
            catch
            {
                // fall through
            }
        }
        return Brushes.LightYellow;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
