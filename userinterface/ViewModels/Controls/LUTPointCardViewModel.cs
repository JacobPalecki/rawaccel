using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using userspace_backend.Model.EditableSettings;
using userspace_backend;
using userspace_backend.Logging;

namespace userinterface.ViewModels.Controls
{
    public partial class LUTPointCardViewModel : ViewModelBase
    {
        private readonly ILoggingService? loggingService;
        
        private string? lastXInterfaceValue;
        private string? lastYInterfaceValue;
        private string? lastXToastValue;
        private string? lastYToastValue;
        
        [ObservableProperty]
        private int pointIndex;
        
        [ObservableProperty]
        private bool hasValidationErrors;
        
        [ObservableProperty]
        private string validationMessage = string.Empty;

        public LUTPointCardViewModel(double x, double y, int index, ILoggingService? loggingService = null)
        {
            this.loggingService = loggingService;
            PointIndex = index;
            
            // Create EditableSetting instances with LUT-specific validators
            XCoordinate = new EditableSetting<double>(
                displayName: "X Coordinate",
                initialValue: x,
                parser: UserInputParsers.LUTXParser,
                validator: LUTModelValueValidators.LUTXValidator,
                autoUpdateFromInterface: true);
                
            YCoordinate = new EditableSetting<double>(
                displayName: "Y Coordinate", 
                initialValue: y,
                parser: UserInputParsers.LUTYParser,
                validator: LUTModelValueValidators.LUTYValidator,
                autoUpdateFromInterface: true);
                
            // Subscribe to property changes for validation feedback
            XCoordinate.PropertyChanged += OnCoordinatePropertyChanged;
            YCoordinate.PropertyChanged += OnCoordinatePropertyChanged;
            
            DeletePointCommand = new RelayCommand(OnDeletePoint);
            
            // Update validation status initially
            UpdateValidationStatus();
        }

        public EditableSetting<double> XCoordinate { get; }
        public EditableSetting<double> YCoordinate { get; }
        
        // Convenience properties for backward compatibility
        public double XValue
        {
            get => XCoordinate.CurrentValidatedValue;
            set => XCoordinate.InterfaceValue = value.ToString(CultureInfo.InvariantCulture);
        }
        
        public double YValue
        {
            get => YCoordinate.CurrentValidatedValue;
            set => YCoordinate.InterfaceValue = value.ToString("F2", CultureInfo.InvariantCulture);
        }
        
        public string XValueText
        {
            get => XCoordinate.InterfaceValue;
            set => XCoordinate.InterfaceValue = value;
        }
        
        public string YValueText
        {
            get => YCoordinate.InterfaceValue;
            set => YCoordinate.InterfaceValue = value;
        }

        public ICommand DeletePointCommand { get; }

        public event EventHandler<PointDeletedEventArgs>? PointDeleted;
        public event EventHandler<PointValueChangedEventArgs>? ValueChanged;

        private void OnDeletePoint()
        {
            PointDeleted?.Invoke(this, new PointDeletedEventArgs(this));
        }
        
        private void OnCoordinatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Debug: Log all property changes for troubleshooting
            var setting = sender as EditableSetting<double>;
            var coordinateType = ReferenceEquals(setting, XCoordinate) ? "X" : "Y";
            loggingService?.LogInformation(LogSource.LUT, 
                "DEBUG: Point {Index} {CoordinateType} property changed: {PropertyName} = '{InterfaceValue}' (ModelValue={ModelValue})", 
                PointIndex, coordinateType, e.PropertyName ?? "null", setting?.InterfaceValue ?? "null", setting?.ModelValue);
            
            // Detect EditableSetting validation failures and show toasts
            if (e.PropertyName == nameof(EditableSetting<double>.InterfaceValue))
            {
                DetectValidationFailure(setting, coordinateType);
            }
            
            if (e.PropertyName == nameof(EditableSetting<double>.CurrentValidatedValue))
            {
                // Notify about value changes
                ValueChanged?.Invoke(this, new PointValueChangedEventArgs(this, XValue, YValue));
                
                // Update validation status
                UpdateValidationStatus();
                
                // Notify UI about property changes for binding
                OnPropertyChanged(nameof(XValue));
                OnPropertyChanged(nameof(YValue));
            }
            else if (e.PropertyName == nameof(EditableSetting<double>.InterfaceValue))
            {
                // Log input changes at debug level for troubleshooting
                loggingService?.LogDebug(LogSource.LUT, 
                    "LUT Point {Index} {CoordinateType} input changed to: '{InterfaceValue}'", 
                    PointIndex, coordinateType, setting?.InterfaceValue ?? "null");
                
                // Update text binding properties
                OnPropertyChanged(nameof(XValueText));
                OnPropertyChanged(nameof(YValueText));
                
                // Update tracking of last interface values
                if (coordinateType == "X")
                    lastXInterfaceValue = setting?.InterfaceValue;
                else
                    lastYInterfaceValue = setting?.InterfaceValue;
            }
        }
        
