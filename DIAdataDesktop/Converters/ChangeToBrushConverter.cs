using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace DIAdataDesktop.Converters
{
    public class ChangeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            if (!decimal.TryParse(value.ToString(), out var d))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

            if (d > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")); // green
            if (d < 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // red
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")); // neutral
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
