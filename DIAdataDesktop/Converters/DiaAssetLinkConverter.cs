using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace DIAdataDesktop.Converters
{
    public sealed class DiaAssetLinkConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            var name = values.Length > 0 ? values[0]?.ToString() : null;
            var address = values.Length > 1 ? values[1]?.ToString() : null;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
                return null;

            name = Uri.EscapeDataString(name.Trim());
            address = Uri.EscapeDataString(address.Trim());

            var url = $"https://www.diadata.org/app/price/asset/{name}/{address}/";
            return new Uri(url);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
