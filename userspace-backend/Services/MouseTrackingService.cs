using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace userspace_backend.Services
{
    public class MouseMovementEventArgs : EventArgs
    {
        public double MouseSpeed { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double OutputSpeed { get; set; }
    }

    public interface IMouseTrackingService : IDisposable
    {
        event EventHandler<MouseMovementEventArgs>? MouseMoved;
        bool IsTracking { get; }
        void SetWindowHandle(IntPtr hwnd);
        void StartTracking();
        void StopTracking();
        void ProcessRawInput(IntPtr lParam);
    }

    public class MouseTrackingService : IMouseTrackingService
    {
        private readonly Timer throttleTimer;
        private volatile MouseMovementEventArgs? lastEventArgs;
        private volatile bool isTracking = false;
        private IntPtr hwndSource = IntPtr.Zero;
        private bool disposed = false;

        // Raw Input structures and constants
        private const int WM_INPUT = 0x00FF;
        private const int RID_INPUT = 0x10000003;
        private const int RIM_TYPEMOUSE = 0;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const uint RIDEV_REMOVE = 0x00000001;

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

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        private uint lastTickCount = 0;
        private double lastX = 0;
        private double lastY = 0;

        public event EventHandler<MouseMovementEventArgs>? MouseMoved;
        public bool IsTracking => isTracking;

        public MouseTrackingService()
        {
            throttleTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void SetWindowHandle(IntPtr hwnd)
        {
            hwndSource = hwnd;
            System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] Window handle set: {hwnd}");
        }

        public void StartTracking()
        {
            if (isTracking) return;

            if (hwndSource == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[MOUSE TRACKING] Cannot start tracking: No window handle set");
                return;
            }

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
                    System.Diagnostics.Debug.WriteLine("[MOUSE TRACKING] Started tracking");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] Failed to register raw input devices. Error: {error}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] Error starting tracking: {ex.Message}");
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
                lastEventArgs = null;
                
                System.Diagnostics.Debug.WriteLine("[MOUSE TRACKING] Stopped tracking");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] Error stopping tracking: {ex.Message}");
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
                                ProcessMouseMovement(rawInput.mouse.lLastX, rawInput.mouse.lLastY);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] Error processing raw input: {ex.Message}");
            }
        }

        private void ProcessMouseMovement(int deltaX, int deltaY)
        {
            if (deltaX == 0 && deltaY == 0) return;

            uint currentTick = GetTickCount();
            double timeMs = currentTick - lastTickCount;
            
            if (timeMs <= 0) return;

            double speed = CalculateSpeed(deltaX, deltaY, timeMs);
            if (speed <= 0 || double.IsNaN(speed) || double.IsInfinity(speed)) return;

            lastX = deltaX;
            lastY = deltaY;
            lastTickCount = currentTick;

            var eventArgs = new MouseMovementEventArgs
            {
                MouseSpeed = speed,
                X = deltaX,
                Y = deltaY,
                OutputSpeed = speed // Will be calculated later with curve interpolation
            };

            lastEventArgs = eventArgs;
        }

        private static double CalculateSpeed(double x, double y, double timeMs)
        {
            if (timeMs <= 0) return 0;
            
            double distance = Math.Sqrt(x * x + y * y);
            return distance / timeMs * 1000.0; // Convert to per second
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

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            StopTracking();
            throttleTimer?.Dispose();
        }
    }
}