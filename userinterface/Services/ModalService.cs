using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using userinterface.Views;
using userinterface.Views.Controls;

namespace userinterface.Services
{
    public enum ModalType
    {
        Confirmation,
        Message,
        Dialog,
        AlphaBuildWarning,
        DeviceConfiguration
    }

    public class ModalQueueItem
    {
        public ModalType Type { get; set; }
        public UserControl? DialogContent { get; set; }
        public string TitleKey { get; set; } = string.Empty;
        public string MessageKey { get; set; } = string.Empty;
        public string ConfirmTextKey { get; set; } = "ModalOK";
        public string CancelTextKey { get; set; } = "ModalCancel";
        public string OkTextKey { get; set; } = "ModalOK";
        public string? DeviceName { get; set; }
        public TaskCompletionSource<object?> TaskCompletionSource { get; set; } = new();
    }

    public class ModalService : IModalService
    {
        private Control? currentModalContent;
        private TaskCompletionSource<bool>? currentConfirmationTask;
        private TaskCompletionSource<object?>? currentDialogTask;
        private readonly LocalizationService localizationService;
        private readonly ISettingsService settingsService;
        private readonly Queue<ModalQueueItem> modalQueue = new();
        private bool isProcessingQueue = false;

        public ModalService(LocalizationService localizationService, ISettingsService settingsService)
        {
            this.localizationService = localizationService;
            this.settingsService = settingsService;
            
            // Initialize queue with Alpha build warning
            EnqueueModal(new ModalQueueItem
            {
                Type = ModalType.AlphaBuildWarning
            });
            
            // Subscribe to backend modal events
            userspace_backend.NotificationManager.QueuedModalRequested += OnBackEndQueuedModalRequested;
        }
        
        private void OnBackEndQueuedModalRequested(object? sender, userspace_backend.ModalEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"\n=== MODAL SERVICE QUEUE HANDLER ===\nReceived modal request: {e.ModalType}\nParameters: {string.Join(", ", e.Parameters)}");
            
            if (e.ModalType == "AlphaBuildWarning")
            {
                EnqueueModal(new ModalQueueItem
                {
                    Type = ModalType.AlphaBuildWarning
                });
            }
            else if (e.ModalType == "UnconfiguredDevice" && e.Parameters.Length > 0)
            {
                EnqueueModal(new ModalQueueItem
                {
                    Type = ModalType.DeviceConfiguration,
                    DeviceName = e.Parameters[0].ToString()
                });
            }
        }
        
        private void EnqueueModal(ModalQueueItem item)
        {
            System.Diagnostics.Debug.WriteLine($"ModalService: Enqueueing modal of type {item.Type}");
            modalQueue.Enqueue(item);
            System.Diagnostics.Debug.WriteLine($"ModalService: Queue length: {modalQueue.Count}");
            
            if (!isProcessingQueue)
            {
                _ = ProcessModalQueueAsync();
            }
        }
        
