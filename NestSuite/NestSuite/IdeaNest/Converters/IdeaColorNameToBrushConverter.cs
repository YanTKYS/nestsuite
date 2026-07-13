using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NestSuite.IdeaNest.Converters;

public class IdeaColorNameToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush YellowBrush = CreateFrozenBrush("#FFF7CC");
    private static readonly SolidColorBrush PinkBrush = CreateFrozenBrush("#FCE7F3");
    private static readonly SolidColorBrush BlueBrush = CreateFrozenBrush("#DBEAFE");
    private static readonly SolidColorBrush GreenBrush = CreateFrozenBrush("#DCFCE7");
    private static readonly SolidColorBrush PurpleBrush = CreateFrozenBrush("#EDE9FE");
    private static readonly SolidColorBrush OrangeBrush = CreateFrozenBrush("#FFEDD5");
    private static readonly SolidColorBrush GrayBrush = CreateFrozenBrush("#F1F3F5");
    private static readonly SolidColorBrush WhiteBrush = CreateFrozenBrush("#FFFFFF");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var name = value as string ?? string.Empty;
        return name switch
        {
            "yellow" => YellowBrush,
            "pink"   => PinkBrush,
            "blue"   => BlueBrush,
            "green"  => GreenBrush,
            "purple" => PurpleBrush,
            "orange" => OrangeBrush,
            "gray"   => GrayBrush,
            "white"  => WhiteBrush,
            _        => WhiteBrush,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static SolidColorBrush CreateFrozenBrush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
