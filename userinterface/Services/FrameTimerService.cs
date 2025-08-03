using Avalonia.Threading;
using System;
using System.Diagnostics;

namespace userinterface.Services
{
    // Service for monitoring UI thread blocking during performance-critical operations.
    // Usage:
    // - Call StartMonitoring("context") before performance-critical operations  
    // - Call StopMonitoring("context") after completion
    // - Use MonitorOperation("name", action) for automatic monitoring
    // 
    public class FrameTimerService
    {
        private readonly Stopwatch frameStopwatch = new();
        private readonly DispatcherTimer frameTimer;
        private const double THRESHOLD_MS = 8.33;
        private bool isMonitoring = false;

        public FrameTimerService()
        {
            frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromTicks(83333)
            };
            frameTimer.Tick += OnFrameTick;
        }

        public void StartMonitoring(string context = "")
        {
            if (isMonitoring) return;
            
            isMonitoring = true;
            frameStopwatch.Restart();
            frameTimer.Start();
        }

        public void StopMonitoring(string context = "")
        {
            if (!isMonitoring) return;
            
            frameTimer.Stop();
            isMonitoring = false;
        }


        private void OnFrameTick(object? sender, EventArgs e)
        {
            if (!isMonitoring) return;

            var elapsed = frameStopwatch.ElapsedMilliseconds;
            if (elapsed >= THRESHOLD_MS)
            {
            }
            
            frameStopwatch.Restart();
        }


        // Monitors operation execution time and detects UI thread blocking
        public void MonitorOperation(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            
            StartMonitoring($"Operation: {operationName}");
            
            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                StopMonitoring($"Operation: {operationName}");
            }
        }
    }
}