using System;
using System.Collections.Generic;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    /// <summary>
    /// Validator for LUT X-axis values (mouse speed).
    /// Ensures values are non-negative and within reasonable bounds.
    /// </summary>
    public class LUTXValueValidator : IModelValueValidator<double>
    {
        public const double MinValue = 0.0;
        public const double MaxValue = 10000.0; // Reasonable maximum mouse speed
        
        public bool Validate(double value)
        {
            return value >= MinValue && value <= MaxValue && !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    /// <summary>
    /// Validator for LUT Y-axis values (output sensitivity/velocity).
    /// Ensures values are non-negative and within reasonable bounds.
    /// </summary>
    public class LUTYValueValidator : IModelValueValidator<double>
    {
        public const double MinValue = 0.0;
        public const double MaxValue = 1000.0; // Reasonable maximum output value
        
        public bool Validate(double value)
        {
            return value >= MinValue && value <= MaxValue && !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    /// <summary>
    /// Validator for LUT coordinate sequences.
    /// Ensures X values are strictly increasing and collection doesn't exceed maximum points.
    /// </summary>
    public class LUTSequenceValidator : IModelValueValidator<double[]>
    {
        public const int MaxPoints = 257; // AccelArgs.MaxLutPoints from driver
        
        public bool Validate(double[] coordinates)
        {
            if (coordinates == null)
                return false;
                
            // Must have even number of values (pairs)
            if (coordinates.Length % 2 != 0)
                return false;
                
            // Must have at least one point
            if (coordinates.Length < 2)
                return false;
                
            // Cannot exceed maximum points
            int pointCount = coordinates.Length / 2;
            if (pointCount > MaxPoints)
                return false;
                
            // Validate individual values and sequence
            var xValidator = new LUTXValueValidator();
            var yValidator = new LUTYValueValidator();
            
            double lastX = double.NegativeInfinity;
            
            for (int i = 0; i < coordinates.Length - 1; i += 2)
            {
                double x = coordinates[i];
                double y = coordinates[i + 1];
                
                // Validate individual coordinate values
                if (!xValidator.Validate(x) || !yValidator.Validate(y))
                    return false;
                    
                // Ensure X values are strictly increasing
                if (x <= lastX)
                    return false;
                    
                lastX = x;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Enhanced ModelValueValidators with LUT-specific validators
    /// </summary>
    public static class LUTModelValueValidators
    {
        public static readonly LUTXValueValidator LUTXValidator = new LUTXValueValidator();
        public static readonly LUTYValueValidator LUTYValidator = new LUTYValueValidator();
        public static readonly LUTSequenceValidator LUTSequenceValidator = new LUTSequenceValidator();
    }
}