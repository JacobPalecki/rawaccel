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
        event EventHandler? MouseIdle;
        bool IsTracking { get; }
        void SetWindowHandle(IntPtr hwnd);
        void StartTracking();
        void StopTracking();
        void ProcessRawInput(IntPtr lParam);
    }

    public class MouseTrackingService : IMouseTrackingService
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

        // Signal processing for smoothing and outlier detection
        private readonly double[] speedHistory = new double[5]; // Rolling window for smoothing
        private int speedHistoryIndex = 0;
        private bool speedHistoryFull = false;
        private double lastValidSpeed = 0;
        private const double OutlierThreshold = 5.0; // Max 5x speed increase
        private const double SmoothingFactor = 0.3; // Exponential smoothing factor

        // Performance optimizations
        private readonly object eventArgsPool = new object();
        private MouseMovementEventArgs? pooledEventArgs;
        private double predictedSpeed = 0;
        private long sampleCount = 0;
        private double averageTimeDelta = 16.67; // Initial estimate for 60Hz

        public event EventHandler<MouseMovementEventArgs>? MouseMoved;
        public event EventHandler? MouseIdle;
        public bool IsTracking => isTracking;

        public MouseTrackingService()
        {
            throttleTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            idleTimer = new Timer(OnIdleTimeout, null, Timeout.Infinite, Timeout.Infinite);
            
            // Initialize high-precision timing
            if (QueryPerformanceFrequency(out performanceFrequency))
            {
                useHighPrecisionTiming = true;
                System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] High-precision timing enabled (freq: {performanceFrequency} Hz)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MOUSE TRACKING] Falling back to GetTickCount timing");
            }
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
                idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                lastEventArgs = null;
                
                // Reset performance metrics
                ResetPerformanceMetrics();
                
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

            double timeMs = GetHighPrecisionTimeMs();
            if (timeMs <= 0) return;

            double speed = CalculateSpeed(deltaX, deltaY, timeMs);
            if (speed <= 0 || double.IsNaN(speed) || double.IsInfinity(speed)) return;

            lastX = deltaX;
            lastY = deltaY;

            var eventArgs = new MouseMovementEventArgs
            {
                MouseSpeed = speed,
                X = deltaX,
                Y = deltaY,
                OutputSpeed = speed // Will be calculated later with curve interpolation
            };

            lastEventArgs = eventArgs;
            
            // Reset idle timer on movement
            idleTimer.Change(IdleTimeoutMs, Timeout.Infinite);
        }

        private double ProcessSpeedSignal(double rawSpeed)
        {
            // Outlier detection - reject impossible speed spikes
            if (lastValidSpeed > 0 && rawSpeed > lastValidSpeed * OutlierThreshold)
            {
                // Use exponentially smoothed value instead of the outlier
                return lastValidSpeed * (1 + SmoothingFactor);
            }

            // Add to rolling history
            speedHistory[speedHistoryIndex] = rawSpeed;
            speedHistoryIndex = (speedHistoryIndex + 1) % speedHistory.Length;
            if (!speedHistoryFull && speedHistoryIndex == 0)
                speedHistoryFull = true;

            // Calculate smoothed speed using moving average
            double smoothedSpeed = CalculateMovingAverage();
            
            // Apply exponential smoothing for responsiveness
            double finalSpeed = lastValidSpeed == 0 ? smoothedSpeed : 
                lastValidSpeed * (1 - SmoothingFactor) + smoothedSpeed * SmoothingFactor;

            lastValidSpeed = finalSpeed;
            return finalSpeed;
        }

        private double CalculateMovingAverage()
        {
            double sum = 0;
            int count = speedHistoryFull ? speedHistory.Length : speedHistoryIndex;
            
            if (count == 0) return 0;

            for (int i = 0; i < count; i++)
            {
                sum += speedHistory[i];
            }

            return sum / count;
        }

        private MouseMovementEventArgs GetPooledEventArgs()
        {
            lock (eventArgsPool)
            {
                if (pooledEventArgs == null)
                {
                    pooledEventArgs = new MouseMovementEventArgs();
                }
                return pooledEventArgs;
            }
        }

        private void UpdatePerformanceMetrics(double timeMs, double speed)
        {
            sampleCount++;
            
            // Update running average of time delta for polling rate estimation
            if (sampleCount > 1)
            {
                averageTimeDelta = (averageTimeDelta * 0.95) + (timeMs * 0.05);
            }
            
            // Simple prediction for next speed (linear extrapolation)
            if (lastValidSpeed > 0)
            {
                double speedDelta = speed - lastValidSpeed;
                predictedSpeed = speed + speedDelta * 0.5; // Predict half a delta ahead
            }
            else
            {
                predictedSpeed = speed;
            }
            
            // Log performance metrics periodically
            if (sampleCount % 1000 == 0)
            {
                double estimatedPollingRate = 1000.0 / averageTimeDelta;
                System.Diagnostics.Debug.WriteLine($"[MOUSE TRACKING] Avg delta: {averageTimeDelta:F2}ms, Est. polling: {estimatedPollingRate:F0}Hz");
            }
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

                    long deltaCounter = currentCounter - lastPerformanceCounter;
                    lastPerformanceCounter = currentCounter;
                    
                    // Convert to milliseconds with high precision
                    return (double)deltaCounter * 1000.0 / performanceFrequency;
                }
            }
            
            // Fallback to GetTickCount
            uint currentTick = GetTickCount();
            if (lastTickCount == 0)
            {
                lastTickCount = currentTick;
                return 0;
            }
            
            double timeMs = currentTick - lastTickCount;
            lastTickCount = currentTick;
            return timeMs;
        }

        private static double CalculateOptimizedSpeed(double x, double y, double timeMs)
        {
            if (timeMs <= 0) return 0;
            
            double magnitude = OptimizedMagnitude(x, y);
            return magnitude / timeMs * 1000.0; // Convert to per second
        }

        private static double OptimizedMagnitude(double x, double y)
        {
            // Fast path optimizations from original grapher
            if (x == 0)
            {
                return Math.Abs(y);
            }

            if (y == 0)
            {
                return Math.Abs(x);
            }

            // For non-axis-aligned movement, use standard distance formula
            return Math.Sqrt(x * x + y * y);
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
        
        private void OnIdleTimeout(object? state)
        {
            MouseIdle?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("[MOUSE TRACKING] Mouse idle detected");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            StopTracking();
            throttleTimer?.Dispose();
            idleTimer?.Dispose();
        }

        private void ResetPerformanceMetrics()
        {
            lastValidSpeed = 0;
            predictedSpeed = 0;
            sampleCount = 0;
            averageTimeDelta = 16.67;
            speedHistoryIndex = 0;
            speedHistoryFull = false;
            lastPerformanceCounter = 0;
            lastTickCount = 0;
            Array.Clear(speedHistory, 0, speedHistory.Length);
        }
    }
}