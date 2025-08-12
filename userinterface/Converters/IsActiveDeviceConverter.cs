using Avalonia.Data.Converters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using userspace_backend;

namespace userinterface.Converters
{
    public class IsActiveDeviceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MultiHandleDevice device && !string.IsNullOrEmpty(device.id))
            {
                var backEnd = App.Services?.GetService<BackEnd>();

                if (backEnd?.Hardware.ActiveDevice != null)
                {
                    bool isActive = device.id.Equals(backEnd.Hardware.ActiveDevice.HardwareID.CurrentValidatedValue, StringComparison.OrdinalIgnoreCase);
                    return isActive;
                }
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}