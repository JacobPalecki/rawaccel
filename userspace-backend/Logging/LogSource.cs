using System;

namespace userspace_backend.Logging
{
    public enum LogSource
    {
        Backend,
        UI,
        Hardware,
        Performance,
        System,
        Modal,
        LUT,
        Toast
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
                LogSource.Modal => "RawAccel.Modal",
                LogSource.LUT => "RawAccel.LUT",
                LogSource.Toast => "RawAccel.Toast",
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
                LogSource.Modal => "Modal Dialogs",
                LogSource.LUT => "LUT Operations",
                LogSource.Toast => "Toast Notifications",
                _ => "Unknown"
            };
        }
    }
}