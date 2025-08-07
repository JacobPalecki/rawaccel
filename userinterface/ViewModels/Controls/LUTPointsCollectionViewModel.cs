using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using userspace_backend.Model.AccelDefinitions;

namespace userinterface.ViewModels.Controls
{
    public partial class LUTPointsCollectionViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool canAddPoints = true;

        [ObservableProperty]
        private int currentPointIndex = 0;

        public bool HasPoints => Points.Count > 0;
        
        public bool CanNavigatePrevious => CurrentPointIndex > 0;
        
        public bool CanNavigateNext => CurrentPointIndex < Points.Count - 1;
        
        public LUTPointCardViewModel? CurrentPoint => Points.Count > CurrentPointIndex ? Points[CurrentPointIndex] : null;
        
        public LUTPointCardViewModel? PreviousPoint => CurrentPointIndex > 0 ? Points[CurrentPointIndex - 1] : null;
        
        public LUTPointCardViewModel? NextPoint => CurrentPointIndex < Points.Count - 1 ? Points[CurrentPointIndex + 1] : null;

        public LUTPointsCollectionViewModel()
        {
            Points = new ObservableCollection<LUTPointCardViewModel>();
            Points.CollectionChanged += OnPointsCollectionChanged;
            
            AddPointCommand = new RelayCommand(AddPoint, () => CanAddPoints);
            ClearAllPointsCommand = new RelayCommand(ClearAllPoints, () => Points.Count > 0);
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
                var pointCard = new LUTPointCardViewModel(data[i], data[i + 1], (i / 2) + 1);
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

        private void AddPoint()
        {
            double nextXValue = 0;
            double nextYValue = 1;

            if (Points.Count > 0)
            {
                var lastPoint = Points.Last();
                nextXValue = lastPoint.XValue + 10;
                nextYValue = lastPoint.YValue;
            }

            var newPoint = new LUTPointCardViewModel(nextXValue, nextYValue, Points.Count + 1);
            SubscribeToPointEvents(newPoint);
            Points.Add(newPoint);
            
            UpdatePointIndices();
            CurrentPointIndex = Points.Count - 1; // Navigate to new point
            ((RelayCommand)ClearAllPointsCommand).NotifyCanExecuteChanged();
            UpdateNavigationProperties();
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

        private void ClearAllPoints()
        {
            foreach (var point in Points)
            {
                UnsubscribeFromPointEvents(point);
            }
            
            Points.Clear();
            CurrentPointIndex = 0;
            ((RelayCommand)ClearAllPointsCommand).NotifyCanExecuteChanged();
            UpdateNavigationProperties();
        }

        private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasPoints));
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs());
            UpdateNavigationProperties();
        }

        private void OnPointDeleted(object? sender, PointDeletedEventArgs e)
        {
            var deletedIndex = Points.IndexOf(e.Point);
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
            
            ((RelayCommand)ClearAllPointsCommand).NotifyCanExecuteChanged();
            UpdateNavigationProperties();
        }

        private void OnPointValueChanged(object? sender, PointValueChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs());
        }

        private void SubscribeToPointEvents(LUTPointCardViewModel point)
        {
            point.PointDeleted += OnPointDeleted;
            point.ValueChanged += OnPointValueChanged;
        }

        private void UnsubscribeFromPointEvents(LUTPointCardViewModel point)
        {
            point.PointDeleted -= OnPointDeleted;
            point.ValueChanged -= OnPointValueChanged;
        }

        private void UpdatePointIndices()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].PointIndex = i + 1;
            }
        }
    }

    public class CollectionChangedEventArgs : EventArgs
    {
    }
}