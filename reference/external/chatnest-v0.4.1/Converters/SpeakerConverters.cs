using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ChatNest.Models;

namespace ChatNest.Converters
{
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

    public class SpeakerBorderThicknessConverter : IValueConverter
    {
        // Bubble is always to the right of the label, so left border for all speakers.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => new Thickness(3, 0, 0, 0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class SpeakerLabelAlignmentConverter : IValueConverter
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

    public class SpeakerButtonAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Speaker speaker)
            {
                return speaker == Speaker.自分
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right;
            }
            return HorizontalAlignment.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class SpeakerTextAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Speaker speaker)
            {
                return speaker == Speaker.自分
                    ? TextAlignment.Right
                    : TextAlignment.Left;
            }
            return TextAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
