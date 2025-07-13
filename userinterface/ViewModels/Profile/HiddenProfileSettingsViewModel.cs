using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class HiddenProfileSettingsViewModel : ViewModelBase
    {
        public HiddenProfileSettingsViewModel(BE.ProfileComponents.IHiddenModel hiddenBE)
        {
            HiddenBE = hiddenBE;
            RotationField = new EditableFieldViewModel(hiddenBE.RotationDegrees);
            SpeedCapField = new EditableFieldViewModel(hiddenBE.SpeedCap);
            LRRatioField = new EditableFieldViewModel(hiddenBE.LeftRightRatio);
            UDRatioField = new EditableFieldViewModel(hiddenBE.UpDownRatio);
            AngleSnappingField = new EditableFieldViewModel(hiddenBE.AngleSnappingDegrees);
            OutputSmoothingHalfLifeField = new EditableFieldViewModel(hiddenBE.OutputSmoothingHalfLife);
        }

        protected BE.ProfileComponents.IHiddenModel HiddenBE { get; }

        public EditableFieldViewModel RotationField { get; set; }

        public EditableFieldViewModel SpeedCapField { get; set; }

        public EditableFieldViewModel LRRatioField { get; set; }

        public EditableFieldViewModel UDRatioField { get; set; }

        public EditableFieldViewModel AngleSnappingField { get; set; }

        public EditableFieldViewModel OutputSmoothingHalfLifeField { get; set; }
    }
}