        private async Task ProcessModalQueueAsync()
        {
            if (isProcessingQueue)
            {
                System.Diagnostics.Debug.WriteLine("ModalService: Queue already being processed");
                return;
            }
            
            isProcessingQueue = true;
            System.Diagnostics.Debug.WriteLine("\n=== MODAL SERVICE STARTING QUEUE PROCESSING ===");
            
            // Wait a bit to ensure UI is fully loaded
            await Task.Delay(500);
            
            while (modalQueue.Count > 0)
            {
                var item = modalQueue.Dequeue();
                System.Diagnostics.Debug.WriteLine($"\n=== PROCESSING QUEUED MODAL ===\nType: {item.Type}\nRemaining in queue: {modalQueue.Count}");
                
                try
                {
                    switch (item.Type)
                    {
                        case ModalType.Confirmation:
                            var confirmResult = await ShowConfirmationImmediatelyAsync(item.TitleKey, item.MessageKey, item.ConfirmTextKey, item.CancelTextKey);
                            item.TaskCompletionSource.SetResult(confirmResult);
                            break;
                            
                        case ModalType.Message:
                            await ShowMessageImmediatelyAsync(item.TitleKey, item.MessageKey, item.OkTextKey);
                            item.TaskCompletionSource.SetResult(true);
                            break;
                            
                        case ModalType.Dialog:
                            var dialogResult = await ShowDialogImmediatelyAsync<object?>(item.DialogContent!, item.TitleKey);
                            item.TaskCompletionSource.SetResult(dialogResult);
                            break;
                            
                        case ModalType.AlphaBuildWarning:
                            await ShowAlphaBuildWarningAsync();
                            item.TaskCompletionSource.SetResult(true);
                            break;
                            
                        case ModalType.DeviceConfiguration:
                            var deviceResult = await ShowDeviceConfigurationAsync(item.DeviceName!);
                            item.TaskCompletionSource.SetResult(deviceResult);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ModalService: Error processing modal: {ex.Message}");
                    item.TaskCompletionSource.SetException(ex);
                }
                
                System.Diagnostics.Debug.WriteLine($"Modal of type {item.Type} completed, continuing to next item in queue");
            }
            
            isProcessingQueue = false;
            System.Diagnostics.Debug.WriteLine("=== MODAL SERVICE QUEUE PROCESSING COMPLETE ===");
        }

        private bool TryGetModalOverlay(out ModalOverlay modalOverlay)
        {
            modalOverlay = null!;

            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow as MainWindow;
                var overlay = mainWindow?.FindControl<ModalOverlay>("ModalOverlay");
                if (overlay != null)
                {
                    modalOverlay = overlay;
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> ShowConfirmationAsync(string titleKey, string messageKey, string confirmTextKey = "ModalOK", string cancelTextKey = "ModalCancel")
        {
            if (!settingsService.ShowConfirmModals)
            {
                return true;
            }
            
            var item = new ModalQueueItem
            {
                Type = ModalType.Confirmation,
                TitleKey = titleKey,
                MessageKey = messageKey,
                ConfirmTextKey = confirmTextKey,
                CancelTextKey = cancelTextKey
            };
            
            EnqueueModal(item);
            var result = await item.TaskCompletionSource.Task;
            return result is bool boolResult ? boolResult : false;
        }
        
        private async Task<bool> ShowConfirmationImmediatelyAsync(string titleKey, string messageKey, string confirmTextKey, string cancelTextKey)
        {
            if (!TryGetModalOverlay(out var modalOverlay)) return false;

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            currentConfirmationTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var confirmationDialog = new ConfirmationModalView
                {
                    Title = localizationService.GetText(titleKey),
                    Message = localizationService.GetText(messageKey),
                    ConfirmText = localizationService.GetText(confirmTextKey),
                    CancelText = localizationService.GetText(cancelTextKey)
                };

                confirmationDialog.ConfirmClicked += () =>
                {
                    currentConfirmationTask?.SetResult(true);
                    CloseCurrentModal();
                };

                confirmationDialog.CancelClicked += () =>
                {
                    currentConfirmationTask?.SetResult(false);
                    CloseCurrentModal();
                };

                modalOverlay.BackgroundClicked += () =>
                {
                    if (!currentConfirmationTask!.Task.IsCompleted)
                    {
                        currentConfirmationTask.SetResult(false);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = confirmationDialog;
                modalOverlay.ShowModal(confirmationDialog);
            });

            return await currentConfirmationTask.Task;
        }

        public async Task ShowMessageAsync(string titleKey, string messageKey, string okTextKey = "ModalOK")
        {
            var item = new ModalQueueItem
            {
                Type = ModalType.Message,
                TitleKey = titleKey,
                MessageKey = messageKey,
                OkTextKey = okTextKey
            };
            
            EnqueueModal(item);
            await item.TaskCompletionSource.Task;
        }
        
        private async Task ShowMessageImmediatelyAsync(string titleKey, string messageKey, string okTextKey)
        {
            if (!TryGetModalOverlay(out var modalOverlay)) return;

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            var messageTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var messageDialog = new MessageModalView
                {
                    Title = localizationService.GetText(titleKey),
                    Message = localizationService.GetText(messageKey),
                    OkText = localizationService.GetText(okTextKey)
                };

                messageDialog.OkClicked += () =>
                {
                    messageTask.SetResult(true);
                    CloseCurrentModal();
                };

                modalOverlay.BackgroundClicked += () =>
                {
                    if (!messageTask.Task.IsCompleted)
                    {
                        messageTask.SetResult(true);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = messageDialog;
                modalOverlay.ShowModal(messageDialog);
            });

            await messageTask.Task;
        }

        public async Task<T?> ShowDialogAsync<T>(UserControl dialogContent, string titleKey = "")
        {
            System.Diagnostics.Debug.WriteLine($"ModalService: ShowDialogAsync called for {dialogContent.GetType().Name}");
            
            var item = new ModalQueueItem
            {
                Type = ModalType.Dialog,
                DialogContent = dialogContent,
                TitleKey = titleKey
            };
            
            EnqueueModal(item);
            var result = await item.TaskCompletionSource.Task;
            return result is T typedResult ? typedResult : default(T);
        }
        
        private async Task<T?> ShowDialogImmediatelyAsync<T>(UserControl dialogContent, string titleKey)
        {
            System.Diagnostics.Debug.WriteLine($"ModalService: ShowDialogImmediatelyAsync called for {dialogContent.GetType().Name}");
            if (!TryGetModalOverlay(out var modalOverlay)) return default(T);

            if (currentModalContent != null)
            {
                System.Diagnostics.Debug.WriteLine("ModalService: Closing existing modal");
                CloseCurrentModal();
            }

            currentDialogTask = new TaskCompletionSource<object?>();
            System.Diagnostics.Debug.WriteLine("ModalService: Created new dialog task");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                modalOverlay.BackgroundClicked += () =>
                {
                    System.Diagnostics.Debug.WriteLine("ModalService: Background clicked");
                    if (!currentDialogTask!.Task.IsCompleted)
                    {
                        System.Diagnostics.Debug.WriteLine("ModalService: Setting result to null due to background click");
                        currentDialogTask.SetResult(null);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = dialogContent;
                modalOverlay.ShowModal(dialogContent);
                System.Diagnostics.Debug.WriteLine("ModalService: Modal shown in overlay");
            });

            System.Diagnostics.Debug.WriteLine("ModalService: Waiting for dialog task completion...");
            var result = await currentDialogTask.Task;
            System.Diagnostics.Debug.WriteLine($"ModalService: Dialog task completed with result: {result}");
            return result is T typedResult ? typedResult : default(T);
        }

        public void CloseCurrentModal()
        {
            System.Diagnostics.Debug.WriteLine("ModalService: CloseCurrentModal called");
            if (TryGetModalOverlay(out var modalOverlay) && currentModalContent != null)
            {
                System.Diagnostics.Debug.WriteLine($"ModalService: Hiding modal: {currentModalContent.GetType().Name}");
                modalOverlay.HideModal();
                currentModalContent = null;
                
                // Complete the dialog task if it hasn't been completed yet
                if (currentDialogTask != null && !currentDialogTask.Task.IsCompleted)
                {
                    System.Diagnostics.Debug.WriteLine("ModalService: Completing dialog task as modal was closed");
                    currentDialogTask.SetResult(null);
                }
            }
        }
        
        public void CloseCurrentModalWithResult<T>(T result)
        {
            System.Diagnostics.Debug.WriteLine($"ModalService: CloseCurrentModalWithResult called with result: {result}");
            if (TryGetModalOverlay(out var modalOverlay) && currentModalContent != null)
            {
                System.Diagnostics.Debug.WriteLine($"ModalService: Hiding modal: {currentModalContent.GetType().Name}");
                
                // Set the result first, then close
                if (currentDialogTask != null && !currentDialogTask.Task.IsCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"ModalService: Setting dialog task result to: {result}");
                    currentDialogTask.SetResult(result);
                }
                
                modalOverlay.HideModal();
                currentModalContent = null;
            }
        }

        private async Task ShowAlphaBuildWarningAsync()
        {
            System.Diagnostics.Debug.WriteLine("\n=== SHOWING ALPHA BUILD WARNING MODAL ===");
            
            var warningView = new Views.Controls.AlphaBuildWarningView();
            System.Diagnostics.Debug.WriteLine("Created AlphaBuildWarningView, showing modal...");
            
            await ShowDialogImmediatelyAsync<bool>(warningView, "");
            
            System.Diagnostics.Debug.WriteLine("Alpha build warning modal closed");
        }
        
        private async Task<bool?> ShowDeviceConfigurationAsync(string deviceName)
        {
            System.Diagnostics.Debug.WriteLine($"\n=== SHOWING UNCONFIGURED DEVICE MODAL ===\nDevice: {deviceName}");
            
            var confirmationView = new Views.Controls.DeviceConfigurationPromptView(deviceName);
            System.Diagnostics.Debug.WriteLine("Created DeviceConfigurationPromptView, showing modal...");
            
            var result = await ShowDialogImmediatelyAsync<bool?>(confirmationView, "UnconfiguredDeviceTitle");
            
            System.Diagnostics.Debug.WriteLine($"Modal closed. Dialog result: {result}");
            
            // Get the BackEnd service to handle the device configuration
            if (result == true)
            {
                var backEnd = App.Services?.GetService<userspace_backend.BackEnd>();
                if (backEnd?.UnconfiguredActiveDevice != null)
                {
                    System.Diagnostics.Debug.WriteLine("User chose to create device, adding to configured devices");
                    backEnd.Devices.TryAddDevice(backEnd.UnconfiguredActiveDevice.MapToData());
                    backEnd.UnconfiguredActiveDevice = null;
                    backEnd.ApplySettingsOnly();
                    System.Diagnostics.Debug.WriteLine("Device configuration saved");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User cancelled or no unconfigured device available");
            }
            
            return result;
        }

        public void Dispose()
        {
            userspace_backend.NotificationManager.QueuedModalRequested -= OnBackEndQueuedModalRequested;
            GC.SuppressFinalize(this);
        }
    }
}