using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend.Model;
using userspace_backend.Data;

namespace userspace_backend.Hardware
{
    public class HardwareManager
    {
        private readonly DevicesModel devices;
        private readonly Dictionary<IntPtr, string> handleToHIDMap = new Dictionary<IntPtr, string>();

        public HardwareManager(DevicesModel devices)
        {
            this.devices = devices;
        }

        private DeviceModel? activeDevice;

        public DeviceModel? ActiveDevice
        {
            get
            {
                if (activeDevice == null && !string.IsNullOrEmpty(CurrentInputDeviceHID))
                {
                    activeDevice = FindDeviceByHID(CurrentInputDeviceHID);
                    if (activeDevice == null)
                    {
                        activeDevice = CreateTemporaryDeviceModel();
                    }
                }
                return activeDevice;
            }
            set
            {
                activeDevice = value;
            }
        }

        public IntPtr CurrentInputDeviceHandle { get; set; } = IntPtr.Zero;

        public string CurrentInputDeviceHID { get; set; } = string.Empty;

        public string CurrentInputDeviceName { get; set; } = string.Empty;

        public void UpdateCurrentInputDevice(IntPtr handle, string hid, string name)
        {
            CurrentInputDeviceHandle = handle;
            CurrentInputDeviceHID = hid;
            CurrentInputDeviceName = name;
            
            if (hid.StartsWith("HANDLE_"))
            {
                if (!handleToHIDMap.ContainsKey(handle))
                {
                    var systemDevice = devices.SystemDevices.FirstOrDefault(d => 
                        !string.IsNullOrEmpty(d.name) && d.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    
                    if (systemDevice != null && !string.IsNullOrEmpty(systemDevice.id))
                    {
                        handleToHIDMap[handle] = systemDevice.id;
                        CurrentInputDeviceHID = systemDevice.id;
                    }
                }
                else
                {
                    CurrentInputDeviceHID = handleToHIDMap[handle];
                }
            }
            
            if (activeDevice == null && !string.IsNullOrEmpty(CurrentInputDeviceHID) && !CurrentInputDeviceHID.StartsWith("HANDLE_"))
            {
                activeDevice = FindDeviceByHID(CurrentInputDeviceHID);
            }
        }

        public DeviceModel? FindDeviceByHID(string hid)
        {
            if (string.IsNullOrEmpty(hid)) return null;
            
            return devices.Devices.FirstOrDefault(device => 
                string.Equals(device.HardwareID.CurrentValidatedValue, hid, StringComparison.OrdinalIgnoreCase));
        }

        public (string deviceName, int sourceDPI, bool isKnownDevice) GetCurrentDeviceInfo()
        {
            if (string.IsNullOrEmpty(CurrentInputDeviceHID))
            {
                return ("No device detected", 1000, false);
            }

            var device = ActiveDevice;
            if (device != null)
            {
                string deviceName = !string.IsNullOrEmpty(device.Name.CurrentValidatedValue) 
                    ? device.Name.CurrentValidatedValue 
                    : device.ProductString.CurrentValidatedValue;
                
                if (string.IsNullOrEmpty(deviceName))
                {
                    deviceName = "Configured Device";
                }
                
                return (deviceName, device.DPI.CurrentValidatedValue, true);
            }

            string productString = devices.GetProductStringFromHID(CurrentInputDeviceHID);
            if (!string.IsNullOrEmpty(productString))
            {
                return (productString, 1000, false);
            }

            if (!string.IsNullOrEmpty(CurrentInputDeviceName))
            {
                return (CurrentInputDeviceName, 1000, false);
            }

            return ("Unknown Device", 1000, false);
        }

        public string ResolveDeviceNameFromHID(string hid)
        {
            if (string.IsNullOrEmpty(hid)) return "Unknown Device";

            string productString = devices.GetProductStringFromHID(hid);
            if (!string.IsNullOrEmpty(productString))
            {
                return productString;
            }

            if (!string.IsNullOrEmpty(CurrentInputDeviceName))
            {
                return CurrentInputDeviceName;
            }

            return ExtractBasicNameFromHID(hid);
        }

        public string GetCurrentActiveDeviceName()
        {
            var (deviceName, _, _) = GetCurrentDeviceInfo();
            return deviceName;
        }

        public (string userConfiguredName, string productString, bool hasProductString) GetCurrentDeviceDisplayInfo()
        {
            if (string.IsNullOrEmpty(CurrentInputDeviceHID))
            {
                return (string.Empty, string.Empty, false);
            }

            var device = ActiveDevice;
            if (device != null)
            {
                string userConfiguredName = device.Name.CurrentValidatedValue;
                string productString = device.ProductString.CurrentValidatedValue;
                
                return (userConfiguredName, productString, !string.IsNullOrEmpty(productString));
            }

            string detectedProductString = devices.GetProductStringFromHID(CurrentInputDeviceHID);
            return (string.Empty, detectedProductString, !string.IsNullOrEmpty(detectedProductString));
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
            }

            return "Mouse Device";
        }

        private DeviceModel? CreateTemporaryDeviceModel()
        {
            if (string.IsNullOrEmpty(CurrentInputDeviceHID))
                return null;

            string productString = devices.GetProductStringFromHID(CurrentInputDeviceHID);
            if (string.IsNullOrEmpty(productString))
            {
                productString = CurrentInputDeviceName;
            }
            if (string.IsNullOrEmpty(productString))
            {
                productString = ExtractBasicNameFromHID(CurrentInputDeviceHID);
            }

            var tempDevice = new Device
            {
                Name = productString,
                HWID = CurrentInputDeviceHID,
                ProductString = productString,
                DPI = 1000,
                PollingRate = 1000,
                Ignore = false,
                DeviceGroup = "Default"
            };

            var defaultDeviceGroup = DeviceGroups.DefaultDeviceGroup;
            var nameValidator = new DeviceModelNameValidator(devices);
            var hwidValidator = new DeviceModelHWIDValidator(devices);

            return new DeviceModel(tempDevice, defaultDeviceGroup, nameValidator, hwidValidator);
        }
    }
}