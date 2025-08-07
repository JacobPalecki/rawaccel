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
            // Regenerate points with LUT-aware range if needed
            if (IsLookupTableProfile(profile))
            {
                double lutMaxX = GetLutMaximumX(profile);
                RegeneratePointsForRange(CurveCalculationHelpers.SlowestHandSpeed, lutMaxX);
            }
            else
            {
                // Ensure we have full range points for non-LUT profiles
                if (Points.Count == 0 || Points.Max(p => p.MouseSpeed) < CurveCalculationHelpers.FastestHandSpeed * 0.9)
                {
                    RegeneratePointsForRange(CurveCalculationHelpers.SlowestHandSpeed, CurveCalculationHelpers.FastestHandSpeed);
                }
            }

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

        private bool IsLookupTableProfile(Profile profile)
        {
            return profile.argsX.mode == AccelMode.lut;
        }

        private double GetLutMaximumX(Profile profile)
        {
            if (!IsLookupTableProfile(profile))
                return CurveCalculationHelpers.FastestHandSpeed; // Default to full range

            if (profile.argsX.data == null || profile.argsX.data.Length == 0)
                return CurveCalculationHelpers.FastestHandSpeed; // Default to full range

            // LUT data format: [x1, y1, x2, y2, x3, y3, ...]
            // Find maximum X value (every even index)
            double maxX = 0;
            for (int i = 0; i < profile.argsX.data.Length; i += 2)
            {
                if (i < profile.argsX.data.Length)
                {
                    maxX = Math.Max(maxX, profile.argsX.data[i]);
                }
            }

            // Add small buffer to ensure last point is included
            return Math.Max(maxX * 1.1, CurveCalculationHelpers.SlowestHandSpeed);
        }

        private void RegeneratePointsForRange(double minSpeed, double maxSpeed)
        {
            Points.Clear();
            
            // Generate logarithmically distributed input speeds for the specified range
            ICollection<double> speeds = CurveCalculationHelpers.CalculateCurvePointSpeeds(minSpeed, maxSpeed);
            
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
