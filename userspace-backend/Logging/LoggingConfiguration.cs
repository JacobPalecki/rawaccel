using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace userspace_backend.Logging
{
    public class LoggingConfiguration
    {
        public bool EnableLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableConsoleLogging { get; set; } = false;
        public bool EnableDebugLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public string LogFileNameFormat { get; set; } = "rawaccel-{0:yyyyMMdd}.log";
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFileCount { get; set; } = 7;
        public Dictionary<LogSource, LogLevel> SourceLogLevels { get; set; } = new()
        {
            { LogSource.Backend, LogLevel.Information },
            { LogSource.UI, LogLevel.Information },
            { LogSource.Hardware, LogLevel.Information },
            { LogSource.Performance, LogLevel.Warning },
            { LogSource.System, LogLevel.Information },
            { LogSource.Modal, LogLevel.None }
        };

        public LoggingConfiguration()
        {
        }

        public LogLevel GetLogLevel(LogSource source)
        {
            return SourceLogLevels.TryGetValue(source, out var level) ? level : LogLevel.Information;
        }

        public void SetLogLevel(LogSource source, LogLevel level)
        {
            SourceLogLevels[source] = level;
        }

        public bool IsSourceEnabled(LogSource source)
        {
            return EnableLogging && GetLogLevel(source) != LogLevel.None;
        }
    }
}