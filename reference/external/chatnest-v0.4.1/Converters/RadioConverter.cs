using System;
using System.Globalization;
using System.Windows.Data;
using ChatNest.Models;

namespace ChatNest.Converters
{
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
}
