using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace DIAdataDesktop.Converters
{
    public sealed class StringEqualsToBoolConverter : IValueConverter
    {
        // Convert: Mode(string) -> bool (RadioButton.IsChecked)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = value?.ToString();
            var desired = parameter?.ToString();
            return string.Equals(current, desired, StringComparison.OrdinalIgnoreCase);
        }

        // ConvertBack: bool -> Mode(string)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return parameter?.ToString() ?? "Symbol";

            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
