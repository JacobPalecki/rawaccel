using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls
{
    public partial class ToastContainerView : UserControl
    {
        private readonly Dictionary<Guid, CancellationTokenSource> activeAnimations = new();
        private readonly SemaphoreSlim operationSemaphore = new(1, 1);
        private readonly object animationLock = new();
        private volatile bool areAnimationsActive;

        private const double ToastHeight = 80.0;
        private const double ToastSpacing = -10;
        private const int AnimationDurationMs = 400;
        private const int EntryStaggerMs = 50;
        private const int ExitStaggerMs = 30;
        private const double BasePosition = 120.0;

        public ToastContainerView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ToastContainerViewModel viewModel)
            {
                viewModel.ToastItems.CollectionChanged += async (s, args) =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        // Focus on the new toast (index 0) when added
                        var focusIndex = args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ? 0 : -1;
                        await AnimateAllToastsToPositions(focusIndex);
                    });
                };
            }
        }

        private double CalculatePositionForIndex(int index)
        {
            return -(index * (ToastHeight + ToastSpacing));
        }

        public async Task AnimateAllToastsToPositions(int focusToastIndex = -1)
        {
            await operationSemaphore.WaitAsync();
            try
            {
                lock (animationLock)
                {
                    areAnimationsActive = true;
                }

                var itemsControl = this.FindControl<ItemsControl>("ToastItemsControl");
                if (itemsControl == null) return;

                var toastViews = GetToastViews(itemsControl);
                if (!toastViews.Any()) return;

                var animationTasks = new List<Task>();

                for (int i = 0; i < toastViews.Count; i++)
                {
                    var staggerDelay = CalculateStaggerDelay(i, focusToastIndex, toastViews.Count);
                    var position = i;
                    var toastView = toastViews[i];

                    animationTasks.Add(Task.Run(async () =>
                    {
                        await Task.Delay(staggerDelay);
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await AnimateToastToPosition(toastView, position);
                        });
                    }));
                }

                await Task.WhenAll(animationTasks);
            }
            finally
            {
                lock (animationLock)
                {
                    areAnimationsActive = false;
                }
                operationSemaphore.Release();
            }
        }

        private async Task AnimateToastToPosition(ToastView toast, int position)
        {
            if (toast.DataContext is not ToastViewModel toastViewModel)
                return;

            var toastId = toastViewModel.Id;

            lock (animationLock)
            {
                if (activeAnimations.TryGetValue(toastId, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                    activeAnimations.Remove(toastId);
                }
            }

            var targetY = CalculatePositionForIndex(position);
            var targetTransform = $"translate(0px, {targetY}px)";
            
            // Check if already at target position
            var currentTransformString = toast.RenderTransform?.ToString();
            if (currentTransformString == targetTransform)
            {
                toast.ZIndex = position;
                return;
            }

            var cts = new CancellationTokenSource();
            lock (animationLock)
            {
                activeAnimations[toastId] = cts;
            }

            try
            {
                // Check if this is an entry animation (coming from below screen)
                var isEntry = string.IsNullOrEmpty(currentTransformString) || 
                             currentTransformString.Contains($"translate(0px, {BasePosition}px)");
                if (isEntry)
                {
                    toast.Opacity = 0.0;
                }

                toast.ZIndex = position;

                // Set transform and opacity - transitions will handle the animation
                toast.RenderTransform = TransformOperations.Parse(targetTransform);
                toast.Opacity = 1.0;

                // Wait for animation to complete (based on transition duration)
                await Task.Delay(AnimationDurationMs, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Toast animation cancelled for position {position}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error in toast animation: {ex.Message}");
            }
            finally
            {
                lock (animationLock)
                {
                    activeAnimations.Remove(toastId);
                }
                cts?.Dispose();
            }
        }

        public async Task AnimateToastExit(ToastView toast)
        {
            if (toast.DataContext is not ToastViewModel toastViewModel)
                return;

            var toastId = toastViewModel.Id;

            lock (animationLock)
            {
                if (activeAnimations.TryGetValue(toastId, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                    activeAnimations.Remove(toastId);
                }
            }

            var cts = new CancellationTokenSource();
            lock (animationLock)
            {
                activeAnimations[toastId] = cts;
            }

            try
            {
                // Set exit transform and opacity - transitions will handle the animation
                toast.RenderTransform = TransformOperations.Parse($"translate(0px, {BasePosition}px)");
                toast.Opacity = 0.0;

                // Wait for animation to complete (based on transition duration)
                await Task.Delay(AnimationDurationMs, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Toast exit animation cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error in toast exit animation: {ex.Message}");
            }
            finally
            {
                lock (animationLock)
                {
                    activeAnimations.Remove(toastId);
                }
                cts?.Dispose();
            }
        }

        private int CalculateStaggerDelay(int index, int focusIndex, int totalCount)
        {
            if (focusIndex >= 0 && focusIndex < totalCount)
            {
                var distance = Math.Abs(index - focusIndex);
                var clampedDistance = Math.Min(distance, 3);
                return clampedDistance * EntryStaggerMs;
            }

            return index * EntryStaggerMs;
        }

        private List<ToastView> GetToastViews(ItemsControl itemsControl)
        {
            var toastViews = new List<ToastView>();
            
            var presenter = itemsControl.Presenter;
            if (presenter?.Panel == null) return toastViews;

            foreach (var child in presenter.Panel.Children)
            {
                if (child is ContentPresenter contentPresenter && 
                    contentPresenter.Child is ToastView toastView)
                {
                    toastViews.Add(toastView);
                }
            }

            return toastViews;
        }

        public bool AreAnimationsActive
        {
            get
            {
                lock (animationLock)
                {
                    return areAnimationsActive;
                }
            }
        }
    }
}