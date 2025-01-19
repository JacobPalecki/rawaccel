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
        public DevicesModel()
        {
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

        protected void RefreshSystemDevices()
        {
            SystemDevices.Clear();
            var systemDevicesList = MultiHandleDevice.GetList();
            foreach (var systemDevice in systemDevicesList)
            {
                SystemDevices.Add(systemDevice);
            }
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

    public interface ISystemDevicesProvider
    {
        ReadOnlyObservableCollection<ISystemDevice> SystemDevices { get; }

        void RefreshSystemDevices();
    }

    public class SystemDevicesProvider : ISystemDevicesProvider
    {
        public SystemDevicesProvider(ISystemDevicesRetriever devicesRetriever)
        {
            DevicesRetriever = devicesRetriever;
            SystemDevicesInternal = new ObservableCollection<ISystemDevice>();
            SystemDevices = new ReadOnlyObservableCollection<ISystemDevice>(SystemDevicesInternal);
            RefreshSystemDevices();
        }

        public ReadOnlyObservableCollection<ISystemDevice> SystemDevices { get; }

        protected ObservableCollection<ISystemDevice> SystemDevicesInternal { get; }

        protected ISystemDevicesRetriever DevicesRetriever { get; }

        public void RefreshSystemDevices()
        {
            // TODO: Replace with "addrange" equivalent from ObservableCollection child class
            SystemDevicesInternal.Clear();
            IList<ISystemDevice> retrievedDevices = DevicesRetriever.GetSystemDevices();

            foreach (ISystemDevice retrievedDevice in retrievedDevices)
            {
                SystemDevicesInternal.Add(retrievedDevice);
            }
        }
    }

    public interface ISystemDevicesRetriever
    {
        IList<ISystemDevice> GetSystemDevices();
    }

    public sealed class SystemDevicesRetriever : ISystemDevicesRetriever
    {
        public IList<ISystemDevice> GetSystemDevices()
        {
            IList<MultiHandleDevice> rawDevices = MultiHandleDevice.GetList();
            return rawDevices.Select(d => new SystemDevice(d) as ISystemDevice).ToList();
        }
    }

    /// <summary>
    /// Interface to represent devices as they come from windows.
    /// The actual classes from windows are non-trivial to construct and test.
    /// </summary>
    public interface ISystemDevice
    {
        public string Name { get; }

        public string HWID { get; }
    }

    /// <summary>
    /// Data class to wrap <see cref="MultiHandleDevice"/>
    /// </summary>
    public class SystemDevice : ISystemDevice
    {
        public SystemDevice(MultiHandleDevice multiHandleDevice)
        {
            RawDevice = multiHandleDevice;
        }

        public string Name { get => RawDevice.name; }

        public string HWID { get => RawDevice.id; }

        private MultiHandleDevice RawDevice { get; }
    }
}
