using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NestSuite.Converters;

public class WorkspaceKindToAccentBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush NoteNestBrush = CreateFrozenBrush(0x4A, 0x90, 0xD9);
    private static readonly SolidColorBrush IdeaNestBrush = CreateFrozenBrush(0xE8, 0xA0, 0x20);
    private static readonly SolidColorBrush ChatNestBrush = CreateFrozenBrush(0x4C, 0xAF, 0x50);
    private static readonly SolidColorBrush TempBrush = CreateFrozenBrush(0xA0, 0xA0, 0xA8);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is NestSuiteWorkspaceKind kind
            ? kind switch
            {
                NestSuiteWorkspaceKind.NoteNest => NoteNestBrush,
                NestSuiteWorkspaceKind.IdeaNest => IdeaNestBrush,
                NestSuiteWorkspaceKind.ChatNest  => ChatNestBrush,
                NestSuiteWorkspaceKind.Temp => TempBrush,
                _ => Brushes.Transparent
            }
            : (object)Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static SolidColorBrush CreateFrozenBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
