using System;

namespace userspace_backend.Logging
{
    public enum LogSource
    {
        Backend,
        UI,
        Hardware,
        Performance,
        System
    }

    public static class LogSourceExtensions
    {
        public static string ToCategory(this LogSource source)
        {
            return source switch
            {
                LogSource.Backend => "RawAccel.Backend",
                LogSource.UI => "RawAccel.UI",
                LogSource.Hardware => "RawAccel.Hardware",
                LogSource.Performance => "RawAccel.Performance",
                LogSource.System => "RawAccel.System",
                _ => "RawAccel.Unknown"
            };
        }

        public static string ToDisplayName(this LogSource source)
        {
            return source switch
            {
                LogSource.Backend => "Backend Operations",
                LogSource.UI => "User Interface",
                LogSource.Hardware => "Hardware & Devices",
                LogSource.Performance => "Performance Metrics",
                LogSource.System => "System Operations",
                _ => "Unknown"
            };
        }
    }
}