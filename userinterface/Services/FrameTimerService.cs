using Avalonia.Threading;
using System;
using System.Diagnostics;
using userspace_backend.Logging;

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
        private readonly ILoggingService loggingService;
        private const double THRESHOLD_MS = 20.0;
        private bool isMonitoring = false;

        public FrameTimerService(ILoggingService loggingService)
        {
            this.loggingService = loggingService;
            frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(THRESHOLD_MS)
            };
            frameTimer.Tick += OnFrameTick;
        }

        public void StartMonitoring(string context = "")
        {
            if (isMonitoring) return;

            isMonitoring = true;
            frameStopwatch.Restart();
            frameTimer.Start();
            loggingService.LogDebug(LogSource.Performance, "Started monitoring: {Context}", context);
        }

        public void StopMonitoring(string context = "")
        {
            if (!isMonitoring) return;

            frameTimer.Stop();
            isMonitoring = false;
            loggingService.LogDebug(LogSource.Performance, "Stopped monitoring: {Context}", context);
        }


        private void OnFrameTick(object? sender, EventArgs e)
        {
            if (!isMonitoring) return;

            var elapsed = frameStopwatch.ElapsedMilliseconds;
            if (elapsed >= THRESHOLD_MS)
            {
                loggingService.LogWarning(LogSource.Performance, "UI Thread blocked for {ElapsedMs}ms - potential frame drop!", elapsed);
            }

            frameStopwatch.Restart();
        }


        public void MonitorOperation(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            loggingService.LogDebug(LogSource.Performance, "Starting operation: {OperationName}", operationName);

            StartMonitoring($"Operation: {operationName}");

            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                StopMonitoring($"Operation: {operationName}");
                loggingService.LogDebug(LogSource.Performance, "Completed operation: {OperationName} in {ElapsedMs}ms", operationName, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}