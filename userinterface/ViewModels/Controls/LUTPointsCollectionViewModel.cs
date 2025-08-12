using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Services;
using userspace_backend.Logging;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class LUTPointsCollectionViewModel : ViewModelBase
    {
        private readonly INotificationService? notificationService;
        private readonly ILoggingService? loggingService;
        private readonly IModalService? modalService;
        private readonly LocalizationService? localizationService;

        [ObservableProperty]
        private bool canAddPoints = true;

        [ObservableProperty]
        private int currentPointIndex = 0;

        public bool HasPoints => Points.Count > 0;
        
        public string PointsCountLabel => localizationService != null 
            ? string.Format(localizationService.GetText("LutPointsCountFormat"), Points.Count)
            : $"{Points.Count} points";
        
        public bool CanNavigatePrevious => CurrentPointIndex > 0;
        
        public bool CanNavigateNext => CurrentPointIndex < Points.Count - 1;
        
        public LUTPointCardViewModel? CurrentPoint => Points.Count > CurrentPointIndex ? Points[CurrentPointIndex] : null;
        
        public LUTPointCardViewModel? PreviousPoint => CurrentPointIndex > 0 ? Points[CurrentPointIndex - 1] : null;
        
        public LUTPointCardViewModel? NextPoint => CurrentPointIndex < Points.Count - 1 ? Points[CurrentPointIndex + 1] : null;

        public LUTPointsCollectionViewModel(INotificationService? notificationService = null, ILoggingService? loggingService = null, IModalService? modalService = null, LocalizationService? localizationService = null)
        {
            this.notificationService = notificationService;
            this.loggingService = loggingService;
            this.modalService = modalService;
            this.localizationService = localizationService;
            
            // Subscribe to language changes
            if (this.localizationService != null)
            {
                this.localizationService.PropertyChanged += OnLocalizationChanged;
            }
            
            Points = new ObservableCollection<LUTPointCardViewModel>();
            Points.CollectionChanged += OnPointsCollectionChanged;
            
            AddPointCommand = new RelayCommand(TryAddPoint);
            ClearAllPointsCommand = new AsyncRelayCommand(ClearAllPointsAsync, () => Points.Count > 0);
            NavigatePreviousCommand = new RelayCommand(NavigatePrevious, () => CanNavigatePrevious);
            NavigateNextCommand = new RelayCommand(NavigateNext, () => CanNavigateNext);
        }

        public ObservableCollection<LUTPointCardViewModel> Points { get; }

        public ICommand AddPointCommand { get; }
        public ICommand ClearAllPointsCommand { get; }
        public ICommand NavigatePreviousCommand { get; }
        public ICommand NavigateNextCommand { get; }

        public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;

        public void LoadFromData(double[] data)
        {
            Points.Clear();

            for (int i = 0; i < data.Length - 1; i += 2)
            {
                var pointCard = new LUTPointCardViewModel(data[i], data[i + 1], (i / 2) + 1, loggingService, localizationService);
                SubscribeToPointEvents(pointCard);
                Points.Add(pointCard);
            }

            UpdatePointIndices();
        }

        public double[] ConvertToData()
        {
            var coordinates = new List<double>();
            
            foreach (var point in Points.OrderBy(p => p.PointIndex))
            {
                coordinates.Add(point.XValue);
                coordinates.Add(point.YValue);
            }

            return coordinates.ToArray();
        }

        private void TryAddPoint()
        {
            // Check maximum points constraint and prevent adding beyond limit
            if (Points.Count >= LUTSequenceValidator.MaxPoints)
            {
                loggingService?.LogWarning(LogSource.LUT, "Attempted to add point beyond maximum limit: {MaxPoints}", LUTSequenceValidator.MaxPoints);
                return;
            }
            
            AddPoint();
        }
        
        private void AddPoint()
        {

            double nextXValue = 0;
            double nextYValue = 1;

            // Smart defaults for new points
            if (Points.Count > 0)
            {
                var lastPoint = Points.Last();
                // Calculate intelligent X increment
                if (Points.Count == 1)
                {
                    nextXValue = lastPoint.XValue + 10; // Default increment for second point
                }
                else
                {
                    // Calculate average gap for more intelligent spacing
                    var averageGap = CalculateAverageXGap();
                    nextXValue = lastPoint.XValue + averageGap;
                }
                
                // Intelligent Y value interpolation
                nextYValue = CalculateInterpolatedYValue(nextXValue);
            }

            var newPoint = new LUTPointCardViewModel(nextXValue, nextYValue, Points.Count + 1, loggingService, localizationService);
            SubscribeToPointEvents(newPoint);
            Points.Add(newPoint);
            
            UpdatePointIndices();
            CurrentPointIndex = Points.Count - 1; // Navigate to new point
            ((AsyncRelayCommand)ClearAllPointsCommand).NotifyCanExecuteChanged();
            UpdateNavigationProperties();
            
            // Update max points constraint
            CanAddPoints = Points.Count < LUTSequenceValidator.MaxPoints;
            ((RelayCommand)AddPointCommand).NotifyCanExecuteChanged();
            
            // Show toast when maximum points reached
            if (Points.Count >= LUTSequenceValidator.MaxPoints)
            {
                notificationService?.ShowWarningToast("LUT_MaxPointsReached", 6000, LUTSequenceValidator.MaxPoints);
            }
            
            // Log the addition (no toast notification for successful point additions)
            loggingService?.LogInformation(LogSource.LUT, "LUT point added: ({X}, {Y}), Total points: {Count}", nextXValue, nextYValue, Points.Count);
        }
        
        private double CalculateAverageXGap()
        {
            if (Points.Count < 2) return 10.0; // Default gap
            
            double totalGap = 0;
            for (int i = 1; i < Points.Count; i++)
            {
                totalGap += Points[i].XValue - Points[i - 1].XValue;
            }
            
            return Math.Max(1.0, totalGap / (Points.Count - 1)); // Ensure minimum gap of 1.0
        }
        
        private double CalculateInterpolatedYValue(double targetX)
        {
            if (Points.Count == 0) return 1.0; // Default Y value
            if (Points.Count == 1) return Points[0].YValue; // Use existing point's Y value
            
            var sortedPoints = Points.OrderBy(p => p.XValue).ToList();
            
            // If X is before all existing points, use first point's Y value
            if (targetX <= sortedPoints[0].XValue)
            {
                return sortedPoints[0].YValue;
            }
            
            // If X is after all existing points, extrapolate based on last two points
            if (targetX >= sortedPoints.Last().XValue)
            {
                if (sortedPoints.Count >= 2)
                {
                    var lastPoint = sortedPoints.Last();
                    var secondLastPoint = sortedPoints[sortedPoints.Count - 2];
                    
                    // Linear extrapolation: maintain the same slope
                    var slope = (lastPoint.YValue - secondLastPoint.YValue) / (lastPoint.XValue - secondLastPoint.XValue);
                    var extrapolatedY = lastPoint.YValue + slope * (targetX - lastPoint.XValue);
                    
                    // Ensure Y value stays within reasonable bounds
                    return Math.Max(0.1, Math.Min(1000.0, extrapolatedY));
                }
                return sortedPoints.Last().YValue;
            }
            
            // Find interpolation points
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var leftPoint = sortedPoints[i];
                var rightPoint = sortedPoints[i + 1];
                
                if (targetX >= leftPoint.XValue && targetX <= rightPoint.XValue)
                {
                    // Linear interpolation between the two points
                    var ratio = (targetX - leftPoint.XValue) / (rightPoint.XValue - leftPoint.XValue);
                    var interpolatedY = leftPoint.YValue + ratio * (rightPoint.YValue - leftPoint.YValue);
                    
                    // Ensure Y value stays within reasonable bounds
                    return Math.Max(0.1, Math.Min(1000.0, interpolatedY));
                }
            }
            
            // Fallback - should not reach here
            return sortedPoints.Last().YValue;
        }
        
        private void NavigatePrevious()
        {
            if (CanNavigatePrevious)
            {
                CurrentPointIndex--;
                UpdateNavigationProperties();
            }
        }
        
        private void NavigateNext()
        {
            if (CanNavigateNext)
            {
                CurrentPointIndex++;
                UpdateNavigationProperties();
            }
        }
        
        private void UpdateNavigationProperties()
        {
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(CurrentPoint));
            OnPropertyChanged(nameof(PreviousPoint));
            OnPropertyChanged(nameof(NextPoint));
            ((RelayCommand)NavigatePreviousCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NavigateNextCommand).NotifyCanExecuteChanged();
        }

        private async Task ClearAllPointsAsync()
        {
            var pointCount = Points.Count;
            
            // Show confirmation modal
            if (modalService != null)
            {
                loggingService?.LogInformation(LogSource.LUT, "Showing clear all points confirmation modal for {Count} points", pointCount);
                var confirmed = await modalService.ShowConfirmationAsync(
                    "LUT_ClearAllPointsTitle",
                    "LUT_ClearAllPointsMessage",
                    "LUT_ClearAllPointsConfirm",
                    "ModalCancel");
                    
                if (!confirmed)
                {
                    loggingService?.LogInformation(LogSource.LUT, "Clear all points operation cancelled by user");
                    return;
                }
            }
            
            // Set index to 0 and update navigation BEFORE clearing to avoid index issues
            CurrentPointIndex = 0;
            UpdateNavigationProperties();
            
            // Unsubscribe from events before clearing
            foreach (var point in Points)
            {
                UnsubscribeFromPointEvents(point);
            }
            
            // Clear the collection
            Points.Clear();
            
            // Update commands after clearing
            ((AsyncRelayCommand)ClearAllPointsCommand).NotifyCanExecuteChanged();
            
            // Re-enable adding points after clearing
            CanAddPoints = true;
            ((RelayCommand)AddPointCommand).NotifyCanExecuteChanged();
            
            // Final navigation update to ensure consistency
            UpdateNavigationProperties();
            
            // Success notification
            notificationService?.ShowSuccessToast("LUT_AllPointsCleared", 3000, pointCount);
            loggingService?.LogInformation(LogSource.LUT, "All LUT points cleared, {Count} points removed", pointCount);
        }

        private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasPoints));
            OnPropertyChanged(nameof(PointsCountLabel));
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs());
            UpdateNavigationProperties();
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == LocalizationService.LanguageChangedPropertyName)
            {
                OnPropertyChanged(nameof(PointsCountLabel));
            }
        }

        private void OnPointDeleted(object? sender, PointDeletedEventArgs e)
        {
            var deletedIndex = Points.IndexOf(e.Point);
            var deletedX = e.Point.XValue;
            var deletedY = e.Point.YValue;
            
            UnsubscribeFromPointEvents(e.Point);
            Points.Remove(e.Point);
            UpdatePointIndices();
            
            // Adjust current index if needed
            if (CurrentPointIndex >= Points.Count && Points.Count > 0)
            {
                CurrentPointIndex = Points.Count - 1;
            }
            else if (CurrentPointIndex > deletedIndex && CurrentPointIndex > 0)
            {
                CurrentPointIndex--;
            }
            
            // Re-enable adding points if we were at the limit
            CanAddPoints = Points.Count < LUTSequenceValidator.MaxPoints;
            ((RelayCommand)AddPointCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ClearAllPointsCommand).NotifyCanExecuteChanged();
            UpdateNavigationProperties();
            
            // Success notification
            notificationService?.ShowInfoToast("LUT_PointDeleted", 3000, deletedX, deletedY);
            loggingService?.LogInformation(LogSource.LUT, "LUT point deleted: ({X}, {Y}), Remaining points: {Count}", deletedX, deletedY, Points.Count);
        }

        private void OnPointValueChanged(object? sender, PointValueChangedEventArgs e)
        {
            // Debug: Log every point value change for troubleshooting
            loggingService?.LogInformation(LogSource.LUT, 
                "DEBUG: Point value changed - Point {Index}: ({X}, {Y})", 
                e.Point.PointIndex, e.XValue, e.YValue);
            
            // Check for sequence validation errors (X values must be strictly increasing)
            ValidatePointSequence(e.Point);
            
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs());
        }
        
        private void ValidatePointSequence(LUTPointCardViewModel changedPoint)
        {
            var sortedPoints = Points.OrderBy(p => p.PointIndex).ToList();
            var changedIndex = sortedPoints.IndexOf(changedPoint);
            
            if (changedIndex < 0) return; // Point not found
            
            // Check if X value violates sequence constraint
            bool hasSequenceError = false;
            string? errorMessage = null;
            
            // Check against previous point
            if (changedIndex > 0)
            {
                var prevPoint = sortedPoints[changedIndex - 1];
                if (changedPoint.XValue <= prevPoint.XValue)
                {
                    hasSequenceError = true;
                    errorMessage = $"X value must be greater than {prevPoint.XValue:F1} (previous point)";
                }
            }
            
            // Check against next point
            if (!hasSequenceError && changedIndex < sortedPoints.Count - 1)
            {
                var nextPoint = sortedPoints[changedIndex + 1];
                if (changedPoint.XValue >= nextPoint.XValue)
                {
                    hasSequenceError = true;
                    errorMessage = $"X value must be less than {nextPoint.XValue:F1} (next point)";
                }
            }
            
            if (hasSequenceError)
            {
                notificationService?.ShowWarningToast("LUT_SequenceError", 5000, errorMessage ?? "X values must increase");
                loggingService?.LogWarning(LogSource.LUT, "LUT sequence validation error: {Error}", errorMessage);
            }
        }

        private void SubscribeToPointEvents(LUTPointCardViewModel point)
        {
            point.PointDeleted += OnPointDeleted;
            point.ValueChanged += OnPointValueChanged;
            point.SwapYRequested += OnSwapYRequested;
        }

        private void UnsubscribeFromPointEvents(LUTPointCardViewModel point)
        {
            point.PointDeleted -= OnPointDeleted;
            point.ValueChanged -= OnPointValueChanged;
            point.SwapYRequested -= OnSwapYRequested;
        }

        private void OnSwapYRequested(object? sender, SwapYValuesEventArgs e)
        {
            var pointIndex = Points.IndexOf(e.Point);
            if (pointIndex < 0) return;

            int targetIndex = e.Direction == SwapDirection.Previous ? pointIndex - 1 : pointIndex + 1;

            // Validate bounds
            if (targetIndex < 0 || targetIndex >= Points.Count) return;

            // Swap Y values only (preserve X values)
            var currentY = e.Point.YValue;
            var targetY = Points[targetIndex].YValue;

            // Perform the swap
            e.Point.YValue = targetY;
            Points[targetIndex].YValue = currentY;

            // Move selection to follow the swapped point
            CurrentPointIndex = targetIndex;
            UpdateNavigationProperties();

            loggingService?.LogInformation(LogSource.LUT, 
                "Swapped Y values between Point {Index1} and Point {Index2}: Y1={Y1} <-> Y2={Y2}", 
                e.Point.PointIndex, Points[targetIndex].PointIndex, currentY, targetY);

            // Notify that collection changed for chart update
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs());
        }

        private void UpdateSwapCapabilities()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].CanSwapWithPrevious = i > 0;
                Points[i].CanSwapWithNext = i < Points.Count - 1;
            }
        }


        private void UpdatePointIndices()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].PointIndex = i + 1;
            }
            UpdateSwapCapabilities();
        }
    }

    public class CollectionChangedEventArgs : EventArgs
    {
    }
}