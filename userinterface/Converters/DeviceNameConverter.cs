using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace userinterface.Converters
{
    public class DeviceNameConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MultiHandleDevice device)
            {
                return string.IsNullOrWhiteSpace(device.name) ? device.id : device.name;
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}