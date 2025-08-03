using System;
using userinterface.Models;

namespace userinterface.Services
{
    public interface INotificationService : IDisposable
    {
        void ShowToast(string messageKey, ToastType type, int durationMs = 5000);
        void ShowToast(string messageKey, ToastType type, int durationMs = 5000, params object[] formatArgs);

        void ShowImmediateToast(string messageKey, ToastType type, int durationMs = 5000);
        void ShowImmediateToast(string messageKey, ToastType type, int durationMs = 5000, params object[] formatArgs);

        void HideToast();

        void ShowSuccessToast(string messageKey, int durationMs = 5000);
        void ShowSuccessToast(string messageKey, int durationMs = 5000, params object[] formatArgs);

        void ShowImmediateSuccessToast(string messageKey, int durationMs = 5000);
        void ShowImmediateSuccessToast(string messageKey, int durationMs = 5000, params object[] formatArgs);

        void ShowErrorToast(string messageKey, int durationMs = 8000);
        void ShowErrorToast(string messageKey, int durationMs = 8000, params object[] formatArgs);

        void ShowImmediateErrorToast(string messageKey, int durationMs = 8000);
        void ShowImmediateErrorToast(string messageKey, int durationMs = 8000, params object[] formatArgs);

        void ShowWarningToast(string messageKey, int durationMs = 6000);
        void ShowWarningToast(string messageKey, int durationMs = 6000, params object[] formatArgs);

        void ShowImmediateWarningToast(string messageKey, int durationMs = 6000);
        void ShowImmediateWarningToast(string messageKey, int durationMs = 6000, params object[] formatArgs);

        void ShowInfoToast(string messageKey, int durationMs = 4000);
        void ShowInfoToast(string messageKey, int durationMs = 4000, params object[] formatArgs);

        void ShowImmediateInfoToast(string messageKey, int durationMs = 4000);
        void ShowImmediateInfoToast(string messageKey, int durationMs = 4000, params object[] formatArgs);

        void ClearQueue();

        event EventHandler<ToastNotificationEventArgs> ToastRequested;

        event EventHandler ToastDismissed;
    }
}