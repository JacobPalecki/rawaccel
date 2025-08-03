using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using Microsoft.Extensions.DependencyInjection;
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
                    if (isActive)
                    {
                        string deviceDisplayName = string.IsNullOrWhiteSpace(device.name) ? device.id : device.name;
                        Debug.WriteLine($"=== Active Device Match ===\nDevice: {deviceDisplayName}\nActive Device: {backEnd.Hardware.ActiveDevice.Name.CurrentValidatedValue}\nMatch: TRUE");
                    }
                    Debug.WriteLine($"=== Active Device ===\nDevice: {device.id}\nActive Device: {backEnd.Hardware.ActiveDevice.HardwareID.CurrentValidatedValue}\nMatch: TRUE");
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