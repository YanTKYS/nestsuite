using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace NoteNest.NestSuite.IdeaNest.Converters;

public class IdeaBodyTrimConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not string body || values[1] is not int maxLines || maxLines <= 0)
            return values.Length > 0 ? (values[0] ?? "") : "";

        var lines = body.Split('\n');
        if (lines.Length <= maxLines) return body;
        return string.Join("\n", lines.Take(maxLines)) + "…";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
