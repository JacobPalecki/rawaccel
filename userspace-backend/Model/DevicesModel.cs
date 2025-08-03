using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Hardware;

namespace userspace_backend.Model
{
    public class DevicesModel
    {
        private readonly IDeviceInfoProvider? deviceInfoProvider;

        public DevicesModel() : this(null)
        {
        }

        public DevicesModel(IDeviceInfoProvider? deviceInfoProvider)
        {
            this.deviceInfoProvider = deviceInfoProvider;
            Devices = new ObservableCollection<DeviceModel>();
            DeviceGroups = new DeviceGroups([]);
            DeviceModelNameValidator = new DeviceModelNameValidator(this);
            DeviceModelHWIDValidator = new DeviceModelHWIDValidator(this);
            SystemDevices = new ObservableCollection<MultiHandleDevice>();
            RefreshSystemDevices();
        }

        public DeviceGroups DeviceGroups { get; set; }

        public IEnumerable<DeviceModel> DevicesEnumerable { get => Devices; }

        public ObservableCollection<DeviceModel> Devices { get; set; }

        public ObservableCollection<MultiHandleDevice> SystemDevices { get; protected set; }

        protected DeviceModelNameValidator DeviceModelNameValidator { get; }

        protected DeviceModelHWIDValidator DeviceModelHWIDValidator { get; }

