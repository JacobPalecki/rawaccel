using System;
using System.Collections.Generic;
using System.Threading;
using userinterface.Models;

namespace userinterface.Services
{
    public class NotificationService : INotificationService
    {
        private readonly LocalizationService localizationService;
        private readonly ISettingsService settingsService;

        private const int DefaultToastDurationMs = 3000;

        public NotificationService(LocalizationService localizationService, ISettingsService settingsService)
        {
            this.localizationService = localizationService;
            this.settingsService = settingsService;
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
                return;
            }

            var localizedMessage = localizationService.GetText(messageKey);
            if (formatArgs.Length > 0)
            {
                localizedMessage = string.Format(localizedMessage, formatArgs);
            }

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
                return;
            }

            ClearQueue();

            var localizedMessage = localizationService.GetText(messageKey);
            if (formatArgs.Length > 0)
            {
                localizedMessage = string.Format(localizedMessage, formatArgs);
            }

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