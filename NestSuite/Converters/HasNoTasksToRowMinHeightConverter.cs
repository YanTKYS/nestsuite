using System.Globalization;
using System.Windows.Data;

namespace NestSuite.Converters;

/// <summary>
/// v2.13.5 M16 フォローアップ: NoteNest 右ペインのタスク行の最小高さを、既存タスクがない場合は
/// 0（コンテンツに合わせて縮む）にし、ある場合は従来どおり 100 を維持する。
/// </summary>
public class HasNoTasksToRowMinHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 0.0 : 100.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
