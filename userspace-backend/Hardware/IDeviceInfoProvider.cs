using System;
using System.Collections.Generic;

namespace userspace_backend.Hardware
{
    public class DeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string HardwareID { get; set; } = string.Empty;
        public IntPtr Handle { get; set; } = IntPtr.Zero;
        public string DevicePath { get; set; } = string.Empty;
    }

    public interface IDeviceInfoProvider
    {
        string GetDeviceNameFromHandle(IntPtr deviceHandle);
        
        string GetDeviceHardwareIDFromHandle(IntPtr deviceHandle);
        
        DeviceInfo GetDeviceInfoFromHandle(IntPtr deviceHandle);
        
        string GetDeviceNameFromHardwareID(string hardwareID);
        
        string GetDeviceNameFromPath(string devicePath);
        
        void ClearCache();
        
        IEnumerable<DeviceInfo> GetAllConnectedDevices();
    }
}