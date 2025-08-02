using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Display.Calculations;

namespace userspace_backend.Display
{
    public interface ICurvePreview
    {
        ObservableCollection<CurvePoint> Points { get; }

        void GeneratePoints(Profile profile);

        void SetPoints(IEnumerable<CurvePoint> points);
    }

    public class CurvePreview : ICurvePreview
    {
        public CurvePreview()
        {
            Points = new ObservableCollection<CurvePoint>();
            InitPoints();
        }

        public ObservableCollection<CurvePoint> Points { get; }

        public void GeneratePoints(Profile profile)
        {
            ManagedAccel accel = new ManagedAccel(profile).CreateStatelessCopy();

            foreach (CurvePoint point in Points)
            {
                // Apply acceleration to input speed (counts/second)
                var output = accel.Accelerate(point.MouseSpeed, 0, 1, 1);
                
                // Calculate output speed magnitude (counts/second)
                var outputSpeed = Math.Sqrt(Math.Pow(output.Item1, 2) + Math.Pow(output.Item2, 2));
                
                // Store as acceleration multiplier (dimensionless ratio)
                // Output = 1.0 means no acceleration, >1.0 means speed up, <1.0 means slow down
                point.Output = outputSpeed / point.MouseSpeed;
            }
        }

        public void SetPoints(IEnumerable<CurvePoint> points)
        {
            Points.Clear();
            foreach (var point in points)
            {
                Points.Add(point);
            }
        }

        protected void InitPoints()
        {
            // Generate logarithmically distributed input speeds (counts/second)
            ICollection<double> speeds = CurveCalculationHelpers.CalculateCurvePointSpeeds();
            
            foreach (double speed in speeds)
            {
                Points.Add(new CurvePoint() { MouseSpeed = speed, Output = 0.0 });
            }
        }
    }

    public partial class CurvePoint : ObservableObject
    {
        [ObservableProperty]
        public double mouseSpeed; // Input speed in counts/second

        [ObservableProperty]
        public double output; // Acceleration multiplier (dimensionless ratio: output_speed / input_speed)
    }

}
