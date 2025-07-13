using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public interface IHiddenModel : IEditableSettingsCollectionSpecific<Hidden>
    {
        IEditableSettingSpecific<double> RotationDegrees { get; }

        IEditableSettingSpecific<double> AngleSnappingDegrees { get; }

        IEditableSettingSpecific<double> LeftRightRatio { get; }

        IEditableSettingSpecific<double> UpDownRatio { get; }

        IEditableSettingSpecific<double> SpeedCap { get; }

        IEditableSettingSpecific<double> OutputSmoothingHalfLife { get; }
    }

    public class HiddenModel : EditableSettingsCollectionV2<Hidden>, IHiddenModel
    {
        public const string RotationDegreesDIKey = $"{nameof(HiddenModel)}.{nameof(RotationDegrees)}";
        public const string AngleSnappingDegreesDIKey = $"{nameof(HiddenModel)}.{nameof(AngleSnappingDegrees)}";
        public const string LeftRightRatioDIKey = $"{nameof(HiddenModel)}.{nameof(LeftRightRatio)}";
        public const string UpDownRatioDIKey = $"{nameof(HiddenModel)}.{nameof(UpDownRatio)}";
        public const string SpeedCapDIKey = $"{nameof(HiddenModel)}.{nameof(SpeedCap)}";
        public const string OutputSmoothingHalfLifeDIKey = $"{nameof(HiddenModel)}.{nameof(OutputSmoothingHalfLife)}";

        public HiddenModel(
            [FromKeyedServices(RotationDegreesDIKey)]IEditableSettingSpecific<double> rotationDegrees,
            [FromKeyedServices(AngleSnappingDegreesDIKey)]IEditableSettingSpecific<double> angleSnappingDegrees,
            [FromKeyedServices(LeftRightRatioDIKey)]IEditableSettingSpecific<double> leftRightRatio,
            [FromKeyedServices(UpDownRatioDIKey)]IEditableSettingSpecific<double> upDownRatio,
            [FromKeyedServices(SpeedCapDIKey)]IEditableSettingSpecific<double> speedCap,
            [FromKeyedServices(OutputSmoothingHalfLifeDIKey)]IEditableSettingSpecific<double> outputSmoothingHalfLife
            ) : base([rotationDegrees, angleSnappingDegrees, leftRightRatio, upDownRatio, speedCap, outputSmoothingHalfLife], [])
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

        protected override bool TryMapEditableSettingsCollectionsFromData(Hidden data)
        {
            return true;
        }

        protected override bool TryMapEditableSettingsFromData(Hidden data)
        {
            return RotationDegrees.TryUpdateModelDirectly(data.RotationDegrees)
                & AngleSnappingDegrees.TryUpdateModelDirectly(data.AngleSnappingDegrees)
                & LeftRightRatio.TryUpdateModelDirectly(data.LeftRightRatio)
                & UpDownRatio.TryUpdateModelDirectly(data.UpDownRatio)
                & SpeedCap.TryUpdateModelDirectly(data.SpeedCap)
                & OutputSmoothingHalfLife.TryUpdateModelDirectly(data.OutputSmoothingHalfLife);
        }
    }
}
