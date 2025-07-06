using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface IJumpAccelerationDefinitionModel : IAccelDefinitionModelSpecific<JumpAccel>
    {
    }

    public class JumpAccelerationDefinitionModel : EditableSettingsCollectionV2<JumpAccel>, IJumpAccelerationDefinitionModel
    {
        public const string SmoothDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Smooth)}";
        public const string InputDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Input)}";
        public const string OutputDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Output)}";

        public JumpAccelerationDefinitionModel(
            [FromKeyedServices(SmoothDIKey)]IEditableSettingSpecific<double> smooth,
            [FromKeyedServices(InputDIKey)]IEditableSettingSpecific<double> input,
            [FromKeyedServices(OutputDIKey)]IEditableSettingSpecific<double> output)
            : base([smooth, input, output], [])
        {
            Smooth = smooth;
            Input = input;
            Output = output;
        }

        public IEditableSettingSpecific<double> Smooth { get; set; }

        public IEditableSettingSpecific<double> Input { get; set; }

        public IEditableSettingSpecific<double> Output { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.jump,
                smooth = Smooth.ModelValue,
                cap = new Vec2<double> { x = Input.ModelValue, y = Output.ModelValue },
            };
        }

        public override JumpAccel MapToData()
        {
            return new JumpAccel()
            {
                Smooth = Smooth.ModelValue,
                Input = Input.ModelValue,
                Output = Output.ModelValue
            };
        }

        protected override void TryMapEditableSettingsFromData(JumpAccel data)
        {
            Smooth.InterfaceValue = data.Smooth.ToString();
            Input.InterfaceValue = data.Input.ToString();
            Output.InterfaceValue = data.Output.ToString();
        }
    }
}
