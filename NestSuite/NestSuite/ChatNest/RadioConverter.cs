using System.Globalization;
using System.Windows.Data;

namespace NoteNest.NestSuite.ChatNest;

/// <summary>
/// 発言者選択ラジオボタンと <see cref="Speaker"/> を双方向バインドする Converter。
/// 参照ソース ChatNest v0.4.1 Converters/RadioConverter.cs より取り込み。
/// </summary>
public class RadioConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Speaker speaker && parameter is string paramStr)
        {
            if (Enum.TryParse<Speaker>(paramStr, out var paramSpeaker))
                return speaker == paramSpeaker;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramStr)
        {
            if (Enum.TryParse<Speaker>(paramStr, out var paramSpeaker))
                return paramSpeaker;
        }
        return Binding.DoNothing;
    }
}
