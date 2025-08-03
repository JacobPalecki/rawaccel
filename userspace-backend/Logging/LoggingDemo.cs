using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace userspace_backend.Logging
{
    public static class LoggingDemo
    {
        public static void RunDemo()
        {
            var config = new LoggingConfiguration
            {
                EnableLogging = true,
                EnableFileLogging = true,
                EnableConsoleLogging = true,
                EnableDebugLogging = true,
                LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
            };

            config.SetLogLevel(LogSource.Backend, LogLevel.Debug);
            config.SetLogLevel(LogSource.UI, LogLevel.Information);
            config.SetLogLevel(LogSource.Hardware, LogLevel.Information);
            config.SetLogLevel(LogSource.Performance, LogLevel.Warning);
            config.SetLogLevel(LogSource.System, LogLevel.Information);

            using var loggingService = new LoggingService(config);

            loggingService.LogInformation(LogSource.System, "Logging system demo started");
            
            loggingService.LogDebug(LogSource.Backend, "Loading profile: {ProfileName}", "TestProfile");
            loggingService.LogInformation(LogSource.Backend, "Profile loaded successfully");

            loggingService.LogInformation(LogSource.UI, "User navigated to page: {PageName}", "Settings");
            loggingService.LogDebug(LogSource.UI, "UI state updated");

            loggingService.LogInformation(LogSource.Hardware, "Device detected: {DeviceName}", "Test Mouse");
            loggingService.LogWarning(LogSource.Hardware, "Device configuration not found");

            try
            {
                throw new InvalidOperationException("Test exception for logging");
            }
            catch (Exception ex)
            {
                loggingService.LogError(LogSource.System, ex, "Caught test exception");
            }

            loggingService.LogWarning(LogSource.Performance, "Operation took longer than expected: {Duration}ms", 1500);

            loggingService.LogInformation(LogSource.System, "Logging system demo completed");

            Console.WriteLine($"Logs written to: {config.LogDirectory}");
            Console.WriteLine("Check the log files to verify the logging system is working correctly.");
        }

        public static void TestSourceToggling()
        {
            var config = new LoggingConfiguration();
            using var loggingService = new LoggingService(config);

            Console.WriteLine("Testing log source toggling...");

            loggingService.LogInformation(LogSource.Backend, "Backend logging enabled");
            
            config.SetLogLevel(LogSource.Backend, LogLevel.None);
            loggingService.UpdateConfiguration(config);
            
            loggingService.LogInformation(LogSource.Backend, "This should not appear - Backend logging disabled");
            
            config.SetLogLevel(LogSource.Backend, LogLevel.Information);
            loggingService.UpdateConfiguration(config);
            
            loggingService.LogInformation(LogSource.Backend, "Backend logging re-enabled");

            Console.WriteLine("Source toggling test completed.");
        }
    }
}