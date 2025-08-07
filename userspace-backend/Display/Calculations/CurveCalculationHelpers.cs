using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Display.Calculations
{
    public static class CurveCalculationHelpers
    {
        // Speed range for acceleration curves (in counts/second)
        public const double SlowestHandSpeed = 0.05;  // Minimum input speed (counts/second)
        public const double FastestHandSpeed = 500;   // Maximum input speed (counts/second)
        public const double CurvePointsResolution = 256; // Number of curve points to generate

        public static ICollection<double> CalculateCurvePointSpeeds()
        {
            return CalculateCurvePointSpeeds(SlowestHandSpeed, FastestHandSpeed);
        }

        public static ICollection<double> CalculateCurvePointSpeeds(double minSpeed, double maxSpeed)
        {
            List<double> curvePointSpeeds = new List<double>();

            // Calculate logarithmic distribution of speeds from minSpeed to maxSpeed
            // This provides more detail at lower speeds where mouse movement is more precise
            double ratio = maxSpeed / minSpeed;
            double sqrtRatio = Math.Sqrt(ratio);
            double middle = sqrtRatio * minSpeed;
            double increment = 2.0 / (CurvePointsResolution - 1.0);

            for (double i = -1; i <= 1; i += increment)
            {
                // Generate speed values distributed logarithmically (counts/second)
                double speed = middle * Math.Pow(sqrtRatio, i);
                curvePointSpeeds.Add(speed);
            }

            return curvePointSpeeds;
        }
    }
}
