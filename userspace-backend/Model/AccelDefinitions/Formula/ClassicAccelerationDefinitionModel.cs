using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{

    public interface IClassicAccelerationDefinitionModel : IAccelDefinitionModelSpecific<ClassicAccel>
    {
    }

    public class ClassicAccelerationDefinitionModel : EditableSettingsCollectionV2<ClassicAccel>, IClassicAccelerationDefinitionModel
    {
        public const string AccelerationDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Acceleration)}";
        public const string ExponentDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Exponent)}";
        public const string OffsetDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Offset)}";
        public const string CapDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(CapDIKey)}";

        public ClassicAccelerationDefinitionModel(
            [FromKeyedServices(AccelerationDIKey)]IEditableSettingSpecific<double> acceleration,
            [FromKeyedServices(ExponentDIKey)]IEditableSettingSpecific<double> exponent,
            [FromKeyedServices(OffsetDIKey)]IEditableSettingSpecific<double> offset,
            [FromKeyedServices(CapDIKey)]IEditableSettingSpecific<double> cap)
            : base([acceleration, exponent, offset, cap], [])
        {
            Acceleration = acceleration;
            Exponent = exponent;
            Offset = offset;
            Cap = cap;
        }

        public IEditableSettingSpecific<double> Acceleration { get; set; }

        public IEditableSettingSpecific<double> Exponent { get; set; }

        public IEditableSettingSpecific<double> Offset { get; set;  }

        public IEditableSettingSpecific<double> Cap { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.classic,
                acceleration = Acceleration.ModelValue,
                exponentClassic = Exponent.ModelValue,
                inputOffset = Offset.ModelValue,
                cap = new Vec2<double> { x = 0, y = Cap.ModelValue },
                capMode = CapMode.output
            };
        }

        public override ClassicAccel MapToData()
        {
            return new ClassicAccel()
            {
                Acceleration = Acceleration.ModelValue,
                Exponent = Exponent.ModelValue,
                Offset = Offset.ModelValue,
                Cap = Cap.ModelValue,
            };
        }
    }
}
