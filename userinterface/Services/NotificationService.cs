using System;
using System.Collections.Generic;
using System.Threading;
using userinterface.Models;

namespace userinterface.Services
{
    public class NotificationService : INotificationService
    {
        private Timer? timer;
        private readonly Queue<ToastNotificationEventArgs> toastQueue;
        private bool isDisplayingToast;
        private readonly LocalizationService localizationService;
        private readonly ISettingsService settingsService;

        public NotificationService(LocalizationService localizationService, ISettingsService settingsService)
        {
            this.localizationService = localizationService;
            this.settingsService = settingsService;
            this.toastQueue = new Queue<ToastNotificationEventArgs>();
            this.isDisplayingToast = false;
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

            timer?.Dispose();
            toastQueue.Clear();

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

            DisplayToast(toastArgs);
        }

        public void HideToast()
        {
            timer?.Dispose();
            isDisplayingToast = false;
            ToastDismissed?.Invoke(this, EventArgs.Empty);
            
            ProcessQueue();
        }

        public void ShowSuccessToast(string messageKey, int durationMs = 5000)
        {
            ShowToast(messageKey, ToastType.Success, durationMs);
        }

        public void ShowSuccessToast(string messageKey, int durationMs = 5000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Success, durationMs, formatArgs);
        }

        public void ShowErrorToast(string messageKey, int durationMs = 8000)
        {
            ShowToast(messageKey, ToastType.Error, durationMs);
        }

        public void ShowErrorToast(string messageKey, int durationMs = 8000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Error, durationMs, formatArgs);
        }

        public void ShowWarningToast(string messageKey, int durationMs = 6000)
        {
            ShowToast(messageKey, ToastType.Warning, durationMs);
        }

        public void ShowWarningToast(string messageKey, int durationMs = 6000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Warning, durationMs, formatArgs);
        }

        public void ShowInfoToast(string messageKey, int durationMs = 4000)
        {
            ShowToast(messageKey, ToastType.Info, durationMs);
        }

        public void ShowInfoToast(string messageKey, int durationMs = 4000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Info, durationMs, formatArgs);
        }

        public void QueueToast(string messageKey, ToastType type, int durationMs = 5000)
        {
            QueueToast(messageKey, type, durationMs, new object[0]);
        }

        public void QueueToast(string messageKey, ToastType type, int durationMs = 5000, params object[] formatArgs)
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

            if (isDisplayingToast)
            {
                toastQueue.Enqueue(toastArgs);
            }
            else
            {
                DisplayToast(toastArgs);
            }
        }

        public void ClearQueue()
        {
            toastQueue.Clear();
        }

        private void DisplayToast(ToastNotificationEventArgs toastArgs)
        {
            isDisplayingToast = true;
            
            ToastRequested?.Invoke(this, toastArgs);

            timer = new Timer(state => HideToast(), null, (int)toastArgs.Duration.TotalMilliseconds, Timeout.Infinite);
        }

        private void ProcessQueue()
        {
            if (toastQueue.Count > 0)
            {
                var nextToast = toastQueue.Dequeue();
                DisplayToast(nextToast);
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}