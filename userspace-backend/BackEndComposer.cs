using Microsoft.Extensions.DependencyInjection;
using System;
using userspace_backend.Model;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;

namespace userspace_backend
{
    public static class BackEndComposer
    {
        public static IServiceProvider Compose(IServiceCollection services)
        {
            services.AddSingleton<ISystemDevicesRetriever, SystemDevicesRetriever>();
            services.AddSingleton<ISystemDevicesProvider, SystemDevicesProvider>();

            // Hidden
            services.AddTransient<IHiddenModel, HiddenModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.RotationDegreesDIKey, (_, _) =>
                        new EditableSettingV2<double>(
                        displayName: "Rotation",
                        initialValue: 0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.AngleSnappingDegreesDIKey, (_, _) =>
                        new EditableSettingV2<double>(
                            displayName: "Angle Snapping",
                            initialValue: 0,
                            parser: UserInputParsers.DoubleParser,
                            validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.LeftRightRatioDIKey, (_, _) =>
                        new EditableSettingV2<double>(
                            displayName: "L/R Ratio",
                            initialValue: 1,
                            parser: UserInputParsers.DoubleParser,
                            validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.UpDownRatioDIKey, (_, _) =>
                        new EditableSettingV2<double>(
                            displayName: "U/D Ratio",
                            initialValue: 1,
                            parser: UserInputParsers.DoubleParser,
                            validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.SpeedCapDIKey, (_, _) =>
                        new EditableSettingV2<double>(
                            displayName: "Speed Cap",
                            initialValue: 0,
                            parser: UserInputParsers.DoubleParser,
                            validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.OutputSmoothingHalfLifeDIKey, (_, _) =>
                        new EditableSettingV2<double>(
                            displayName: "Output Smoothing Half-Life",
                            initialValue: 0,
                            parser: UserInputParsers.DoubleParser,
                            validator: ModelValueValidators.DefaultDoubleValidator));


            return services.BuildServiceProvider();
        }
    }
}
