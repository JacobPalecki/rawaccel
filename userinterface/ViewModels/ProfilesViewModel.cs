using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using userinterface.Models.Splines;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
            ScatterModel = new PlotModel();
            var yaxis = new LinearAxis();
            var xaxis = new LinearAxis();
            xaxis.Position = AxisPosition.Bottom;
            xaxis.IsZoomEnabled = false;
            yaxis.Position = AxisPosition.Left;
            yaxis.IsZoomEnabled = false;
            ScatterModel.Axes.Add(xaxis);
            ScatterModel.Axes.Add(yaxis);

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

            SplineSeries = new LineSeries
            {
                StrokeThickness = 0,
                MarkerSize = 3,
                MarkerFill = OxyColors.Blue,
                MarkerStroke = OxyColors.Blue,
                MarkerType = MarkerType.Circle,
            };

            ScatterModel.Series.Add(line);
            ScatterModel.Series.Add(LastMouseMoveSeries);
            ScatterModel.Series.Add(SplineSeries);


            ControlPoints = new SplineMouseFollower();

            ScatterModel.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == OxyMouseButton.Left)
                {
                    var point = Axis.InverseTransform(e.Position, xaxis, yaxis);
                    System.Diagnostics.Debug.WriteLine($"Clicked chart at {point.X}, {point.Y}");
                    ControlPoints.Select(point.X, point.Y);
                    ControlPoints.FillSeries(SplineSeries);
                    ScatterModel.InvalidatePlot(true);
                }
            };

            ScatterModel.MouseUp += (s, e) =>
            {
                ControlPoints.LetGo();
            };

            ScatterModel.MouseMove += (s, e) =>
            {
                if (ControlPoints.IsSelected)
                {
                    var point = Axis.InverseTransform(e.Position, xaxis, yaxis);
                    ControlPoints.Drag(point.X, point.Y);
                    ControlPoints.FillSeries(SplineSeries);
                    System.Diagnostics.Debug.WriteLine($"Dragged chart at {point.X}, {point.Y}");
                    ScatterModel.InvalidatePlot(true);
                }
            };
        }

        public string ProfileOneTitle { get; set; } = "Profile1";

        public string ChartTitle { get; } = "Test Chart";

        public PlotModel ScatterModel { get; }

        public SplineMouseFollower ControlPoints { get; }

        private LineSeries LastMouseMoveSeries { get; set; }

        private LineSeries SplineSeries { get; set; }

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMoveSeries.Points[0] = new DataPoint(x, y);
            ScatterModel.InvalidatePlot(false);
        }
    }
}
