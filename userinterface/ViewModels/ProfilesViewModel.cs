using Avalonia;
using Avalonia.Threading;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
            ScatterModel = new PlotModel();

            var line = new LineSeries
                         {
                             StrokeThickness = 3,
                             Color = OxyColors.Blue,
                         };
            line.Points.AddRange(new DataPoint[]
            {
                new DataPoint(0, 1),
                new DataPoint(100, 1),
            });

            LastMouseMoveSeries = new LineSeries
            {
                StrokeThickness = 0,
                MarkerSize = 5,
                MarkerFill = OxyColors.Red,
                MarkerStroke = OxyColors.Red,
                MarkerType = MarkerType.Circle,
            };
            LastMouseMoveSeries.Points.Add(new DataPoint(0, 1));

            ScatterModel.Series.Add(line);
            ScatterModel.Series.Add(LastMouseMoveSeries);
        }

        public string ProfileOneTitle { get; set; } = "Profile1";

        public string ChartTitle { get; } = "Test Chart";

        public PlotModel ScatterModel { get; }

        private LineSeries LastMouseMoveSeries { get; set; }

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMoveSeries.Points.RemoveAt(0);
            LastMouseMoveSeries.Points.Add(new DataPoint(x, y));
            ScatterModel.InvalidatePlot(false);
        }
    }
}
