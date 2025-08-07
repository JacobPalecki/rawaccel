using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class LUTPointCardViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int pointIndex;
        
        [ObservableProperty]
        private bool hasValidationErrors;
        
        [ObservableProperty]
        private string validationMessage = string.Empty;

        public LUTPointCardViewModel(double x, double y, int index)
        {
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
                // Update text binding properties
                OnPropertyChanged(nameof(XValueText));
                OnPropertyChanged(nameof(YValueText));
            }
        }
        
        private void UpdateValidationStatus()
        {
            // Check for validation errors in either coordinate
            bool xHasError = !string.IsNullOrEmpty(XCoordinate.InterfaceValue) && 
                           !UserInputParsers.LUTXParser.TryParse(XCoordinate.InterfaceValue, out _);
            bool yHasError = !string.IsNullOrEmpty(YCoordinate.InterfaceValue) && 
                           !UserInputParsers.LUTYParser.TryParse(YCoordinate.InterfaceValue, out _);
                           
            HasValidationErrors = xHasError || yHasError;
            
            if (xHasError)
            {
                var xVal = XCoordinate.InterfaceValue;
                if (double.TryParse(xVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                {
                    if (x < LUTXValueValidator.MinValue)
                        ValidationMessage = $"X value must be at least {LUTXValueValidator.MinValue}";
                    else if (x > LUTXValueValidator.MaxValue)
                        ValidationMessage = $"X value must be no more than {LUTXValueValidator.MaxValue}";
                    else
                        ValidationMessage = "Invalid X coordinate value";
                }
                else
                {
                    ValidationMessage = "X coordinate must be a valid number";
                }
            }
            else if (yHasError)
            {
                var yVal = YCoordinate.InterfaceValue;
                if (double.TryParse(yVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    if (y < LUTYValueValidator.MinValue)
                        ValidationMessage = $"Y value must be at least {LUTYValueValidator.MinValue}";
                    else if (y > LUTYValueValidator.MaxValue)
                        ValidationMessage = $"Y value must be no more than {LUTYValueValidator.MaxValue}";
                    else
                        ValidationMessage = "Invalid Y coordinate value";
                }
                else
                {
                    ValidationMessage = "Y coordinate must be a valid number";
                }
            }
            else
            {
                ValidationMessage = string.Empty;
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