using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using userinterface.Models;
using userinterface.Views;
using userinterface.Views.Controls;
using userspace_backend.Logging;

namespace userinterface.Services
{
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
        public bool IsStartupModal { get; set; } = false;
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
        private readonly ILoggingService? logger;

        public ModalService(LocalizationService localizationService, ISettingsService settingsService)
        {
            this.localizationService = localizationService;
            this.settingsService = settingsService;
            this.logger = App.Services?.GetService<ILoggingService>();

            // Initialize queue with Alpha build warning
            EnqueueModal(new ModalQueueItem
            {
                Type = ModalType.AlphaBuildWarning,
                IsStartupModal = true
            });

            userspace_backend.NotificationManager.QueuedModalRequested += OnBackEndQueuedModalRequested;

            if (App.IsAppLoaded)
            {
                _ = ProcessModalQueueAsync();
            }
            else
            {
                App.AppLoadCompleted += OnAppLoadCompleted;
            }
        }

        private void OnAppLoadCompleted()
        {
            _ = ProcessModalQueueAsync();
        }

        private void OnBackEndQueuedModalRequested(object? sender, userspace_backend.ModalEventArgs e)
        {
            if (e.ModalType == "AlphaBuildWarning")
            {
                EnqueueModal(new ModalQueueItem
                {
                    Type = ModalType.AlphaBuildWarning,
                    IsStartupModal = true
                });
            }
            else if (e.ModalType == "UnconfiguredDevice" && e.Parameters.Length > 0)
            {
                EnqueueModal(new ModalQueueItem
                {
                    Type = ModalType.DeviceConfiguration,
                    DeviceName = e.Parameters[0].ToString(),
                    IsStartupModal = true
                });
            }
        }

        private void EnqueueModal(ModalQueueItem item)
        {
            logger?.LogInformation(LogSource.Modal, $"EnqueueModal: Type={item.Type}, IsStartupModal={item.IsStartupModal}, Queue count={modalQueue.Count}");
            modalQueue.Enqueue(item);

            logger?.LogInformation(LogSource.Modal, $"EnqueueModal: App.IsAppLoaded={App.IsAppLoaded}, isProcessingQueue={isProcessingQueue}");

            // For user-triggered modals, process immediately if app is loaded
            // For startup modals, wait for app load completion
            if (!isProcessingQueue && (App.IsAppLoaded || !item.IsStartupModal))
            {
                logger?.LogInformation(LogSource.Modal, "Starting ProcessModalQueueAsync");
                _ = ProcessModalQueueAsync();
            }
            else
            {
                logger?.LogInformation(LogSource.Modal, "Not processing queue - waiting for app load or already processing");
            }
        }

