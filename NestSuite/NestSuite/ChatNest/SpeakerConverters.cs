using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NoteNest.NestSuite.ChatNest;

/// <summary>
/// 発言者ごとの吹き出し背景色。参照ソース ChatNest v0.4.1 Converters より取り込み。
/// </summary>
public class SpeakerBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Speaker speaker)
        {
            return speaker switch
            {
                Speaker.自分 => new SolidColorBrush(Color.FromRgb(232, 240, 254)),
                Speaker.反論 => new SolidColorBrush(Color.FromRgb(252, 228, 236)),
                Speaker.補足 => new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Speaker.結論 => new SolidColorBrush(Color.FromRgb(255, 248, 225)),
                _ => new SolidColorBrush(Colors.White)
            };
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 発言者ごとのアクセント色。参照ソース ChatNest v0.4.1 Converters より取り込み。
/// </summary>
public class SpeakerAccentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Speaker speaker)
        {
            return speaker switch
            {
                Speaker.自分 => new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Speaker.反論 => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                Speaker.補足 => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Speaker.結論 => new SolidColorBrush(Color.FromRgb(230, 119, 0)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 発言者ごとの吹き出し配置（自分＝右寄せ／他＝左寄せ）。参照ソース ChatNest v0.4.1 より取り込み。
/// </summary>
public class SpeakerAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Speaker speaker)
        {
            return speaker == Speaker.自分
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
