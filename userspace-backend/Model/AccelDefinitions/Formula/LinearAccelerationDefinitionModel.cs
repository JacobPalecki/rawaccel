﻿using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface ILinearAccelerationDefinitionModel : IAccelDefinitionModelSpecific<LinearAccel>
    {
    }

    public class LinearAccelerationDefinitionModel
        : EditableSettingsSelectable<LinearAccel, FormulaAccel>,
        ILinearAccelerationDefinitionModel
    {
        public const string AccelerationDIKey = $"{nameof(LinearAccelerationDefinitionModel)}.{nameof(Acceleration)}";
        public const string OffsetDIKey = $"{nameof(LinearAccelerationDefinitionModel)}.{nameof(Offset)}";
        public const string CapDIKey = $"{nameof(LinearAccelerationDefinitionModel)}.{nameof(CapDIKey)}";

        public LinearAccelerationDefinitionModel(
            [FromKeyedServices(AccelerationDIKey)]IEditableSettingSpecific<double> acceleration,
            [FromKeyedServices(OffsetDIKey)]IEditableSettingSpecific<double> offset,
            [FromKeyedServices(CapDIKey)]IEditableSettingSpecific<double> cap)
            : base([acceleration, offset, cap], [])
        {
            Acceleration = acceleration;
            Offset = offset;
            Cap = cap;
        }

        public IEditableSettingSpecific<double> Acceleration { get; set; }

        public IEditableSettingSpecific<double> Offset { get; set;  }

        public IEditableSettingSpecific<double> Cap { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.classic,
                acceleration = Acceleration.ModelValue,
                exponentClassic = 2,
                inputOffset = Offset.ModelValue,
                cap = new Vec2<double> { x = 0, y = Cap.ModelValue },
                capMode = CapMode.output,
            };
        }

        public override LinearAccel MapToData()
        {
            return new LinearAccel()
            {
                Acceleration = Acceleration.ModelValue,
                Offset = Offset.ModelValue,
                Cap = Cap.ModelValue,
            };
        }

        protected override bool TryMapEditableSettingsFromData(LinearAccel data)
        {
            return Acceleration.TryUpdateModelDirectly(data.Acceleration)
                & Offset.TryUpdateModelDirectly(data.Offset)
                & Cap.TryUpdateModelDirectly(data.Cap);
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(LinearAccel data)
        {
            return true;
        }
    }
}