        private async Task ProcessModalQueueAsync()
        {
            logger?.LogInformation(LogSource.Modal, $"ProcessModalQueueAsync called: isProcessingQueue={isProcessingQueue}, queue count={modalQueue.Count}");

            if (isProcessingQueue)
            {
                logger?.LogInformation(LogSource.Modal, "Already processing queue, returning");
                return;
            }

            isProcessingQueue = true;
            logger?.LogInformation(LogSource.Modal, "Started processing modal queue");

            while (modalQueue.Count > 0)
            {
                var item = modalQueue.Dequeue();
                logger?.LogInformation(LogSource.Modal, $"Processing modal: Type={item.Type}, TitleKey='{item.TitleKey}'");

                try
                {
                    switch (item.Type)
                    {
                        case ModalType.Confirmation:
                            logger?.LogInformation(LogSource.Modal, "Calling ShowConfirmationImmediatelyAsync");
                            var confirmResult = await ShowConfirmationImmediatelyAsync(item.TitleKey, item.MessageKey, item.ConfirmTextKey, item.CancelTextKey);
                            logger?.LogInformation(LogSource.Modal, $"ShowConfirmationImmediatelyAsync returned: {confirmResult}");
                            item.TaskCompletionSource.SetResult(confirmResult);
                            break;

                        case ModalType.Dialog:
                            logger?.LogInformation(LogSource.Modal, "Calling ShowDialogImmediatelyAsync");
                            var dialogResult = await ShowDialogImmediatelyAsync<object?>(item.DialogContent!, item.TitleKey);
                            item.TaskCompletionSource.SetResult(dialogResult);
                            break;

                        case ModalType.DeviceConfiguration:
                            logger?.LogInformation(LogSource.Modal, "Calling ShowDeviceConfigurationAsync");
                            var deviceResult = await ShowDeviceConfigurationAsync(item.DeviceName!);
                            item.TaskCompletionSource.SetResult(deviceResult);
                            break;

                        default:
                            // Handle simple modal types that don't return specific values (always return true)
                            logger?.LogInformation(LogSource.Modal, $"Calling Show{item.Type}Async");
                            switch (item.Type)
                            {
                                case ModalType.Message:
                                    await ShowMessageImmediatelyAsync(item.TitleKey, item.MessageKey, item.OkTextKey);
                                    break;
                                case ModalType.AlphaBuildWarning:
                                    await ShowAlphaBuildWarningAsync();
                                    break;
                                default:
                                    throw new NotSupportedException($"Modal type {item.Type} is not supported");
                            }
                            item.TaskCompletionSource.SetResult(true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(LogSource.Modal, ex, $"Error processing modal: Type={item.Type}");
                    item.TaskCompletionSource.SetException(ex);
                }

            }

            logger?.LogInformation(LogSource.Modal, "Finished processing modal queue");
            isProcessingQueue = false;
        }

        private bool TryGetModalOverlay(out ModalOverlay modalOverlay)
        {
            modalOverlay = null!;
            logger?.LogInformation(LogSource.Modal, "TryGetModalOverlay: Starting");

            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                logger?.LogInformation(LogSource.Modal, "TryGetModalOverlay: Found desktop lifetime");
                var mainWindow = desktop.MainWindow as MainWindow;
                logger?.LogInformation(LogSource.Modal, $"TryGetModalOverlay: MainWindow = {(mainWindow != null ? "found" : "null")}");

                var overlay = mainWindow?.FindControl<ModalOverlay>("ModalOverlay");
                logger?.LogInformation(LogSource.Modal, $"TryGetModalOverlay: ModalOverlay control = {(overlay != null ? "found" : "null")}");

                if (overlay != null)
                {
                    modalOverlay = overlay;
                    logger?.LogInformation(LogSource.Modal, "TryGetModalOverlay: Success");
                    return true;
                }
            }
            else
            {
                logger?.LogInformation(LogSource.Modal, "TryGetModalOverlay: Desktop lifetime not found");
            }

            logger?.LogInformation(LogSource.Modal, "TryGetModalOverlay: Failed");
            return false;
        }

        public async Task<bool> ShowConfirmationAsync(string titleKey, string messageKey, string confirmTextKey = "ModalOK", string cancelTextKey = "ModalCancel")
        {
            logger?.LogInformation(LogSource.Modal, $"ShowConfirmationAsync called: titleKey='{titleKey}', messageKey='{messageKey}'");

            if (!settingsService.ShowConfirmModals)
            {
                logger?.LogInformation(LogSource.Modal, "ShowConfirmModals is disabled, returning true");
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

            logger?.LogInformation(LogSource.Modal, "Enqueueing confirmation modal");
            EnqueueModal(item);
            logger?.LogInformation(LogSource.Modal, "Waiting for confirmation modal result");
            var result = await item.TaskCompletionSource.Task;
            logger?.LogInformation(LogSource.Modal, $"Confirmation modal result: {result}");
            return result is bool boolResult ? boolResult : false;
        }

        private async Task<bool> ShowConfirmationImmediatelyAsync(string titleKey, string messageKey, string confirmTextKey, string cancelTextKey)
        {
            logger?.LogInformation(LogSource.Modal, $"ShowConfirmationImmediatelyAsync called: titleKey='{titleKey}'");

            if (!TryGetModalOverlay(out var modalOverlay))
            {
                logger?.LogError(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Failed to get modal overlay");
                return false;
            }

            logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Got modal overlay successfully");

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            currentConfirmationTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Creating ConfirmationModalView");

                var confirmationDialog = new ConfirmationModalView
                {
                    Title = localizationService.GetText(titleKey),
                    Message = localizationService.GetText(messageKey),
                    ConfirmText = localizationService.GetText(confirmTextKey),
                    CancelText = localizationService.GetText(cancelTextKey)
                };

                logger?.LogInformation(LogSource.Modal, $"ShowConfirmationImmediatelyAsync: Dialog created - Title='{confirmationDialog.Title}', Message='{confirmationDialog.Message}'");

                confirmationDialog.ConfirmClicked += () =>
                {
                    logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Confirm clicked");
                    currentConfirmationTask?.SetResult(true);
                    CloseCurrentModal();
                };

                confirmationDialog.CancelClicked += () =>
                {
                    logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Cancel clicked");
                    currentConfirmationTask?.SetResult(false);
                    CloseCurrentModal();
                };

                modalOverlay.BackgroundClicked += () =>
                {
                    logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Background clicked");
                    if (!currentConfirmationTask!.Task.IsCompleted)
                    {
                        currentConfirmationTask.SetResult(false);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = confirmationDialog;
                logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: Calling modalOverlay.ShowModal");
                modalOverlay.ShowModal(confirmationDialog);
                logger?.LogInformation(LogSource.Modal, "ShowConfirmationImmediatelyAsync: modalOverlay.ShowModal completed");
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
            if (!TryGetModalOverlay(out var modalOverlay)) return default(T);

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            currentDialogTask = new TaskCompletionSource<object?>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                modalOverlay.BackgroundClicked += () =>
                {
                    if (!currentDialogTask!.Task.IsCompleted)
                    {
                        currentDialogTask.SetResult(null);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = dialogContent;
                modalOverlay.ShowModal(dialogContent);
            });

            var result = await currentDialogTask.Task;
            return result is T typedResult ? typedResult : default(T);
        }

        public void CloseCurrentModal()
        {
            if (TryGetModalOverlay(out var modalOverlay) && currentModalContent != null)
            {
                modalOverlay.HideModal();
                currentModalContent = null;

                // Complete the dialog task if it hasn't been completed yet
                if (currentDialogTask != null && !currentDialogTask.Task.IsCompleted)
                {
                    currentDialogTask.SetResult(null);
                }
            }
        }

        public void CloseCurrentModalWithResult<T>(T result)
        {
            if (TryGetModalOverlay(out var modalOverlay) && currentModalContent != null)
            {
                // Set the result first, then close
                if (currentDialogTask != null && !currentDialogTask.Task.IsCompleted)
                {
                    currentDialogTask.SetResult(result);
                }

                modalOverlay.HideModal();
                currentModalContent = null;
            }
        }

        private async Task ShowAlphaBuildWarningAsync()
        {
            var warningView = new Views.Controls.AlphaBuildWarningView();

            await ShowDialogImmediatelyAsync<bool>(warningView, "");

        }

        private async Task<bool?> ShowDeviceConfigurationAsync(string deviceName)
        {
            var confirmationView = new DeviceConfigurationPromptView(deviceName);

            var result = await ShowDialogImmediatelyAsync<bool?>(confirmationView, "UnconfiguredDeviceTitle");


            // Get the BackEnd service to handle the device configuration
            if (result == true)
            {
                var backEnd = App.Services?.GetService<userspace_backend.BackEnd>();
                if (backEnd?.UnconfiguredActiveDevice != null)
                {
                    try
                    {

                        var deviceToAdd = backEnd.UnconfiguredActiveDevice;
                        bool success = backEnd.Devices.TryAddDevice(deviceToAdd.MapToData());

                        if (success)
                        {
                            backEnd.UnconfiguredActiveDevice = null;
                            backEnd.ApplySettingsOnly();

                            // Show success notification
                            userspace_backend.NotificationManager.QueueNotification(
                                "DeviceCreatedSuccessfully",
                                userspace_backend.NotificationType.Success,
                                deviceToAdd.Name.CurrentValidatedValue);
                        }
                        else
                        {

                            // Show error notification
                            userspace_backend.NotificationManager.QueueNotification(
                                "DeviceCreationFailed",
                                userspace_backend.NotificationType.Error,
                                deviceToAdd.Name.CurrentValidatedValue);
                        }
                    }
                    catch (Exception ex)
                    {

                        // Show error notification
                        userspace_backend.NotificationManager.QueueNotification(
                            "DeviceCreationError",
                            userspace_backend.NotificationType.Error,
                            ex.Message);
                    }
                }
                else
                {

                    // Show error notification
                    userspace_backend.NotificationManager.QueueNotification(
                        "NoUnconfiguredDevice",
                        userspace_backend.NotificationType.Error);
                }
            }
            else
            {
            }

            return result;
        }

        public void Dispose()
        {
            userspace_backend.NotificationManager.QueuedModalRequested -= OnBackEndQueuedModalRequested;
            App.AppLoadCompleted -= OnAppLoadCompleted;
            GC.SuppressFinalize(this);
        }
    }
}