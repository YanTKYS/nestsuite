using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NestSuite.Converters;

/// <summary>
/// v2.13.5 M16 フォローアップ: NoteNest 右ペインのタスク行を、既存タスクがない場合は
/// コンテンツに合わせて縮め、ある場合は従来どおり 2* 比率で確保する。
/// </summary>
public class HasNoTasksToRowHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? new GridLength(0, GridUnitType.Auto) : new GridLength(2, GridUnitType.Star);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
