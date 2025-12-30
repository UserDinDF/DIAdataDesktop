using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace DIAdataDesktop.Converters
{
    public class ChangeToArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return "";
            if (!decimal.TryParse(value.ToString(), out var d)) return "";
            return d > 0 ? "▲" : d < 0 ? "▼" : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
