using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public interface IAnisotropyModel : IEditableSettingsCollectionSpecific<Anisotropy>
    {
        IEditableSettingSpecific<double> DomainX { get; }

        IEditableSettingSpecific<double> DomainY { get; }

        IEditableSettingSpecific<double> RangeX { get; }

        IEditableSettingSpecific<double> RangeY { get; }

        IEditableSettingSpecific<double> LPNorm { get; }

        IEditableSettingSpecific<bool> CombineXYComponents { get; }
    }

    public class AnisotropyModel : EditableSettingsCollectionV2<Anisotropy>, IAnisotropyModel
    {
        public const string DomainXDIKey = $"{nameof(AnisotropyModel)}.{nameof(DomainX)}";
        public const string DomainYDIKey = $"{nameof(AnisotropyModel)}.{nameof(DomainY)}";
        public const string RangeXDIKey = $"{nameof(AnisotropyModel)}.{nameof(RangeX)}";
        public const string RangeYDIKey = $"{nameof(AnisotropyModel)}.{nameof(RangeYDIKey)}";
        public const string LPNormDIKey = $"{nameof(AnisotropyModel)}.{nameof(LPNorm)}";
        public const string CombineXYComponentsDIKey = $"{nameof(AnisotropyModel)}.{nameof(CombineXYComponents)}";

        public AnisotropyModel(
            [FromKeyedServices(DomainXDIKey)]IEditableSettingSpecific<double> domainX,
            [FromKeyedServices(DomainYDIKey)]IEditableSettingSpecific<double> domainY,
            [FromKeyedServices(RangeXDIKey)]IEditableSettingSpecific<double> rangeX,
            [FromKeyedServices(RangeYDIKey)]IEditableSettingSpecific<double> rangeY,
            [FromKeyedServices(LPNormDIKey)]IEditableSettingSpecific<double> lpNorm,
            [FromKeyedServices(CombineXYComponentsDIKey)]IEditableSettingSpecific<bool> combineXYComponents
            ) : base([domainX, domainY, rangeX, rangeY, lpNorm, combineXYComponents], [])
        {
            DomainX = domainX;
            DomainY = domainY;
            RangeX = rangeX;
            RangeY = rangeY;
            LPNorm = lpNorm;
            CombineXYComponents = combineXYComponents;
        }

        public IEditableSettingSpecific<double> DomainX { get; set; }

        public IEditableSettingSpecific<double> DomainY { get; set; }

        public IEditableSettingSpecific<double> RangeX { get; set; }

        public IEditableSettingSpecific<double> RangeY { get; set; }

        public IEditableSettingSpecific<double> LPNorm { get; set; }

        public IEditableSettingSpecific<bool> CombineXYComponents { get; set; }

        public override Anisotropy MapToData()
        {
            return new Anisotropy()
            {
                Domain = new Vector2() { X = DomainX.ModelValue, Y = DomainY.ModelValue },
                Range = new Vector2() { X = RangeX.ModelValue, Y = RangeY.ModelValue },
                LPNorm = LPNorm.ModelValue,
            };
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(Anisotropy data)
        {
            // Nothing to do here
            return true;
        }

        protected override bool TryMapEditableSettingsFromData(Anisotropy data)
        {
            return DomainX.TryUpdateModelDirectly(data.Domain.X)
                & DomainY.TryUpdateModelDirectly(data.Domain.Y)
                & RangeX.TryUpdateModelDirectly(data.Range.X)
                & RangeY.TryUpdateModelDirectly(data.Range.Y)
                & LPNorm.TryUpdateModelDirectly(data.LPNorm)
                & CombineXYComponents.TryUpdateModelDirectly(data.CombineXYComponents);
        }
    }
}
