﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model;

namespace userspace_backend.Common
{
    public static class DriverHelpers
    {
        public static Profile MapProfileModelToDriver(ProfileModel model)
        {
            return new Profile()
            {
                outputDPI = model.OutputDPI.ModelValue,
                yxOutputDPIRatio = model.YXRatio.ModelValue,
                argsX = model.Acceleration.MapToDriver(),
                domainXY = new Vec2<double>
                {
                    x = model.Acceleration.Anisotropy.DomainX.ModelValue,
                    y = model.Acceleration.Anisotropy.DomainY.ModelValue,
                },
                rangeXY = new Vec2<double>
                {
                    x = model.Acceleration.Anisotropy.RangeX.ModelValue,
                    y = model.Acceleration.Anisotropy.RangeY.ModelValue,
                },
                rotation = model.Hidden.RotationDegrees.ModelValue,
                lrOutputDPIRatio = model.Hidden.LeftRightRatio.ModelValue,
                udOutputDPIRatio = model.Hidden.UpDownRatio.ModelValue,
                snap = model.Hidden.AngleSnappingDegrees.ModelValue,
                maximumSpeed = model.Hidden.SpeedCap.ModelValue,
                minimumSpeed = 0,
                inputSpeedArgs = new SpeedArgs
                {
                    combineMagnitudes = model.Acceleration.Anisotropy.CombineXYComponents.ModelValue,
                    lpNorm = model.Acceleration.Anisotropy.LPNorm.ModelValue,
                    outputSmoothHalflife = model.Hidden.OutputSmoothingHalfLife.ModelValue,
                    inputSmoothHalflife = model.Acceleration.Coalescion.InputSmoothingHalfLife.ModelValue,
                    scaleSmoothHalflife = model.Acceleration.Coalescion.ScaleSmoothingHalfLife.ModelValue,
                }
            };

        }
    }
}
