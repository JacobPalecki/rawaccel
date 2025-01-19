using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public class AnisotropyModel : EditableSettingsCollection<Anisotropy>
    {
        public AnisotropyModel(Anisotropy dataObject) : base(dataObject)
        {
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

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [DomainX, DomainY, RangeX, RangeY, LPNorm];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Anisotropy dataObject)
        {
            DomainX = new EditableSetting<double>(
                displayName: "Domain X",
                initialValue: dataObject?.Domain?.X ?? 1,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            DomainY = new EditableSetting<double>(
                displayName: "Domain Y",
                initialValue: dataObject?.Domain?.Y ?? 1,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            RangeX = new EditableSetting<double>(
                displayName: "Range X",
                initialValue: dataObject?.Range?.X ?? 1,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            RangeY = new EditableSetting<double>(
                displayName: "Range Y",
                initialValue: dataObject?.Range?.Y ?? 1,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            LPNorm = new EditableSetting<double>(
                displayName: "LP Norm",
                initialValue: dataObject?.LPNorm ?? 2,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            CombineXYComponents = new EditableSetting<bool>(
                displayName: "Combine X and Y Components",
                initialValue: dataObject?.CombineXYComponents ?? false,
                parser: UserInputParsers.BoolParser,
                validator: ModelValueValidators.DefaultBoolValidator);
        }
    }
}
