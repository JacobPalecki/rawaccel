using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace userspace_backend.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string logDirectory;
        private readonly string fileNameFormat;
        private readonly int maxFileSizeMB;
        private readonly int maxFileCount;
        private readonly ConcurrentDictionary<string, FileLogger> loggers = new();
        private readonly object lockObject = new();

        public FileLoggerProvider(string logDirectory, string fileNameFormat = "rawaccel-{0:yyyyMMdd}.log", int maxFileSizeMB = 10, int maxFileCount = 7)
        {
            this.logDirectory = logDirectory;
            this.fileNameFormat = fileNameFormat;
            this.maxFileSizeMB = maxFileSizeMB;
            this.maxFileCount = maxFileCount;

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));
        }

        public void Dispose()
        {
            foreach (var logger in loggers.Values)
            {
                logger.Dispose();
            }
            loggers.Clear();
        }

        internal void WriteLog(string categoryName, LogLevel logLevel, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            lock (lockObject)
            {
                try
                {
                    var fileName = string.Format(fileNameFormat, DateTime.Now);
                    var filePath = Path.Combine(logDirectory, fileName);
                    
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{categoryName}] {message}{Environment.NewLine}";
                    
                    File.AppendAllText(filePath, logEntry, Encoding.UTF8);
                    
                    ManageLogFiles(filePath);
                }
                catch (Exception)
                {
                }
            }
        }

        private void ManageLogFiles(string currentFilePath)
        {
            try
            {
                var fileInfo = new FileInfo(currentFilePath);
                if (fileInfo.Length > maxFileSizeMB * 1024 * 1024)
                {
                    RotateLogFile(currentFilePath);
                }

                CleanupOldLogFiles();
            }
            catch (Exception)
            {
            }
        }

        private void RotateLogFile(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                
                var rotatedFileName = $"{fileNameWithoutExtension}_{DateTime.Now:HHmmss}{extension}";
                var rotatedFilePath = Path.Combine(directory!, rotatedFileName);
                
                File.Move(filePath, rotatedFilePath);
            }
            catch (Exception)
            {
            }
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var directory = new DirectoryInfo(logDirectory);
                var logFiles = directory.GetFiles("*.log");
                
                if (logFiles.Length > maxFileCount)
                {
                    Array.Sort(logFiles, (x, y) => x.CreationTime.CompareTo(y.CreationTime));
                    
                    for (int i = 0; i < logFiles.Length - maxFileCount; i++)
                    {
                        logFiles[i].Delete();
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }

    internal class FileLogger : ILogger, IDisposable
    {
        private readonly string categoryName;
        private readonly FileLoggerProvider provider;

        public FileLogger(string categoryName, FileLoggerProvider provider)
        {
            this.categoryName = categoryName;
            this.provider = provider;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (exception != null)
            {
                message += Environment.NewLine + exception.ToString();
            }

            provider.WriteLog(categoryName, logLevel, message);
        }

        public void Dispose()
        {
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}