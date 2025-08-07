using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;
using System.Windows.Input;

namespace userinterface.ViewModels.Controls
{
    public partial class LUTPointCardViewModel : ViewModelBase
    {
        [ObservableProperty]
        private double xValue;

        [ObservableProperty] 
        private double yValue;

        [ObservableProperty]
        private int pointIndex;

        [ObservableProperty]
        private string xValueText = "0";

        [ObservableProperty]
        private string yValueText = "0";

        public LUTPointCardViewModel(double x, double y, int index)
        {
            XValue = x;
            YValue = y;
            PointIndex = index;
            XValueText = x.ToString(CultureInfo.InvariantCulture);
            YValueText = y.ToString("F2", CultureInfo.InvariantCulture);
            DeletePointCommand = new RelayCommand(OnDeletePoint);
        }

        public ICommand DeletePointCommand { get; }

        public event EventHandler<PointDeletedEventArgs>? PointDeleted;
        public event EventHandler<PointValueChangedEventArgs>? ValueChanged;

        private void OnDeletePoint()
        {
            PointDeleted?.Invoke(this, new PointDeletedEventArgs(this));
        }

        partial void OnXValueChanged(double value)
        {
            ValueChanged?.Invoke(this, new PointValueChangedEventArgs(this, value, YValue));
        }

        partial void OnYValueChanged(double value)
        {
            ValueChanged?.Invoke(this, new PointValueChangedEventArgs(this, XValue, value));
        }

        partial void OnXValueTextChanged(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                if (Math.Abs(parsed - XValue) > 0.001) // Avoid circular updates
                {
                    XValue = parsed;
                }
            }
        }

        partial void OnYValueTextChanged(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                if (Math.Abs(parsed - YValue) > 0.001) // Avoid circular updates
                {
                    YValue = parsed;
                }
            }
        }

        public void UpdateTextFromValues()
        {
            XValueText = XValue.ToString(CultureInfo.InvariantCulture);
            YValueText = YValue.ToString("F2", CultureInfo.InvariantCulture);
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