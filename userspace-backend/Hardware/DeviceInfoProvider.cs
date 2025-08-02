using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace userspace_backend.Hardware
{
    public class DeviceInfoProvider : IDeviceInfoProvider
    {
        private readonly Dictionary<IntPtr, DeviceInfo> deviceInfoCache = new Dictionary<IntPtr, DeviceInfo>();
        private readonly Dictionary<IntPtr, string> deviceNameCache = new Dictionary<IntPtr, string>();
        private readonly Dictionary<IntPtr, string> deviceHIDCache = new Dictionary<IntPtr, string>();
        private readonly Dictionary<string, string> hidToNameCache = new Dictionary<string, string>();

        // Raw Input structures and constants
        private const uint RIDI_DEVICENAME = 0x20000007;
        private const uint RIM_TYPEMOUSE = 0;
        private const uint RI_ERROR = uint.MaxValue;

        // HID constants
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint OPEN_EXISTING = 3;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const uint HID_STR_MAX_LEN = 127;

        // Device property keys
        private static readonly Guid DEVPKEY_Device_InstanceId = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);

        // P/Invoke declarations
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint CM_Get_Device_Interface_PropertyW(string pszDeviceInterface, ref Guid PropertyKey, out uint PropertyType, IntPtr PropertyBuffer, ref uint PropertyBufferSize, uint ulFlags);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool HidD_GetProductString(IntPtr HidDeviceObject, IntPtr Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool HidD_GetManufacturerString(IntPtr HidDeviceObject, IntPtr Buffer, uint BufferLength);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        public string GetDeviceNameFromHandle(IntPtr deviceHandle)
        {
            if (deviceHandle == IntPtr.Zero) return "Unknown Device";

            if (deviceNameCache.TryGetValue(deviceHandle, out string? cachedName))
            {
                return cachedName;
            }

            try
            {
                uint nameLength = 0;
                GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, IntPtr.Zero, ref nameLength);

                if (nameLength > 0)
                {
                    IntPtr nameBuffer = Marshal.AllocHGlobal((int)nameLength * 2);
                    try
                    {
                        if (GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, nameBuffer, ref nameLength) > 0)
                        {
                            string devicePath = Marshal.PtrToStringUni(nameBuffer) ?? "Unknown Device";
                            string deviceName = GetDeviceNameFromPath(devicePath);
                            
                            Debug.WriteLine($"\n=== Device Discovery ===\nFound device: {deviceName}\nDevice path: {devicePath}\nHandle: {deviceHandle.ToInt64():X}");
                            
                            deviceNameCache[deviceHandle] = deviceName;
                            return deviceName;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(nameBuffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== Device Name Error ===\nFailed to get name for handle {deviceHandle.ToInt64():X}: {ex.Message}");
            }

            string fallbackName = $"Mouse Device ({deviceHandle.ToInt64():X})";
            Debug.WriteLine($"\n=== Device Discovery (Fallback) ===\nUsing fallback name: {fallbackName}\nHandle: {deviceHandle.ToInt64():X}");
            deviceNameCache[deviceHandle] = fallbackName;
            return fallbackName;
        }

        public string GetDeviceHardwareIDFromHandle(IntPtr deviceHandle)
        {
            if (deviceHandle == IntPtr.Zero) return string.Empty;

            if (deviceHIDCache.TryGetValue(deviceHandle, out string? cachedHID))
            {
                return cachedHID;
            }

            try
            {
                uint nameLength = 0;
                GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, IntPtr.Zero, ref nameLength);

                if (nameLength > 0)
                {
                    IntPtr nameBuffer = Marshal.AllocHGlobal((int)nameLength * 2);
                    try
                    {
                        if (GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, nameBuffer, ref nameLength) > 0)
                        {
                            string devicePath = Marshal.PtrToStringUni(nameBuffer) ?? string.Empty;
                            
                            if (!string.IsNullOrEmpty(devicePath))
                            {
                                uint propSize = 0;
                                uint propType;
                                Guid deviceInstanceIdKey = DEVPKEY_Device_InstanceId;
                                
                                uint result = CM_Get_Device_Interface_PropertyW(devicePath, ref deviceInstanceIdKey, out propType, IntPtr.Zero, ref propSize, 0);
                                
                                if (result == 0x0000001A && propSize > 0) // CR_BUFFER_SMALL
                                {
                                    IntPtr propBuffer = Marshal.AllocHGlobal((int)propSize);
                                    try
                                    {
                                        result = CM_Get_Device_Interface_PropertyW(devicePath, ref deviceInstanceIdKey, out propType, propBuffer, ref propSize, 0);
                                        
                                        if (result == 0) // CR_SUCCESS
                                        {
                                            string instanceId = Marshal.PtrToStringUni(propBuffer) ?? string.Empty;
                                            
                                            int lastBackslash = instanceId.LastIndexOf('\\');
                                            string hardwareId = lastBackslash > 0 ? instanceId.Substring(0, lastBackslash) : instanceId;
                                            
                                            Debug.WriteLine($"\n=== Device HID Extraction ===\nDevice Path: {devicePath}\nInstance ID: {instanceId}\nHardware ID: {hardwareId}");
                                            
                                            deviceHIDCache[deviceHandle] = hardwareId;
                                            return hardwareId;
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(propBuffer);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(nameBuffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== Device HID Error ===\nFailed to get HID for handle {deviceHandle.ToInt64():X}: {ex.Message}");
            }

            string fallbackHID = $"HANDLE_{deviceHandle.ToInt64():X}";
            deviceHIDCache[deviceHandle] = fallbackHID;
            return fallbackHID;
        }

        public DeviceInfo GetDeviceInfoFromHandle(IntPtr deviceHandle)
        {
            if (deviceHandle == IntPtr.Zero)
            {
                return new DeviceInfo { Name = "Unknown Device", HardwareID = string.Empty, Handle = IntPtr.Zero };
            }

            if (deviceInfoCache.TryGetValue(deviceHandle, out DeviceInfo? cachedInfo))
            {
                return cachedInfo;
            }

            var deviceInfo = new DeviceInfo
            {
                Handle = deviceHandle,
                Name = GetDeviceNameFromHandle(deviceHandle),
                HardwareID = GetDeviceHardwareIDFromHandle(deviceHandle)
            };

            deviceInfoCache[deviceHandle] = deviceInfo;
            return deviceInfo;
        }

        public string GetDeviceNameFromHardwareID(string hardwareID)
        {
            if (string.IsNullOrEmpty(hardwareID)) return "Unknown Device";

            if (hidToNameCache.TryGetValue(hardwareID, out string? cachedName))
            {
                return cachedName;
            }

            try
            {
                var connectedDevices = GetAllConnectedDevices();
                var matchingDevice = connectedDevices.FirstOrDefault(d => 
                    string.Equals(d.HardwareID, hardwareID, StringComparison.OrdinalIgnoreCase));

                if (matchingDevice != null)
                {
                    hidToNameCache[hardwareID] = matchingDevice.Name;
                    return matchingDevice.Name;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== Device Name from HID Error ===\nFailed to resolve name for HID {hardwareID}: {ex.Message}");
            }

            return ExtractBasicNameFromHID(hardwareID);
        }

        public string GetDeviceNameFromPath(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return "Unknown Device";

            try
            {
                IntPtr hidDeviceObject = CreateFileW(devicePath, 0, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                
                if (hidDeviceObject != INVALID_HANDLE_VALUE)
                {
                    try
                    {
                        IntPtr productBuffer = Marshal.AllocHGlobal((int)HID_STR_MAX_LEN * 2);
                        IntPtr manufacturerBuffer = Marshal.AllocHGlobal((int)HID_STR_MAX_LEN * 2);
                        
                        try
                        {
                            bool hasProduct = HidD_GetProductString(hidDeviceObject, productBuffer, HID_STR_MAX_LEN);
                            bool hasManufacturer = HidD_GetManufacturerString(hidDeviceObject, manufacturerBuffer, HID_STR_MAX_LEN);
                            
                            if (hasProduct)
                            {
                                string productName = Marshal.PtrToStringUni(productBuffer) ?? "";
                                
                                if (hasManufacturer)
                                {
                                    string manufacturerName = Marshal.PtrToStringUni(manufacturerBuffer) ?? "";
                                    
                                    if (!string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(manufacturerName))
                                    {
                                        if (productName.StartsWith(manufacturerName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            return productName;
                                        }
                                        else
                                        {
                                            return $"{manufacturerName} {productName}";
                                        }
                                    }
                                }
                                
                                return !string.IsNullOrEmpty(productName) ? productName : "Mouse Device";
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(productBuffer);
                            Marshal.FreeHGlobal(manufacturerBuffer);
                        }
                    }
                    finally
                    {
                        CloseHandle(hidDeviceObject);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== HID Device Name Error ===\nFailed to get exact name for {devicePath}: {ex.Message}");
            }

            return ExtractBasicNameFromPath(devicePath);
        }

        public IEnumerable<DeviceInfo> GetAllConnectedDevices()
        {
            var devices = new List<DeviceInfo>();

            try
            {
                uint deviceCount = 0;
                
                if (GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))) == 0 && deviceCount > 0)
                {
                    IntPtr deviceListPtr = Marshal.AllocHGlobal((int)(deviceCount * Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))));
                    
                    try
                    {
                        if (GetRawInputDeviceList(deviceListPtr, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))) != RI_ERROR)
                        {
                            for (int i = 0; i < deviceCount; i++)
                            {
                                IntPtr currentDevicePtr = IntPtr.Add(deviceListPtr, i * Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
                                RAWINPUTDEVICELIST device = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(currentDevicePtr);
                                
                                if (device.dwType == RIM_TYPEMOUSE)
                                {
                                    var deviceInfo = GetDeviceInfoFromHandle(device.hDevice);
                                    devices.Add(deviceInfo);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(deviceListPtr);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== Device Enumeration Error ===\nFailed to enumerate devices: {ex.Message}");
            }

            return devices;
        }

        public void ClearCache()
        {
            deviceInfoCache.Clear();
            deviceNameCache.Clear();
            deviceHIDCache.Clear();
            hidToNameCache.Clear();
        }

        private string ExtractBasicNameFromPath(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return "Unknown Device";

            try
            {
                if (devicePath.Contains("VID_") && devicePath.Contains("PID_"))
                {
                    int vidStart = devicePath.IndexOf("VID_") + 4;
                    int pidStart = devicePath.IndexOf("PID_") + 4;
                    
                    if (vidStart < devicePath.Length - 4 && pidStart < devicePath.Length - 4)
                    {
                        string vid = devicePath.Substring(vidStart, 4);
                        string pid = devicePath.Substring(pidStart, 4);
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
}