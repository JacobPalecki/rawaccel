using System.Collections.Generic;
using DATA = userspace_backend.Data;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using System;
using System.ComponentModel;
using userspace_backend.Common;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using userspace_backend.Display;

namespace userspace_backend.Model
{
    public class ProfileModel : EditableSettingsCollectionV2<DATA.Profile>
    {
        public ProfileModel(
            IEditableSettingSpecific<string> name,
            IEditableSettingSpecific<int> outputDPI,
            IEditableSettingSpecific<double> yxRatio,
            IAccelerationModel acceleration,
            IHiddenModel hidden
            ) : base([name, outputDPI, yxRatio], [acceleration, hidden])
        {
            Name = name;
            OutputDPI = outputDPI;
            YXRatio = yxRatio;
            Acceleration = acceleration;
            Hidden = hidden;

            // Name and Output DPI do not need to generate a new curve preview
            Name.PropertyChanged += AnyNonPreviewPropertyChangedEventHandler;
            OutputDPI.PropertyChanged += AnyNonPreviewPropertyChangedEventHandler;

            // The rest of settings should generate a new curve preview
            YXRatio.PropertyChanged += AnyCurvePreviewPropertyChangedEventHandler;
            Acceleration.AnySettingChanged += AnyCurveSettingCollectionChangedEventHandler;
            Hidden.AnySettingChanged += AnyCurveSettingCollectionChangedEventHandler;

            // TODO: DI - Curve preview to DI
            CurvePreview = new CurvePreview();
            RecalculateDriverDataAndCurvePreview();
        }

        public string CurrentNameForDisplay => Name.ModelValue;

        public IEditableSettingSpecific<string> Name { get; set; }

        public IEditableSettingSpecific<int> OutputDPI { get; set; }

        public IEditableSettingSpecific<double> YXRatio { get; set; }

        public IAccelerationModel Acceleration { get; set; }

        public IHiddenModel Hidden { get; set; }

        public Profile CurrentValidatedDriverProfile { get; protected set; }

        public ICurvePreview CurvePreview { get; protected set; }

        protected IModelValueValidator<string> NameValidator { get; }

        public override DATA.Profile MapToData()
        {
            return new DATA.Profile()
            {
                Name = Name.ModelValue,
                OutputDPI = OutputDPI.ModelValue,
                YXRatio = YXRatio.ModelValue,
                Acceleration = Acceleration.MapToData(),
                Hidden = Hidden.MapToData(),
            };
        }

        protected void AnyNonPreviewPropertyChangedEventHandler(object? send, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(IEditableSettingSpecific<IComparable>.ModelValue)))
            {
                RecalculateDriverData();
            }
        }

        protected void AnyCurvePreviewPropertyChangedEventHandler(object? send, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(IEditableSettingSpecific<IComparable>.ModelValue)))
            {
                RecalculateDriverDataAndCurvePreview();
            }
        }

        protected void AnyCurveSettingCollectionChangedEventHandler(object? sender, EventArgs e)
        {
            // All settings collections currently require curve preview to be re-generated
            RecalculateDriverDataAndCurvePreview();
        }

        protected void RecalculateDriverData()
        {
            CurrentValidatedDriverProfile = DriverHelpers.MapProfileModelToDriver(this);
        }

        protected void RecalculateDriverDataAndCurvePreview()
        {
            RecalculateDriverData();
            CurvePreview.GeneratePoints(CurrentValidatedDriverProfile);
        }

        // TODO: DI - Add init to composition
        protected override void InitEditableSettingsAndCollections(DATA.Profile dataObject)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: NameValidator);
            OutputDPI = new EditableSetting<int>(
                displayName: "Output DPI",
                initialValue: dataObject.OutputDPI,
                parser: UserInputParsers.IntParser,
                validator: ModelValueValidators.DefaultIntValidator);
            YXRatio = new EditableSetting<double>(
                displayName: "Y/X Ratio",
                initialValue: dataObject.YXRatio,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Acceleration = new AccelerationModel(dataObject.Acceleration);
            Hidden = new HiddenModel(dataObject.Hidden);

            // Name and Output DPI do not need to generate a new curve preview
            Name.PropertyChanged += AnyNonPreviewPropertyChangedEventHandler;
            OutputDPI.PropertyChanged += AnyNonPreviewPropertyChangedEventHandler;

            // The rest of settings should generate a new curve preview
            YXRatio.PropertyChanged += AnyCurvePreviewPropertyChangedEventHandler;
            Acceleration.AnySettingChanged += AnyCurveSettingCollectionChangedEventHandler;
            Hidden.AnySettingChanged += AnyCurveSettingCollectionChangedEventHandler;
        }
    }
}
