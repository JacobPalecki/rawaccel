using Microsoft.Extensions.DependencyInjection;
using System;
using userspace_backend.Model;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.AccelDefinitions.Formula;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using static userspace_backend.Data.Profiles.Accel.FormulaAccel;
using static userspace_backend.Data.Profiles.Accel.LookupTableAccel;
using static userspace_backend.Data.Profiles.Acceleration;

namespace userspace_backend
{
    public static class BackEndComposer
    {
        public static IServiceProvider Compose(IServiceCollection services)
        {
            services.AddSingleton<ISystemDevicesRetriever, SystemDevicesRetriever>();
            services.AddSingleton<ISystemDevicesProvider, SystemDevicesProvider>();

            #region Parsers

            services.AddSingleton<IUserInputParser<string>, StringParser>();
            services.AddSingleton<IUserInputParser<int>, IntParser>();
            services.AddSingleton<IUserInputParser<double>, DoubleParser>();
            services.AddSingleton<IUserInputParser<bool>, BoolParser>();
            services.AddSingleton<IUserInputParser<AccelerationDefinitionType>, AccelerationDefinitionTypeParser>();
            services.AddSingleton<IUserInputParser<AccelerationFormulaType>, AccelerationFormulaTypeParser>();
            services.AddSingleton<IUserInputParser<LookupTableType>, LookupTableTypeParser>();
            services.AddSingleton<IUserInputParser<LookupTableData>, LookupTableDataParser>();

            #endregion Parsers

            #region Validators

            services.AddSingleton<IModelValueValidator<int>, DefaultModelValueValidator<int>>();
            services.AddSingleton<IModelValueValidator<double>, DefaultModelValueValidator<double>>();
            services.AddSingleton<IModelValueValidator<string>, DefaultModelValueValidator<string>>();
            services.AddSingleton<IModelValueValidator<bool>, DefaultModelValueValidator<bool>>();
            services.AddSingleton<IModelValueValidator<AccelerationDefinitionType>, DefaultModelValueValidator<AccelerationDefinitionType>>();
            services.AddSingleton<IModelValueValidator<AccelerationFormulaType>, DefaultModelValueValidator<AccelerationFormulaType>>();
            services.AddSingleton<IModelValueValidator<LookupTableType>, DefaultModelValueValidator<LookupTableType>>();
            services.AddSingleton<IModelValueValidator<LookupTableData>, DefaultModelValueValidator<LookupTableData>>();

            services.AddKeyedSingleton<IModelValueValidator<string>, DefaultModelValueValidator<string>>(
                DefaultModelValueValidator<string>.AllChangeInvalidDIKey);

            #endregion Validators

            #region Hidden

            services.AddTransient<IHiddenModel, HiddenModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.RotationDegreesDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                    displayName: "Rotation",
                    initialValue: 0,
                    parser: services.GetRequiredService<IUserInputParser<double>>(),
                    validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.AngleSnappingDegreesDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Angle Snapping",
                        initialValue: 0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.LeftRightRatioDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "L/R Ratio",
                        initialValue: 1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.UpDownRatioDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "U/D Ratio",
                        initialValue: 1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.SpeedCapDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Speed Cap",
                        initialValue: 0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                HiddenModel.OutputSmoothingHalfLifeDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Output Smoothing Half-Life",
                        initialValue: 0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion Hidden

            #region Coalescion

            services.AddTransient<ICoalescionModel, CoalescionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                CoalescionModel.InputSmoothingHalfLifeDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Input Smoothing Half-Life",
                        initialValue: 0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                CoalescionModel.ScaleSmoothingHalfLifeDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Scale Smoothing Half-Life",
                        initialValue: 0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion Coalescion

            #region Anisotropy

            services.AddTransient<IAnisotropyModel, AnisotropyModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.DomainXDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Domain X",
                        initialValue: 1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.DomainYDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Domain Y",
                        initialValue: 1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.RangeXDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Range X",
                        initialValue: 1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.RangeYDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Range Y",
                        initialValue: 1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.LPNormDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "LP Norm",
                        initialValue: 2,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<bool>>(
                AnisotropyModel.CombineXYComponentsDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<bool>(
                        displayName: "Combine X and Y Components",
                        initialValue: false,
                        parser: services.GetRequiredService<IUserInputParser<bool>>(),
                        validator: services.GetRequiredService<IModelValueValidator<bool>>()));

            #endregion Anisotropy

            #region Acceleration

            services.AddTransient<IAccelerationModel, AccelerationModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<AccelerationDefinitionType>>(
                AccelerationModel.SelectionDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<AccelerationDefinitionType>(
                        displayName: "Definition Type",
                        initialValue: AccelerationDefinitionType.None,
                        parser: services.GetRequiredService<IUserInputParser<AccelerationDefinitionType>>(),
                        validator: services.GetRequiredService<IModelValueValidator<AccelerationDefinitionType>>()));

            #endregion Acceleration

            #region FormulaAccel

            services.AddTransient<IFormulaAccelModel, FormulaAccelModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<AccelerationFormulaType>>(
                FormulaAccelModel.SelectionDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<AccelerationFormulaType>(
                        displayName: "Formula Type",
                        initialValue: AccelerationFormulaType.Synchronous,
                        parser: services.GetRequiredService<IUserInputParser<AccelerationFormulaType>>(),
                        validator: services.GetRequiredService<IModelValueValidator<AccelerationFormulaType>>(),
                        autoUpdateFromInterface: true));
            services.AddKeyedTransient<IEditableSettingSpecific<bool>>(
                FormulaAccelModel.GainDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<bool>(
                        displayName: "Apply to Gain",
                        initialValue: false,
                        parser: services.GetRequiredService<IUserInputParser<bool>>(),
                        validator: services.GetRequiredService<IModelValueValidator<bool>>()));

            #endregion FormulaAccel

            #region LookupTable

            services.AddTransient<ILookupTableDefinitionModel, LookupTableDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<LookupTableType>>(
                LookupTableDefinitionModel.ApplyAsDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<LookupTableType>(
                        displayName: "Apply as",
                        initialValue: LookupTableType.Velocity,
                        parser: services.GetRequiredService<IUserInputParser<LookupTableType>>(),
                        validator: services.GetRequiredService<IModelValueValidator<LookupTableType>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<LookupTableData>>(
                LookupTableDefinitionModel.DataDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<LookupTableData>(
                        displayName: "Data",
                        initialValue: new LookupTableData(),
                        parser: services.GetRequiredService<IUserInputParser<LookupTableData>>(),
                        validator: services.GetRequiredService<IModelValueValidator<LookupTableData>>()));

            #endregion LookupTable

            #region NoAccel

            services.AddTransient<INoAccelDefinitionModel, NoAccelDefinitionModel>();

            #endregion NoAccel

            #region SynchronousAccel

            services.AddTransient<ISynchronousAccelerationDefinitionModel, SynchronousAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.SyncSpeedDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Sync Speed",
                        15,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.MotivityDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Motivity",
                        1.4,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.GammaDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Gamma",
                        1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.SmoothnessDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Smoothness",
                        0.5,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion SynchronousAccel

            #region LinearAccel

            services.AddTransient<ILinearAccelerationDefinitionModel, LinearAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                LinearAccelerationDefinitionModel.AccelerationDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Acceleration",
                        0.01,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                LinearAccelerationDefinitionModel.OffsetDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Offset",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                LinearAccelerationDefinitionModel.CapDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Cap",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion LinearAccel

            #region ClassicAccel

            services.AddTransient<IClassicAccelerationDefinitionModel, ClassicAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.AccelerationDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Acceleration",
                        0.01,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.ExponentDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Exponent",
                        2,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.OffsetDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Offset",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.CapDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Cap",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion ClassicAccel

            #region PowerAccel

            services.AddTransient<IPowerAccelerationDefinitionModel, PowerAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.ScaleDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Scale",
                        1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.ExponentDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Exponent",
                        0.05,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.OutputOffsetDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Output Offset",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.CapDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Cap",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion PowerAccel

            #region JumpAccel

            services.AddTransient<IJumpAccelerationDefinitionModel, JumpAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                JumpAccelerationDefinitionModel.SmoothDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Smooth",
                        0.5,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                JumpAccelerationDefinitionModel.InputDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Input",
                        15,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                JumpAccelerationDefinitionModel.OutputDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Output",
                        1.5,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion JumpAccel

            #region NaturalAccel

            services.AddTransient<INaturalAccelerationDefinitionModel, NaturalAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                NaturalAccelerationDefinitionModel.DecayRateDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Decay Rate",
                        0.1,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                NaturalAccelerationDefinitionModel.InputOffsetDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Input Offset",
                        0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                NaturalAccelerationDefinitionModel.LimitDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Limit",
                        1.5,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion NaturalAccel

            #region Profile

            services.AddTransient<IProfileModel, ProfileModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                ProfileModel.NameDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<string>(
                        displayName: "Name",
                        "Empty",
                        parser: services.GetRequiredService<IUserInputParser<string>>(),
                        // TODO: DI - change to max name length validator
                        validator: services.GetRequiredService<IModelValueValidator<string>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                ProfileModel.OutputDPIDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<int>(
                        displayName: "Output DPI",
                        1000,
                        parser: services.GetRequiredService<IUserInputParser<int>>(),
                        validator: services.GetRequiredService<IModelValueValidator<int>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ProfileModel.YXRatioDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<double>(
                        displayName: "Y/X Ratio",
                        1.0,
                        parser: services.GetRequiredService<IUserInputParser<double>>(),
                        validator: services.GetRequiredService<IModelValueValidator<double>>()));

            #endregion Profile

            #region DeviceGroup

            services.AddTransient<IDeviceModel, DeviceModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                DeviceModel.NameDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<string>(
                        displayName: "Name",
                        initialValue: "name",
                        parser: services.GetRequiredService<IUserInputParser<string>>(),
                        validator: services.GetRequiredService<IModelValueValidator<string>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                DeviceModel.HardwareIDDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<string>(
                        displayName: "Hardware ID",
                        initialValue: "hwid",
                        parser: services.GetRequiredService<IUserInputParser<string>>(),
                        validator: services.GetRequiredService<IModelValueValidator<string>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                DeviceModel.DPIDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<int>(
                        displayName: "DPI",
                        initialValue: 1000,
                        parser: services.GetRequiredService<IUserInputParser<int>>(),
                        validator: services.GetRequiredService<IModelValueValidator<int>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                DeviceModel.PollRateDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<int>(
                        displayName: "Polling Rate",
                        initialValue: 1000,
                        parser: services.GetRequiredService<IUserInputParser<int>>(),
                        validator: services.GetRequiredService<IModelValueValidator<int>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<bool>>(
                DeviceModel.IgnoreDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<bool>(
                        displayName: "Ignore",
                        initialValue: false,
                        parser: services.GetRequiredService<IUserInputParser<bool>>(),
                        validator: services.GetRequiredService<IModelValueValidator<bool>>()));
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                DeviceModel.DeviceGroupDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<string>(
                        displayName: "Device Group",
                        initialValue: "default",
                        parser: services.GetRequiredService<IUserInputParser<string>>(),
                        validator: services.GetRequiredService<IModelValueValidator<string>>()));

            #endregion DeviceGroup

            #region Mapping

            services.AddTransient<IMappingModel, MappingModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                MappingModel.NameDIKey, (IServiceProvider services, object? key) =>
                    new EditableSettingV2<string>(
                        displayName: "Name",
                        initialValue: "name",
                        parser: services.GetRequiredService<IUserInputParser<string>>(),
                        validator: services.GetRequiredKeyedService<IModelValueValidator<string>>(MappingModel.NameDIKey)));

            #endregion Mapping

            return services.BuildServiceProvider();
        }
    }
}
