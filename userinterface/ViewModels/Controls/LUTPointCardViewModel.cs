using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

        public LUTPointCardViewModel(double x, double y, int index)
        {
            XValue = x;
            YValue = y;
            PointIndex = index;
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