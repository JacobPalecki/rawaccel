using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public interface ICoalescionModel : IEditableSettingsCollectionSpecific<Coalescion>
    {
        IEditableSettingSpecific<double> InputSmoothingHalfLife { get; }

        IEditableSettingSpecific<double> ScaleSmoothingHalfLife { get; }
    }

    public class CoalescionModel : EditableSettingsCollectionV2<Coalescion>, ICoalescionModel
    {
        public const string InputSmoothingHalfLifeDIKey = $"{nameof(CoalescionModel)}.{nameof(InputSmoothingHalfLife)}";
        public const string ScaleSmoothingHalfLifeDIKey = $"{nameof(CoalescionModel)}.{nameof(ScaleSmoothingHalfLife)}";

        public CoalescionModel(
            [FromKeyedServices(InputSmoothingHalfLifeDIKey)]IEditableSettingSpecific<double> inputSmoothingHalfLife,
            [FromKeyedServices(ScaleSmoothingHalfLifeDIKey)]IEditableSettingSpecific<double> scaleSmoothHalfLife)
            : base([inputSmoothingHalfLife, scaleSmoothHalfLife], [])
        {
            InputSmoothingHalfLife = inputSmoothingHalfLife;
            ScaleSmoothingHalfLife = scaleSmoothHalfLife;
        }

        public IEditableSettingSpecific<double> InputSmoothingHalfLife { get; set; }

        public IEditableSettingSpecific<double> ScaleSmoothingHalfLife { get; set; }

        public override Coalescion MapToData()
        {
            return new Coalescion()
            {
                InputSmoothingHalfLife = InputSmoothingHalfLife.ModelValue,
                ScaleSmoothingHalfLife = ScaleSmoothingHalfLife.ModelValue,
            };
        }
    }
}