        private void UpdateValidationStatus()
        {
            // Debug: Log validation check for troubleshooting
            loggingService?.LogInformation(LogSource.LUT, 
                "DEBUG: Point {Index} validation check - X: '{XValue}', Y: '{YValue}'", 
                PointIndex, XCoordinate?.InterfaceValue ?? "null", YCoordinate?.InterfaceValue ?? "null");
            
            // Check for validation errors in either coordinate
            bool xHasError = !string.IsNullOrEmpty(XCoordinate.InterfaceValue) && 
                           !UserInputParsers.LUTXParser.TryParse(XCoordinate.InterfaceValue, out _);
            bool yHasError = !string.IsNullOrEmpty(YCoordinate.InterfaceValue) && 
                           !UserInputParsers.LUTYParser.TryParse(YCoordinate.InterfaceValue, out _);
                           
            HasValidationErrors = xHasError || yHasError;
            
            if (xHasError)
            {
                var xVal = XCoordinate.InterfaceValue;
                string errorMessage;
                
                if (double.TryParse(xVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                {
                    if (x < LUTXValueValidator.MinValue)
                        errorMessage = $"must be at least {LUTXValueValidator.MinValue}";
                    else if (x > LUTXValueValidator.MaxValue)
                        errorMessage = $"must be no more than {LUTXValueValidator.MaxValue}";
                    else
                        errorMessage = "invalid value";
                }
                else
                {
                    errorMessage = "must be a valid number";
                }
                
                ValidationMessage = $"X value {errorMessage}";
            }
            else if (yHasError)
            {
                var yVal = YCoordinate.InterfaceValue;
                string errorMessage;
                
                if (double.TryParse(yVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    if (y < LUTYValueValidator.MinValue)
                        errorMessage = $"must be at least {LUTYValueValidator.MinValue}";
                    else if (y > LUTYValueValidator.MaxValue)
                        errorMessage = $"must be no more than {LUTYValueValidator.MaxValue}";
                    else
                        errorMessage = "invalid value";
                }
                else
                {
                    errorMessage = "must be a valid number";
                }
                
                ValidationMessage = $"Y value {errorMessage}";
            }
            else
            {
                ValidationMessage = string.Empty;
            }
        }

        private void DetectValidationFailure(EditableSetting<double>? setting, string coordinateType)
        {
            if (setting?.InterfaceValue == null) return;
            
            var currentValue = setting.InterfaceValue;
            var lastValue = coordinateType == "X" ? lastXInterfaceValue : lastYInterfaceValue;
            var lastToastValue = coordinateType == "X" ? lastXToastValue : lastYToastValue;
            
            // Skip detection on first load when lastValue is null
            if (lastValue == null) return;
            
            // Detect validation correction: same value set twice in a row indicates EditableSetting correction
            if (currentValue == lastValue)
            {
                // Only show toast if we haven't already shown one for this exact value
                if (currentValue != lastToastValue)
                {
                    loggingService?.LogInformation(LogSource.LUT,
                        "Validation correction detected - Point {Index} {CoordinateType} corrected back to: '{Value}'",
                        PointIndex, coordinateType, currentValue);
                    
                    // Show toast indicating that input was corrected
                    if (double.TryParse(currentValue, out double validValue))
                    {
                        var maxValue = coordinateType == "X" ? LUTXValueValidator.MaxValue : LUTYValueValidator.MaxValue;
                        
                        // Most likely the user exceeded the max value (common case like 9999 > 1000)
                        var toastKey = coordinateType == "X" ? "LUT_XValidationError" : "LUT_YValidationError";
                        var errorMessage = $"exceeds maximum of {maxValue}";
                        
                        NotificationManager.TriggerNotification(toastKey, NotificationType.Warning, 
                            $">{maxValue}", errorMessage);
                        
                        // Track that we showed toast for this value to prevent duplicates
                        if (coordinateType == "X")
                            lastXToastValue = currentValue;
                        else
                            lastYToastValue = currentValue;
                    }
                }
                else
                {
                    loggingService?.LogDebug(LogSource.LUT,
                        "DEBUG: Duplicate validation correction ignored - Point {Index} {CoordinateType}: '{Value}'",
                        PointIndex, coordinateType, currentValue);
                }
            }
            else
            {
                // Reset toast tracking when value actually changes
                if (coordinateType == "X" && lastXToastValue != currentValue)
                    lastXToastValue = null;
                else if (coordinateType == "Y" && lastYToastValue != currentValue)
                    lastYToastValue = null;
            }
        }

        public void UpdateTextFromValues()
        {
            // Update interface values to reflect current model values
            XCoordinate.InterfaceValue = XValue.ToString(CultureInfo.InvariantCulture);
            YCoordinate.InterfaceValue = YValue.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    public class PointDeletedEventArgs : EventArgs
    {
        public LUTPointCardViewModel Point { get; }

        public PointDeletedEventArgs(LUTPointCardViewModel point)
        {
            Point = point;
        }
    }

    public class PointValueChangedEventArgs : EventArgs
    {
        public LUTPointCardViewModel Point { get; }
        public double XValue { get; }
        public double YValue { get; }

        public PointValueChangedEventArgs(LUTPointCardViewModel point, double xValue, double yValue)
        {
            Point = point;
            XValue = xValue;
            YValue = yValue;
        }
    }
}