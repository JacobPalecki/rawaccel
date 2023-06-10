using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public const string SensitivityChartName = "Sensitivity";

        public ProfilesViewModel()
        {
            LastMouseMove = new SignalPlotXYGeneric<double, double>()
            {
                Xs = LastMouseMoveX,
                Ys = LastMouseMoveY,
            };

            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

        public string ProfileOneTitle { get; set; } = "Profile1";

        public string ChartTitle { get; } = "Test Chart";

        private double[] LastMouseMoveX { get; } = new double[1];

        private double[] LastMouseMoveY { get; } = new double[1];

        private double AccumulatorX { get; set; } = 0;

        private double AccumulatorY { get; set; } = 0;

        private int LMMCount { get; set; } = 0;

        private SignalPlotXYGeneric<double, double> LastMouseMove { get; }

        private AvaPlot Sensitivity { get; set; }

        private Stopwatch Stopwatch { get; set; }

        public void InitializeSensitivity(AvaPlot plot)
        {
            Sensitivity = plot;
            plot.Plot.Add(LastMouseMove);
            Sensitivity.Plot.AddSignalConst(new double[] { 1, 1 }, sampleRate: 0.01);
        }

        public void SetLastMouseMove(float x, float y)
        {
            Accumulate(x, y);
            TryUpdate();
        }

        private void Accumulate(float x, float y)
        {
            AccumulatorX += x;
            AccumulatorY += y;
            LMMCount++;
        }

        private void TryUpdate()
        {
            if (Stopwatch.ElapsedMilliseconds > 7)
            {
                LastMouseMoveX[0] = AccumulatorX / LMMCount;
                LastMouseMoveY[0] = AccumulatorY / LMMCount;

                System.Diagnostics.Debug.WriteLine($"Writing last mouse move. x: [{LastMouseMoveX[0]}] y: [{LastMouseMoveY[0]}] time:[{DateTime.Now.Millisecond}] StopWatch:[{Stopwatch.ElapsedMilliseconds}]");

                const RenderType renderType = RenderType.LowQualityThenHighQualityDelayed;

                Stopwatch.Restart();
                Sensitivity.RefreshRequest(renderType);
                ResetAccumulation();
            }
        }

        private void ResetAccumulation()
        {
            AccumulatorX = 0;
            AccumulatorY = 0;
            LMMCount = 0;
        }
    }
}
