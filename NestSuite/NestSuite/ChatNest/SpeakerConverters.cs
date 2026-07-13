using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NestSuite.ChatNest;

/// <summary>
/// ChatNest の発言者表示で使う色と配置を集約する。
/// Converter 呼び出しごとの Brush 生成を避け、XAML 側の認知負荷と描画時の割り当てを抑える。
/// </summary>
internal static class SpeakerVisualPalette
{
    public static readonly Brush OwnBackground = CreateBrush(232, 240, 254);
    public static readonly Brush ObjectionBackground = CreateBrush(252, 228, 236);
    public static readonly Brush SupplementBackground = CreateBrush(232, 245, 233);
    public static readonly Brush ConclusionBackground = CreateBrush(255, 248, 225);
    public static readonly Brush DefaultBackground = CreateBrush(Colors.White);

    public static readonly Brush OwnAccent = CreateBrush(25, 118, 210);
    public static readonly Brush ObjectionAccent = CreateBrush(198, 40, 40);
    public static readonly Brush SupplementAccent = CreateBrush(46, 125, 50);
    public static readonly Brush ConclusionAccent = CreateBrush(230, 119, 0);
    public static readonly Brush DefaultAccent = CreateBrush(Colors.Gray);

    public static Brush GetBackground(object value)
        => value is Speaker speaker
            ? speaker switch
            {
                Speaker.自分 => OwnBackground,
                Speaker.反論 => ObjectionBackground,
                Speaker.補足 => SupplementBackground,
                Speaker.結論 => ConclusionBackground,
                _ => DefaultBackground
            }
            : DefaultBackground;

    public static Brush GetAccent(object value)
        => value is Speaker speaker
            ? speaker switch
            {
                Speaker.自分 => OwnAccent,
                Speaker.反論 => ObjectionAccent,
                Speaker.補足 => SupplementAccent,
                Speaker.結論 => ConclusionAccent,
                _ => DefaultAccent
            }
            : DefaultAccent;

    public static HorizontalAlignment GetAlignment(object value)
        => value is Speaker.自分 ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    private static Brush CreateBrush(byte red, byte green, byte blue) => CreateBrush(Color.FromRgb(red, green, blue));

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// 発言者ごとの吹き出し背景色。参照ソース ChatNest v0.4.1 Converters より取り込み。
/// </summary>
public class SpeakerBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => SpeakerVisualPalette.GetBackground(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 発言者ごとのアクセント色。参照ソース ChatNest v0.4.1 Converters より取り込み。
/// </summary>
public class SpeakerAccentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => SpeakerVisualPalette.GetAccent(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 発言者ごとの吹き出し配置（自分＝右寄せ／他＝左寄せ）。参照ソース ChatNest v0.4.1 より取り込み。
/// </summary>
public class SpeakerAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => SpeakerVisualPalette.GetAlignment(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
