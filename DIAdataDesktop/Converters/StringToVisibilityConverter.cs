using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace DIAdataDesktop.Converters
{
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } 

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            var hasText = !string.IsNullOrWhiteSpace(s);

            if (parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase))
                hasText = !hasText;

            if (Invert) hasText = !hasText;

            return hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }
}
