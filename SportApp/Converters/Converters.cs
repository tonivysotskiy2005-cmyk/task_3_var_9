using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SportApp.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(s)!;
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PercentToWidthConverter : IValueConverter
    {
        // value = percent (0..100), parameter = full width in px (string)
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            double percent = value is double d ? d : 0;
            double fullWidth = 100;
            if (parameter is string sp && double.TryParse(sp, NumberStyles.Any, CultureInfo.InvariantCulture, out double pw))
            {
                fullWidth = pw;
            }
            return Math.Max(0, Math.Min(fullWidth, (percent / 100.0) * fullWidth));
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
