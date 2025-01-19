using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DevicesModel
    {
        public DevicesModel(ISystemDevicesProvider systemDevicesProvider)
        {
            Devices = new ObservableCollection<DeviceModel>();
            DeviceGroups = new DeviceGroups([]);
            DeviceModelNameValidator = new DeviceModelNameValidator(this);
            DeviceModelHWIDValidator = new DeviceModelHWIDValidator(this);
            SystemDevices = systemDevicesProvider;
        }

        public DeviceGroups DeviceGroups { get; set; }

        public IEnumerable<DeviceModel> DevicesEnumerable { get => Devices; }

        public ObservableCollection<DeviceModel> Devices { get; set; }


        public ISystemDevicesProvider SystemDevices { get; protected set; }

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
