using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace userspace_backend.Hardware
{
    public class MouseMovementEventArgs : EventArgs
    {
        public double MouseSpeed { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double OutputSpeed { get; set; }
        public double XSpeed { get; set; }
        public double YSpeed { get; set; }
        public IntPtr DeviceHandle { get; set; }
        public string DeviceName { get; set; } = string.Empty;
    }

    public interface IMouseTracker : IDisposable
    {
        event EventHandler<MouseMovementEventArgs>? MouseMoved;
        event EventHandler? MouseIdle;
        bool IsTracking { get; }
        void SetWindowHandle(IntPtr hwnd);
        void SetDeviceDPI(int dpi);
        void SetBackEnd(BackEnd backEnd);
        void SetDeviceInfoProvider(IDeviceInfoProvider deviceInfoProvider);
        void StartTracking();
        void StopTracking();
        void ProcessRawInput(IntPtr lParam);
    }

    public class MouseTracker : IMouseTracker
    {
        private readonly Timer throttleTimer;
        private readonly Timer idleTimer;
        private volatile MouseMovementEventArgs? lastEventArgs;
        private volatile bool isTracking = false;
        private IntPtr hwndSource = IntPtr.Zero;
        private bool disposed = false;
        private const int IdleTimeoutMs = 1000; // 1 second of no movement = idle

        // Raw Input structures and constants
        private const int WM_INPUT = 0x00FF;
        private const int RID_INPUT = 0x10000003;
        private const int RIM_TYPEMOUSE = 0;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const uint RIDEV_REMOVE = 0x00000001;
        private const uint RIDI_DEVICENAME = 0x20000007;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public ushort usButtonFlags;
            public ushort usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long performanceFrequency;
        private long lastPerformanceCounter = 0;
        private uint lastTickCount = 0;
        private double lastX = 0;
        private double lastY = 0;
        private bool useHighPrecisionTiming = false;
        private int deviceDPI = 1000; // Default to normalized DPI
        private const double DriverNormalizedDPI = 1000.0;
        private readonly Dictionary<IntPtr, string> deviceNameCache = new Dictionary<IntPtr, string>();
        private readonly Dictionary<IntPtr, string> deviceHIDCache = new Dictionary<IntPtr, string>();
        private IntPtr lastActiveDeviceHandle = IntPtr.Zero;
        private string lastActiveDeviceName = string.Empty;
        private BackEnd? backEnd;
        private IDeviceInfoProvider? deviceInfoProvider;

        public event EventHandler<MouseMovementEventArgs>? MouseMoved;
        public event EventHandler? MouseIdle;
        public bool IsTracking => isTracking;

        public MouseTracker()
        {
            throttleTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            idleTimer = new Timer(OnIdleTimeout, null, Timeout.Infinite, Timeout.Infinite);
            
            if (QueryPerformanceFrequency(out performanceFrequency))
            {
                useHighPrecisionTiming = true;
            }
        }

        public void SetWindowHandle(IntPtr hwnd)
        {
            hwndSource = hwnd;
        }

        public void SetDeviceDPI(int dpi)
        {
            deviceDPI = dpi > 0 ? dpi : 1000; // Fallback to normalized DPI if invalid
        }

        public void SetBackEnd(BackEnd backEnd)
        {
            this.backEnd = backEnd;
        }

        public void SetDeviceInfoProvider(IDeviceInfoProvider deviceInfoProvider)
        {
            this.deviceInfoProvider = deviceInfoProvider;
        }

        public void StartTracking()
        {
            if (isTracking) return;

            if (hwndSource == IntPtr.Zero) return;

            try
            {
                var rid = new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01,  // Generic Desktop
                    usUsage = 0x02,      // Mouse
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = hwndSource
                };

                if (RegisterRawInputDevices(new[] { rid }, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                {
                    isTracking = true;
                    throttleTimer.Change(16, 16); // ~60 FPS
                    Debug.WriteLine("\n=== Mouse Tracking Started ===\nListening for mouse input from all devices...");
                    LogAvailableMouseDevices();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== Mouse Tracking Error ===\nFailed to start tracking: {ex.Message}");
            }
        }

        public void StopTracking()
        {
            if (!isTracking) return;

            try
            {
                var rid = new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01,
                    usUsage = 0x02,
                    dwFlags = RIDEV_REMOVE,
                    hwndTarget = IntPtr.Zero
                };

                RegisterRawInputDevices(new[] { rid }, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
                
                isTracking = false;
                throttleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                lastEventArgs = null;
                
                Debug.WriteLine("\n=== Mouse Tracking Stopped ===\nNo longer listening for mouse input.");
                
                // Reset device tracking
                lastActiveDeviceHandle = IntPtr.Zero;
                lastActiveDeviceName = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n=== Mouse Tracking Error ===\nFailed to stop tracking: {ex.Message}");
            }
        }

        public void ProcessRawInput(IntPtr lParam)
        {
            if (!isTracking) return;

            try
            {
                uint dwSize = 0;
                GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

                if (dwSize > 0)
                {
                    IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                    try
                    {
                        if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                        {
                            var rawInput = Marshal.PtrToStructure<RAWINPUT>(buffer);
                            if (rawInput.header.dwType == RIM_TYPEMOUSE)
                            {
                                ProcessMouseMovement(rawInput.mouse.lLastX, rawInput.mouse.lLastY, rawInput.header.hDevice);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            catch
            {
                // Silently fail during raw input processing
            }
        }

        private void ProcessMouseMovement(int deltaX, int deltaY, IntPtr deviceHandle)
        {
            if (deltaX == 0 && deltaY == 0) return;

            // Log device changes and update BackEnd
            LogDeviceChange(deviceHandle);
            UpdateBackEndDeviceInfo(deviceHandle);

            // Get time delta in milliseconds
            double timeMs = GetHighPrecisionTimeMs();
            if (timeMs <= 0) return;

            // Calculate combined speed (counts/second, normalized to 1000 DPI)
            double speed = CalculateSpeed(deltaX, deltaY, timeMs);
            if (speed <= 0 || double.IsNaN(speed) || double.IsInfinity(speed)) return;

            // Calculate individual axis speeds
            // deltaX/deltaY: mouse counts, timeMs: milliseconds  
            // Normalize to 1000 DPI equivalent: (counts/second) * (1000 / deviceDPI)
            double dpiNormalizationFactor = DriverNormalizedDPI / deviceDPI;
            double xSpeed = Math.Abs(deltaX) / timeMs * 1000.0 * dpiNormalizationFactor;
            double ySpeed = Math.Abs(deltaY) / timeMs * 1000.0 * dpiNormalizationFactor;

            lastX = deltaX;
            lastY = deltaY;

            var eventArgs = new MouseMovementEventArgs
            {
                MouseSpeed = speed,        // normalized counts/second (1000 DPI equivalent)
                X = deltaX,                // raw counts
                Y = deltaY,                // raw counts
                OutputSpeed = speed,       // normalized counts/second (1000 DPI equivalent)
                XSpeed = xSpeed,           // normalized counts/second (1000 DPI equivalent)
                YSpeed = ySpeed,           // normalized counts/second (1000 DPI equivalent)
                DeviceHandle = deviceHandle,
                DeviceName = GetDeviceName(deviceHandle)
            };

            lastEventArgs = eventArgs;
            
            idleTimer.Change(IdleTimeoutMs, Timeout.Infinite);
        }


        private double GetHighPrecisionTimeMs()
        {
            if (useHighPrecisionTiming)
            {
                if (QueryPerformanceCounter(out long currentCounter))
                {
                    if (lastPerformanceCounter == 0)
                    {
                        lastPerformanceCounter = currentCounter;
                        return 0;
                    }

                    // Calculate delta in performance counter ticks
                    long deltaCounter = currentCounter - lastPerformanceCounter;
                    lastPerformanceCounter = currentCounter;
                    
                    // Convert ticks to milliseconds
                    // deltaCounter: ticks, performanceFrequency: ticks/second
                    // Result: milliseconds (multiply by 1000 to convert seconds to ms)
                    return (double)deltaCounter * 1000.0 / performanceFrequency;
                }
            }
            
            // Fallback: GetTickCount returns milliseconds directly
            uint currentTick = GetTickCount();
            if (lastTickCount == 0)
            {
                lastTickCount = currentTick;
                return 0;
            }
            
            // Time delta in milliseconds
            double timeMs = currentTick - lastTickCount;
            lastTickCount = currentTick;
            return timeMs;
        }


        private double CalculateSpeed(double x, double y, double timeMs)
        {
            if (timeMs <= 0) return 0;
            
            // Calculate Euclidean distance in counts
            double distance = Math.Sqrt(x * x + y * y);
            
            // Convert to speed and normalize to 1000 DPI equivalent
            // distance: counts, timeMs: milliseconds
            // Result: normalized counts/second 
            double rawSpeed = distance / timeMs * 1000.0;
            double dpiNormalizationFactor = DriverNormalizedDPI / deviceDPI;
            return rawSpeed * dpiNormalizationFactor;
        }

        private void OnTimerTick(object? state)
        {
            var eventArgs = lastEventArgs;
            if (eventArgs != null)
            {
                MouseMoved?.Invoke(this, eventArgs);
                lastEventArgs = null;
            }
        }
        
        private void OnIdleTimeout(object? state)
        {
            MouseIdle?.Invoke(this, EventArgs.Empty);
        }

        private string GetDeviceName(IntPtr deviceHandle)
        {
            if (deviceHandle == IntPtr.Zero) return "Unknown Device";

            // Check cache first
            if (deviceNameCache.TryGetValue(deviceHandle, out string? cachedName))
            {
                return cachedName;
            }

            // Use centralized device service if available
            if (deviceInfoProvider != null)
            {
                string deviceName = deviceInfoProvider.GetDeviceNameFromHandle(deviceHandle);
                deviceNameCache[deviceHandle] = deviceName;
                return deviceName;
            }

            // Fallback to handle-based identification
            string fallbackName = $"Mouse Device ({deviceHandle.ToInt64():X})";
            Debug.WriteLine($"\n=== Device Discovery (Fallback) ===\nUsing fallback name: {fallbackName}\nHandle: {deviceHandle.ToInt64():X}");
            deviceNameCache[deviceHandle] = fallbackName;
            return fallbackName;
        }

        

        private string GetDeviceHID(IntPtr deviceHandle)
        {
            if (deviceHandle == IntPtr.Zero) return string.Empty;

            // Check cache first
            if (deviceHIDCache.TryGetValue(deviceHandle, out string? cachedHID))
            {
                return cachedHID;
            }

            // Use centralized device service if available
            if (deviceInfoProvider != null)
            {
                string hardwareId = deviceInfoProvider.GetDeviceHardwareIDFromHandle(deviceHandle);
                deviceHIDCache[deviceHandle] = hardwareId;
                return hardwareId;
            }

            // Fallback - use handle as string
            string fallbackHID = $"HANDLE_{deviceHandle.ToInt64():X}";
            deviceHIDCache[deviceHandle] = fallbackHID;
            return fallbackHID;
        }

        private void UpdateBackEndDeviceInfo(IntPtr deviceHandle)
        {
            if (backEnd == null) return;

            string deviceName = GetDeviceName(deviceHandle);
            string deviceHID = GetDeviceHID(deviceHandle);
            
            backEnd.UpdateCurrentInputDevice(deviceHandle, deviceHID, deviceName);
        }

        private void LogDeviceChange(IntPtr deviceHandle)
        {
            if (deviceHandle != lastActiveDeviceHandle)
            {
                string deviceName = GetDeviceName(deviceHandle);
                
                if (lastActiveDeviceHandle == IntPtr.Zero)
                {
                    Debug.WriteLine($"\n=== Mouse Device Detection ===\nInitial device detected: {deviceName} (Handle: {deviceHandle.ToInt64():X})");
                }
                else
                {
                    Debug.WriteLine($"\n=== Mouse Device Change ===\nFrom: {lastActiveDeviceName} (Handle: {lastActiveDeviceHandle.ToInt64():X})\nTo: {deviceName} (Handle: {deviceHandle.ToInt64():X})");
                }
                
                lastActiveDeviceHandle = deviceHandle;
                lastActiveDeviceName = deviceName;
            }
        }

        private void LogAvailableMouseDevices()
        {
            try
            {
                uint deviceCount = 0;
                
                // First call to get the number of devices
                if (GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))) == 0 && deviceCount > 0)
                {
                    // Allocate memory for device list
                    IntPtr deviceListPtr = Marshal.AllocHGlobal((int)(deviceCount * Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))));
                    
                    try
                    {
                        // Second call to get the actual device list
                        if (GetRawInputDeviceList(deviceListPtr, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))) != uint.MaxValue)
                        {
                            Debug.WriteLine($"\n=== Available Mouse Devices ({deviceCount} total devices) ===");
                            
                            for (int i = 0; i < deviceCount; i++)
                            {
                                IntPtr currentDevicePtr = IntPtr.Add(deviceListPtr, i * Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
                                RAWINPUTDEVICELIST device = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(currentDevicePtr);
                                
                                // Only log mouse devices (type 0 = mouse)
                                if (device.dwType == 0)
                                {
                                    string deviceName = GetDeviceName(device.hDevice);
                                    Debug.WriteLine($"Mouse {i + 1}: {deviceName} (Handle: {device.hDevice.ToInt64():X})");
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
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            StopTracking();
            throttleTimer?.Dispose();
            idleTimer?.Dispose();
        }

    }
}