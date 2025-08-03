using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using userinterface.Models;
using userinterface.Services;

namespace userinterface.ViewModels.Controls
{
    public class ToastContainerViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly INotificationService notificationService;
        private const int MaxToasts = 3;

        public ToastContainerViewModel(INotificationService notificationService)
        {
            this.notificationService = notificationService;
            ToastItems = new ObservableCollection<ToastViewModel>();
            
            this.notificationService.ToastRequested += OnToastRequested;
            this.notificationService.ToastDismissed += OnToastDismissed;
        }

        public ObservableCollection<ToastViewModel> ToastItems { get; }

        private void OnToastRequested(object? sender, ToastNotificationEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var toastViewModel = new ToastViewModel(notificationService, Guid.NewGuid());
                toastViewModel.ToastExpired += OnIndividualToastExpired;
                
                toastViewModel.SetToastData(e.Message, e.Type, e.Duration);
                
                // Insert new toast at the beginning (bottom of visual stack)
                ToastItems.Insert(0, toastViewModel);
                
                if (ToastItems.Count > MaxToasts)
                {
                    // Remove the oldest toast (now at the end)
                    var oldestToast = ToastItems.Last();
                    oldestToast.ForceClose();
                    ToastItems.RemoveAt(ToastItems.Count - 1);
                }
            });
        }

        private void OnToastDismissed(object? sender, EventArgs e)
        {
        }

        private void OnIndividualToastExpired(object? sender, Guid toastId)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var toastToRemove = ToastItems.FirstOrDefault(t => t.Id == toastId);
                if (toastToRemove != null)
                {
                    toastToRemove.ToastExpired -= OnIndividualToastExpired;
                    ToastItems.Remove(toastToRemove);
                    toastToRemove.Dispose();
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (notificationService != null)
            {
                notificationService.ToastRequested -= OnToastRequested;
                notificationService.ToastDismissed -= OnToastDismissed;
            }

            foreach (var toast in ToastItems)
            {
                toast.ToastExpired -= OnIndividualToastExpired;
                toast.Dispose();
            }
            
            ToastItems.Clear();
        }
    }
}