        public bool DoesDeviceAlreadyExist(string name, string hwid)
        {
            return Devices.Any(d =>
                string.Equals(d.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(d.HardwareID.ModelValue, hwid, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool DoesDeviceNameAlreadyExist(string name)
        {
            return Devices.Any(d =>
                string.Equals(d.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool DoesDeviceHardwareIDAlreadyExist(string hwid)
        {
            return Devices.Any(d =>
                string.Equals(d.HardwareID.ModelValue, hwid, StringComparison.InvariantCultureIgnoreCase));
        }

        protected bool TryGetDefaultDevice([MaybeNullWhen(false)] out Device device)
        {
            for (int i = 0; i < 10; i++)
            {
                string deviceNameToAdd = $"Device{i}";
                if (DoesDeviceNameAlreadyExist(deviceNameToAdd))
                {
                    continue;
                }

                device = new()
                {
                    Name = deviceNameToAdd,
                    HWID = "",
                    DPI = 1600,
                    PollingRate = 1000,
                    DeviceGroup = "Default",
                };

                return true;
            }

            device = null;
            return false;
        }

        public bool TryAddDevice(Device? deviceData = null)
        {
            if (deviceData is null)
            {
                if (!TryGetDefaultDevice(out var defaultDevice))
                {
                    return false;
                }

                deviceData = defaultDevice;
            }
            else if (DoesDeviceAlreadyExist(deviceData.Name, deviceData.HWID))
            {
                return false;
            }

            DeviceGroupModel deviceGroup = DeviceGroups.AddOrGetDeviceGroup(deviceData.DeviceGroup);
            DeviceModel deviceModel = new DeviceModel(deviceData, deviceGroup, DeviceModelNameValidator, DeviceModelHWIDValidator);
            Devices.Add(deviceModel);

            return true;
        }

        public bool RemoveDevice(DeviceModel device)
        {
            return Devices.Remove(device);
        }

        public void RefreshSystemDevices()
        {
            SystemDevices.Clear();
            var systemDevicesList = MultiHandleDevice.GetList();
            foreach (var systemDevice in systemDevicesList)
            {
                SystemDevices.Add(systemDevice);
                
                // Also resolve and cache the clean product string for this device
                if (deviceInfoProvider != null && !string.IsNullOrEmpty(systemDevice.id))
                {
                    try
                    {
                        string productString = deviceInfoProvider.GetDeviceNameFromHardwareID(systemDevice.id);
                        // Update any existing configured devices with the resolved product string
                        UpdateDeviceProductString(systemDevice.id, productString);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        private void UpdateDeviceProductString(string hardwareId, string productString)
        {
            if (string.IsNullOrEmpty(hardwareId) || string.IsNullOrEmpty(productString))
                return;

            var matchingDevice = Devices.FirstOrDefault(d => 
                string.Equals(d.HardwareID.CurrentValidatedValue, hardwareId, StringComparison.OrdinalIgnoreCase));
            
            if (matchingDevice != null)
            {
                // Update the product string if it's different
                if (matchingDevice.ProductString.CurrentValidatedValue != productString)
                {
                    matchingDevice.ProductString.InterfaceValue = productString;
                    matchingDevice.ProductString.TryUpdateFromInterface();
                }
            }
        }

        public string GetExactDeviceNameFromHID(string hardwareId)
        {
            // First try to get from stored product string
            var configuredDevice = Devices.FirstOrDefault(d => 
                string.Equals(d.HardwareID.CurrentValidatedValue, hardwareId, StringComparison.OrdinalIgnoreCase));
            
            if (configuredDevice != null && !string.IsNullOrEmpty(configuredDevice.ProductString.CurrentValidatedValue))
            {
                return configuredDevice.ProductString.CurrentValidatedValue;
            }

            // Fallback to live resolution if device info provider available
            if (deviceInfoProvider != null)
            {
                return deviceInfoProvider.GetDeviceNameFromHardwareID(hardwareId);
            }

            // Final fallback to basic extraction
            return ExtractBasicNameFromHID(hardwareId);
        }

        public string GetProductStringFromHID(string hardwareId)
        {
            if (string.IsNullOrEmpty(hardwareId))
                return string.Empty;

            // Check configured devices first
            var configuredDevice = Devices.FirstOrDefault(d => 
                string.Equals(d.HardwareID.CurrentValidatedValue, hardwareId, StringComparison.OrdinalIgnoreCase));
            
            if (configuredDevice != null && !string.IsNullOrEmpty(configuredDevice.ProductString.CurrentValidatedValue))
            {
                return configuredDevice.ProductString.CurrentValidatedValue;
            }

            // Check system devices from last refresh
            var systemDevice = SystemDevices.FirstOrDefault(d => 
                string.Equals(d.id, hardwareId, StringComparison.OrdinalIgnoreCase));
            
            if (systemDevice != null && !string.IsNullOrEmpty(systemDevice.name))
            {
                return systemDevice.name;
            }

            return string.Empty;
        }

        public string GetExactDeviceNameFromHandle(IntPtr handle)
        {
            if (deviceInfoProvider != null)
            {
                return deviceInfoProvider.GetDeviceNameFromHandle(handle);
            }

            // Fallback to handle-based identification
            return $"Mouse Device ({handle.ToInt64():X})";
        }

        public DeviceInfo? GetDeviceInfoFromHandle(IntPtr handle)
        {
            return deviceInfoProvider?.GetDeviceInfoFromHandle(handle);
        }

        private string ExtractBasicNameFromHID(string hardwareID)
        {
            if (string.IsNullOrEmpty(hardwareID)) return "Unknown Device";

            try
            {
                if (hardwareID.Contains("VID_") && hardwareID.Contains("PID_"))
                {
                    int vidStart = hardwareID.IndexOf("VID_") + 4;
                    int pidStart = hardwareID.IndexOf("PID_") + 4;
                    
                    if (vidStart < hardwareID.Length - 4 && pidStart < hardwareID.Length - 4)
                    {
                        string vid = hardwareID.Substring(vidStart, 4);
                        string pid = hardwareID.Substring(pidStart, 4);
                        return $"Mouse (VID:{vid} PID:{pid})";
                    }
                }
            }
            catch
            {
                // Fallback if parsing fails
            }

            return "Mouse Device";
        }
    }

    public class DeviceModelNameValidator : IModelValueValidator<string>
    {
        public DeviceModelNameValidator(DevicesModel devices)
        {
            Devices = devices;
        }

        public DevicesModel Devices { get; }

        public bool Validate(string modelValue)
        {
            return !Devices.DoesDeviceNameAlreadyExist(modelValue);
        }
    }

    public class DeviceModelHWIDValidator : IModelValueValidator<string>
    {
        public DeviceModelHWIDValidator(DevicesModel devices)
        {
            Devices = devices;
        }

        public DevicesModel Devices { get; }

        public bool Validate(string modelValue)
        {
            return !Devices.DoesDeviceHardwareIDAlreadyExist(modelValue);
        }
    }

}
