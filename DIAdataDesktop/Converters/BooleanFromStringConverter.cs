using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace DIAdataDesktop.Converters
{
    public class BooleanFromStringConverter : IValueConverter
    {
        public static BooleanFromStringConverter SymbolMode { get; } = new("Symbol");
        public static BooleanFromStringConverter AddressMode { get; } = new("Address");

        private readonly string _match;
        private BooleanFromStringConverter(string match) => _match = match;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Equals(value as string, _match, StringComparison.OrdinalIgnoreCase);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? _match : System.Windows.Data.Binding.DoNothing;
    }
}
