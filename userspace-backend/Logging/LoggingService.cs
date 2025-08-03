using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace userspace_backend.Logging
{
    public class LoggingService : ILoggingService, IDisposable
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ConcurrentDictionary<LogSource, ILogger> loggers = new();
        private LoggingConfiguration configuration;

        public LoggingConfiguration Configuration 
        { 
            get => configuration; 
            private set => configuration = value ?? throw new ArgumentNullException(nameof(value));
        }

        public LoggingService(LoggingConfiguration? configuration = null)
        {
            this.configuration = configuration ?? new LoggingConfiguration();
            
            var builder = LoggerFactory.Create(builder =>
            {
                if (this.configuration.EnableConsoleLogging)
                {
                    builder.AddConsole();
                }

                if (this.configuration.EnableDebugLogging)
                {
                    builder.AddDebug();
                }

                if (this.configuration.EnableFileLogging)
                {
                    builder.AddProvider(new FileLoggerProvider(
                        this.configuration.LogDirectory,
                        this.configuration.LogFileNameFormat,
                        this.configuration.MaxLogFileSizeMB,
                        this.configuration.MaxLogFileCount));
                }

                builder.SetMinimumLevel(LogLevel.Trace);
                
                ConfigureFiltering(builder);
            });

            loggerFactory = builder;
        }

        private void ConfigureFiltering(ILoggingBuilder builder)
        {
            foreach (var kvp in configuration.SourceLogLevels)
            {
                var category = kvp.Key.ToCategory();
                var level = kvp.Value;
                builder.AddFilter(category, level);
            }
        }

        public ILogger GetLogger(LogSource source)
        {
            return loggers.GetOrAdd(source, s => loggerFactory.CreateLogger(s.ToCategory()));
        }

        public void LogTrace(LogSource source, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Trace))
            {
                GetLogger(source).LogTrace(message, args);
            }
        }

        public void LogDebug(LogSource source, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Debug))
            {
                GetLogger(source).LogDebug(message, args);
            }
        }

        public void LogInformation(LogSource source, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Information))
            {
                GetLogger(source).LogInformation(message, args);
            }
        }

        public void LogWarning(LogSource source, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Warning))
            {
                GetLogger(source).LogWarning(message, args);
            }
        }

        public void LogError(LogSource source, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Error))
            {
                GetLogger(source).LogError(message, args);
            }
        }

        public void LogError(LogSource source, Exception exception, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Error))
            {
                GetLogger(source).LogError(exception, message, args);
            }
        }

        public void LogCritical(LogSource source, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Critical))
            {
                GetLogger(source).LogCritical(message, args);
            }
        }

        public void LogCritical(LogSource source, Exception exception, string message, params object[] args)
        {
            if (IsEnabled(source, LogLevel.Critical))
            {
                GetLogger(source).LogCritical(exception, message, args);
            }
        }

        public void UpdateConfiguration(LoggingConfiguration newConfiguration)
        {
            if (newConfiguration == null)
                throw new ArgumentNullException(nameof(newConfiguration));

            Configuration = newConfiguration;
            
            loggers.Clear();
        }

        public void SetLogLevel(LogSource source, LogLevel level)
        {
            configuration.SetLogLevel(source, level);
            
            if (loggers.TryRemove(source, out _))
            {
            }
        }

        public bool IsEnabled(LogSource source, LogLevel level)
        {
            return configuration.IsSourceEnabled(source) && 
                   level >= configuration.GetLogLevel(source);
        }

        public void Dispose()
        {
            loggerFactory?.Dispose();
            loggers.Clear();
        }
    }
}