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

            #region Hidden

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

            #endregion Hidden

            #region Coalescion

            services.AddTransient<ICoalescionModel, CoalescionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                CoalescionModel.InputSmoothingHalfLifeDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Input Smoothing Half-Life",
                        initialValue: 0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                CoalescionModel.ScaleSmoothingHalfLifeDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Scale Smoothing Half-Life",
                        initialValue: 0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion Coalescion

            #region Anisotropy

            services.AddTransient<IAnisotropyModel, AnisotropyModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.DomainXDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Domain X",
                        initialValue: 1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.DomainYDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Domain Y",
                        initialValue: 1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.RangeXDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Range X",
                        initialValue: 1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.RangeYDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Range Y",
                        initialValue: 1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                AnisotropyModel.LPNormDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "LP Norm",
                        initialValue: 2,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<bool>>(
                AnisotropyModel.CombineXYComponentsDIKey, (_, _) =>
                    new EditableSettingV2<bool>(
                        displayName: "Combine X and Y Components",
                        initialValue: false,
                        parser: UserInputParsers.BoolParser,
                        validator: ModelValueValidators.DefaultBoolValidator));

            #endregion Anisotropy

            #region Acceleration

            services.AddTransient<IAccelerationModel, AccelerationModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<AccelerationDefinitionType>>(
                AccelerationModel.SelectionDIKey, (_, _) =>
                    new EditableSettingV2<AccelerationDefinitionType>(
                        displayName: "Definition Type",
                        initialValue: AccelerationDefinitionType.None,
                        parser: UserInputParsers.AccelerationDefinitionTypeParser,
                        validator: ModelValueValidators.DefaultAccelerationTypeValidator));

            #endregion Acceleration

            #region FormulaAccel

            services.AddTransient<IFormulaAccelModel, FormulaAccelModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<AccelerationFormulaType>>(
                FormulaAccelModel.SelectionDIKey, (_, _) =>
                    new EditableSettingV2<AccelerationFormulaType>(
                        displayName: "Formula Type",
                        initialValue: AccelerationFormulaType.Synchronous,
                        parser: UserInputParsers.AccelerationFormulaTypeParser,
                        validator: ModelValueValidators.DefaultAccelerationFormulaTypeValidator,
                        autoUpdateFromInterface: true));
            services.AddKeyedTransient<IEditableSettingSpecific<bool>>(
                FormulaAccelModel.GainDIKey, (_, _) =>
                    new EditableSettingV2<bool>(
                        displayName: "Apply to Gain",
                        initialValue: false,
                        parser: UserInputParsers.BoolParser,
                        validator: ModelValueValidators.DefaultBoolValidator));

            #endregion FormulaAccel

            #region LookupTable

            services.AddTransient<ILookupTableDefinitionModel, LookupTableDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<LookupTableType>>(
                LookupTableDefinitionModel.ApplyAsDIKey, (_, _) =>
                    new EditableSettingV2<LookupTableType>(
                        displayName: "Apply as",
                        initialValue: LookupTableType.Velocity,
                        parser: UserInputParsers.LookupTableTypeParser,
                        validator: ModelValueValidators.DefaultLookupTableTypeValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<LookupTableData>>(
                LookupTableDefinitionModel.DataDIKey, (_, _) =>
                    new EditableSettingV2<LookupTableData>(
                        displayName: "Data",
                        initialValue: new LookupTableData(),
                        parser: UserInputParsers.LookupTableDataParser,
                        validator: ModelValueValidators.DefaultLookupTableDataValidator));

            #endregion LookupTable

            #region NoAccel

            services.AddTransient<INoAccelDefinitionModel, NoAccelDefinitionModel>();

            #endregion NoAccel

            #region SynchronousAccel

            services.AddTransient<ISynchronousAccelerationDefinitionModel, SynchronousAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.SyncSpeedDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Sync Speed",
                        15,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.MotivityDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Motivity",
                        1.4,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.GammaDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Gamma",
                        1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                SynchronousAccelerationDefinitionModel.SmoothnessDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Smoothness",
                        0.5,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion SynchronousAccel

            #region LinearAccel

            services.AddTransient<ILinearAccelerationDefinitionModel, LinearAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                LinearAccelerationDefinitionModel.AccelerationDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Acceleration",
                        0.01,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                LinearAccelerationDefinitionModel.OffsetDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Offset",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                LinearAccelerationDefinitionModel.CapDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Cap",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion LinearAccel

            #region ClassicAccel

            services.AddTransient<IClassicAccelerationDefinitionModel, ClassicAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.AccelerationDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Acceleration",
                        0.01,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.ExponentDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Exponent",
                        2,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.OffsetDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Offset",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.CapDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Cap",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion ClassicAccel

            #region PowerAccel

            services.AddTransient<IPowerAccelerationDefinitionModel, PowerAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.ScaleDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Scale",
                        1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.ExponentDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Exponent",
                        0.05,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.OutputOffsetDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Output Offset",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.CapDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Cap",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion PowerAccel

            #region JumpAccel

            services.AddTransient<IJumpAccelerationDefinitionModel, JumpAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                JumpAccelerationDefinitionModel.SmoothDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Smooth",
                        0.5,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                JumpAccelerationDefinitionModel.InputDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Input",
                        15,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                JumpAccelerationDefinitionModel.OutputDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Output",
                        1.5,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion JumpAccel

            #region NaturalAccel

            services.AddTransient<INaturalAccelerationDefinitionModel, NaturalAccelerationDefinitionModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                NaturalAccelerationDefinitionModel.DecayRateDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Decay Rate",
                        0.1,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                NaturalAccelerationDefinitionModel.InputOffsetDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Input Offset",
                        0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                NaturalAccelerationDefinitionModel.LimitDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Limit",
                        1.5,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion NaturalAccel

            #region Profile

            services.AddTransient<IProfileModel, ProfileModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                ProfileModel.NameDIKey, (_, _) =>
                    new EditableSettingV2<string>(
                        displayName: "Name",
                        "Empty",
                        parser: UserInputParsers.StringParser,
                        // TODO: DI - change to max name length validator
                        validator: ModelValueValidators.DefaultStringValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                ProfileModel.OutputDPIDIKey, (_, _) =>
                    new EditableSettingV2<int>(
                        displayName: "Output DPI",
                        1000,
                        parser: UserInputParsers.IntParser,
                        validator: ModelValueValidators.DefaultIntValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<double>>(
                ProfileModel.YXRatioDIKey, (_, _) =>
                    new EditableSettingV2<double>(
                        displayName: "Y/X Ratio",
                        1.0,
                        parser: UserInputParsers.DoubleParser,
                        validator: ModelValueValidators.DefaultDoubleValidator));

            #endregion Profile

            #region DeviceGroup

            services.AddTransient<IDeviceModel, DeviceModel>();
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                DeviceModel.NameDIKey, (_, _) =>
                    new EditableSettingV2<string>(
                        displayName: "Name",
                        initialValue: "name",
                        parser: UserInputParsers.StringParser,
                        validator: ModelValueValidators.DefaultStringValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                DeviceModel.HardwareIDDIKey, (_, _) =>
                    new EditableSettingV2<string>(
                        displayName: "Hardware ID",
                        initialValue: "hwid",
                        parser: UserInputParsers.StringParser,
                        validator: ModelValueValidators.DefaultStringValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                DeviceModel.DPIDIKey, (_, _) =>
                    new EditableSettingV2<int>(
                        displayName: "DPI",
                        initialValue: 1000,
                        parser: UserInputParsers.IntParser,
                        validator: ModelValueValidators.DefaultIntValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                DeviceModel.PollRateDIKey, (_, _) =>
                    new EditableSettingV2<int>(
                        displayName: "Polling Rate",
                        initialValue: 1000,
                        parser: UserInputParsers.IntParser,
                        validator: ModelValueValidators.DefaultIntValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<bool>>(
                DeviceModel.IgnoreDIKey, (_, _) =>
                    new EditableSettingV2<bool>(
                        displayName: "Ignore",
                        initialValue: false,
                        parser: UserInputParsers.BoolParser,
                        validator: ModelValueValidators.DefaultBoolValidator));
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                DeviceModel.DeviceGroupDIKey, (_, _) =>
                    new EditableSettingV2<string>(
                        displayName: "Device Group",
                        initialValue: "default",
                        parser: UserInputParsers.StringParser,
                        validator: ModelValueValidators.DefaultStringValidator));

            #endregion DeviceGroup

            return services.BuildServiceProvider();
        }
    }
}
