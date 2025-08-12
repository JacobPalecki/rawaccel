using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using userinterface.Models;
using userinterface.Services;
using userinterface.Views.Controls;

namespace userinterface.ViewModels.Controls
{
    public class ToastContainerViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly INotificationService notificationService;
        private ToastContainerView? containerView;
        private readonly Queue<ToastViewModel> toastQueue = new();
        private bool isProcessingQueue = false;
        private const int MaxToasts = 3;

        public ToastContainerViewModel(INotificationService notificationService)
        {
            this.notificationService = notificationService;
            ToastItems = new ObservableCollection<ToastViewModel>();

            this.notificationService.ToastRequested += OnToastRequested;
        }

        public ObservableCollection<ToastViewModel> ToastItems { get; }

        public void SetContainerView(ToastContainerView view)
        {
            containerView = view;
        }

        private void OnToastRequested(object? sender, ToastNotificationEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var toastViewModel = new ToastViewModel(notificationService, Guid.NewGuid());
                toastViewModel.SetToastData(e.Message, e.Type, e.Duration);

                toastQueue.Enqueue(toastViewModel);
                ProcessQueue();
            });
        }


        private void ProcessQueue()
        {
            if (isProcessingQueue) return;

            isProcessingQueue = true;

            try
            {
                while (ToastItems.Count < MaxToasts && toastQueue.Count > 0)
                {
                    var toastViewModel = toastQueue.Dequeue();
                    toastViewModel.ToastExpired += OnIndividualToastExpired;
                    ToastItems.Insert(0, toastViewModel);
                }
            }
            finally
            {
                isProcessingQueue = false;
            }
        }

        private void OnIndividualToastExpired(object? sender, Guid toastId)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                var toastToRemove = ToastItems.FirstOrDefault(t => t.Id == toastId);
                if (toastToRemove != null && containerView != null)
                {
                    var toastView = FindToastView(toastId);
                    if (toastView != null)
                    {
                        await containerView.AnimateToastExit(toastView);
                    }

                    toastToRemove.ToastExpired -= OnIndividualToastExpired;
                    ToastItems.Remove(toastToRemove);
                    toastToRemove.Dispose();

                    ProcessQueue();
                }
            });
        }

        private ToastView? FindToastView(Guid toastId)
        {
            if (containerView == null) return null;

            var itemsControl = containerView.FindControl<ItemsControl>("ToastItemsControl");
            if (itemsControl?.Presenter?.Panel == null) return null;

            return itemsControl.Presenter.Panel.Children
                .OfType<ContentPresenter>()
                .Select(cp => cp.Child as ToastView)
                .FirstOrDefault(tv => tv?.DataContext is ToastViewModel vm && vm.Id == toastId);
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
            }

            foreach (var toast in ToastItems)
            {
                toast.ToastExpired -= OnIndividualToastExpired;
                toast.Dispose();
            }
            ToastItems.Clear();

            while (toastQueue.Count > 0)
            {
                var queuedToast = toastQueue.Dequeue();
                queuedToast.Dispose();
            }
        }
    }
}