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
        public const double FastestHandSpeed = 200;   // Maximum input speed (counts/second)
        public const double CurvePointsResolution = 256; // Number of curve points to generate

        public static ICollection<double> CalculateCurvePointSpeeds()
        {
            List<double> curvePointSpeeds = new List<double>();

            // Calculate logarithmic distribution of speeds from SlowestHandSpeed to FastestHandSpeed
            // This provides more detail at lower speeds where mouse movement is more precise
            double ratio = FastestHandSpeed / SlowestHandSpeed;
            double sqrtRatio = Math.Sqrt(ratio);
            double middle = sqrtRatio * SlowestHandSpeed;
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
