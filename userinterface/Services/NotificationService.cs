using System;
using userinterface.Models;
using userspace_backend.Logging;

namespace userinterface.Services
{
    public class NotificationService : INotificationService
    {
        private readonly LocalizationService localizationService;
        private readonly ISettingsService settingsService;
        private readonly ILoggingService loggingService;

        private const int DefaultToastDurationMs = 3000;

        public NotificationService(LocalizationService localizationService, ISettingsService settingsService, ILoggingService loggingService)
        {
            this.localizationService = localizationService;
            this.settingsService = settingsService;
            this.loggingService = loggingService;
        }

        public event EventHandler<ToastNotificationEventArgs>? ToastRequested;

        public event EventHandler? ToastDismissed;

        public void ShowToast(string messageKey, ToastType type, int durationMs = 5000)
        {
            ShowToast(messageKey, type, durationMs, new object[0]);
        }

        public void ShowToast(string messageKey, ToastType type, int durationMs = 5000, params object[] formatArgs)
        {
            if (!settingsService.ShowToastNotifications)
            {
                loggingService.LogDebug(LogSource.Toast, "Toast notification skipped (disabled): MessageKey={MessageKey}, Type={Type}", messageKey, type);
                return;
            }

            var localizedMessage = localizationService.GetText(messageKey);
            if (formatArgs.Length > 0)
            {
                localizedMessage = string.Format(localizedMessage, formatArgs);
            }

            loggingService.LogInformation(LogSource.Toast, "Showing toast: MessageKey={MessageKey}, Type={Type}, Duration={Duration}ms, Message={Message}", 
                messageKey, type, durationMs, localizedMessage);

            var toastArgs = new ToastNotificationEventArgs
            {
                Message = localizedMessage,
                Type = type,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            ToastRequested?.Invoke(this, toastArgs);
        }

        public void ShowImmediateToast(string messageKey, ToastType type, int durationMs = 5000)
        {
            ShowImmediateToast(messageKey, type, durationMs, new object[0]);
        }

        public void ShowImmediateToast(string messageKey, ToastType type, int durationMs = 5000, params object[] formatArgs)
        {
            if (!settingsService.ShowToastNotifications)
            {
                loggingService.LogDebug(LogSource.Toast, "Immediate toast notification skipped (disabled): MessageKey={MessageKey}, Type={Type}", messageKey, type);
                return;
            }

            ClearQueue();

            var localizedMessage = localizationService.GetText(messageKey);
            if (formatArgs.Length > 0)
            {
                localizedMessage = string.Format(localizedMessage, formatArgs);
            }

            loggingService.LogInformation(LogSource.Toast, "Showing immediate toast: MessageKey={MessageKey}, Type={Type}, Duration={Duration}ms, Message={Message}", 
                messageKey, type, durationMs, localizedMessage);

            var toastArgs = new ToastNotificationEventArgs
            {
                Message = localizedMessage,
                Type = type,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            ToastRequested?.Invoke(this, toastArgs);
        }

        public void HideToast()
        {
            ToastDismissed?.Invoke(this, EventArgs.Empty);
        }

        public void ShowSuccessToast(string messageKey, int durationMs = DefaultToastDurationMs)
        {
            ShowToast(messageKey, ToastType.Success, durationMs);
        }

        public void ShowSuccessToast(string messageKey, int durationMs = DefaultToastDurationMs, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Success, durationMs, formatArgs);
        }

        public void ShowImmediateSuccessToast(string messageKey, int durationMs = DefaultToastDurationMs)
        {
            ShowImmediateToast(messageKey, ToastType.Success, durationMs);
        }

        public void ShowImmediateSuccessToast(string messageKey, int durationMs = DefaultToastDurationMs, params object[] formatArgs)
        {
            ShowImmediateToast(messageKey, ToastType.Success, durationMs, formatArgs);
        }

        public void ShowErrorToast(string messageKey, int durationMs = DefaultToastDurationMs + 3000)
        {
            ShowToast(messageKey, ToastType.Error, durationMs);
        }

        public void ShowErrorToast(string messageKey, int durationMs = DefaultToastDurationMs + 3000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Error, durationMs, formatArgs);
        }

        public void ShowImmediateErrorToast(string messageKey, int durationMs = DefaultToastDurationMs + 3000)
        {
            ShowImmediateToast(messageKey, ToastType.Error, durationMs);
        }

        public void ShowImmediateErrorToast(string messageKey, int durationMs = DefaultToastDurationMs + 3000, params object[] formatArgs)
        {
            ShowImmediateToast(messageKey, ToastType.Error, durationMs, formatArgs);
        }

        public void ShowWarningToast(string messageKey, int durationMs = DefaultToastDurationMs)
        {
            ShowToast(messageKey, ToastType.Warning, durationMs);
        }

        public void ShowWarningToast(string messageKey, int durationMs = DefaultToastDurationMs, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Warning, durationMs, formatArgs);
        }

        public void ShowImmediateWarningToast(string messageKey, int durationMs = DefaultToastDurationMs)
        {
            ShowImmediateToast(messageKey, ToastType.Warning, durationMs);
        }

        public void ShowImmediateWarningToast(string messageKey, int durationMs = DefaultToastDurationMs, params object[] formatArgs)
        {
            ShowImmediateToast(messageKey, ToastType.Warning, durationMs, formatArgs);
        }

        public void ShowInfoToast(string messageKey, int durationMs = DefaultToastDurationMs)
        {
            ShowToast(messageKey, ToastType.Info, durationMs);
        }

        public void ShowInfoToast(string messageKey, int durationMs = DefaultToastDurationMs, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Info, durationMs, formatArgs);
        }

        public void ShowImmediateInfoToast(string messageKey, int durationMs = DefaultToastDurationMs)
        {
            ShowImmediateToast(messageKey, ToastType.Info, durationMs);
        }

        public void ShowImmediateInfoToast(string messageKey, int durationMs = DefaultToastDurationMs, params object[] formatArgs)
        {
            ShowImmediateToast(messageKey, ToastType.Info, durationMs, formatArgs);
        }


        public void ClearQueue()
        {
            ToastDismissed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
        }
    }
}