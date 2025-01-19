using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public class HiddenModel : EditableSettingsCollection<Hidden>
    {
        public HiddenModel(Hidden dataObject) : base(dataObject)
        {
        }

        public IEditableSettingSpecific<double> RotationDegrees { get; set; }

        public IEditableSettingSpecific<double> AngleSnappingDegrees { get; set; }

        public IEditableSettingSpecific<double> LeftRightRatio { get; set; }

        public IEditableSettingSpecific<double> UpDownRatio { get; set; }

        public IEditableSettingSpecific<double> SpeedCap { get; set; }

        public IEditableSettingSpecific<double> OutputSmoothingHalfLife { get; set; }

        public override Hidden MapToData()
        {
            return new Hidden()
            {
                RotationDegrees = RotationDegrees.ModelValue,
                AngleSnappingDegrees = AngleSnappingDegrees.ModelValue,
                LeftRightRatio = LeftRightRatio.ModelValue,
                UpDownRatio = UpDownRatio.ModelValue,
                SpeedCap = SpeedCap.ModelValue,
                OutputSmoothingHalfLife = OutputSmoothingHalfLife.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ RotationDegrees, AngleSnappingDegrees, LeftRightRatio, UpDownRatio, SpeedCap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Hidden dataObject)
        {
            RotationDegrees = new EditableSetting<double>(
                displayName: "Rotation",
                initialValue: dataObject?.RotationDegrees ?? 0,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            AngleSnappingDegrees = new EditableSetting<double>(
                displayName: "Angle Snapping",
                initialValue: dataObject?.AngleSnappingDegrees ?? 0,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            LeftRightRatio = new EditableSetting<double>(
                displayName: "L/R Ratio",
                initialValue: dataObject?.LeftRightRatio ?? 1,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            UpDownRatio = new EditableSetting<double>(
                displayName: "U/D Ratio",
                initialValue: dataObject?.UpDownRatio ?? 1,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            SpeedCap = new EditableSetting<double>(
                displayName: "Speed Cap",
                initialValue: dataObject?.SpeedCap ?? 0,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            OutputSmoothingHalfLife = new EditableSetting<double>(
                displayName: "Output Smoothing Half-Life",
                initialValue: dataObject?.OutputSmoothingHalfLife ?? 0,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
