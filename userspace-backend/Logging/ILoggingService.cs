using Microsoft.Extensions.Logging;
using System;

namespace userspace_backend.Logging
{
    public interface ILoggingService
    {
        LoggingConfiguration Configuration { get; }
        
        void LogTrace(LogSource source, string message, params object[] args);
        void LogDebug(LogSource source, string message, params object[] args);
        void LogInformation(LogSource source, string message, params object[] args);
        void LogWarning(LogSource source, string message, params object[] args);
        void LogError(LogSource source, string message, params object[] args);
        void LogError(LogSource source, Exception exception, string message, params object[] args);
        void LogCritical(LogSource source, string message, params object[] args);
        void LogCritical(LogSource source, Exception exception, string message, params object[] args);
        
        ILogger GetLogger(LogSource source);
        void UpdateConfiguration(LoggingConfiguration configuration);
        void SetLogLevel(LogSource source, LogLevel level);
        bool IsEnabled(LogSource source, LogLevel level);
    }
}