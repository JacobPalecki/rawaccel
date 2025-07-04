using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{

    public class ClassicAccelerationDefinitionModel : AccelDefinitionModel<ClassicAccel>
    {
        public ClassicAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public IEditableSettingSpecific<double> Acceleration { get; set; }

        public IEditableSettingSpecific<double> Exponent { get; set; }

        public IEditableSettingSpecific<double> Offset { get; set;  }

        public IEditableSettingSpecific<double> Cap { get; set; }

        public override AccelArgs MapToDriver()
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

        public override Acceleration MapToData()
        {
            return new ClassicAccel()
            {
                Acceleration = Acceleration.ModelValue,
                Exponent = Exponent.ModelValue,
                Offset = Offset.ModelValue,
                Cap = Cap.ModelValue,
            };

        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Acceleration, Exponent, Offset, Cap ];
        }

        protected override IEnumerable<IEditableSettingsCollectionV2> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override ClassicAccel GenerateDefaultDataObject()
        {
            return new ClassicAccel()
            {
                Acceleration = 0.001,
                Exponent = 2,
                Offset = 0,
                Cap = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(ClassicAccel dataObject)
        {
            Acceleration = new EditableSetting<double>(
                displayName: "Acceleration",
                initialValue: dataObject.Acceleration,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Exponent = new EditableSetting<double>(
                displayName: "Exponent",
                initialValue: dataObject.Exponent,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Offset = new EditableSetting<double>(
                displayName: "Offset",
                initialValue: dataObject.Offset,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Cap = new EditableSetting<double>(
                displayName: "Cap",
                initialValue: dataObject.Cap,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
