using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface ISynchronousAccelerationDefinitionModel : IEditableSettingsCollectionSpecific<SynchronousAccel>
    {
    }

    public class SynchronousAccelerationDefinitionModel : EditableSettingsCollectionV2<SynchronousAccel>, ISynchronousAccelerationDefinitionModel
    {
        public const string SyncSpeedDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(SyncSpeed)}";
        public const string MotivityDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(Motivity)}";
        public const string GammaDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(Gamma)}";
        public const string SmoothnessDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(Smoothness)}";

        public SynchronousAccelerationDefinitionModel(
            [FromKeyedServices(SyncSpeedDIKey)]IEditableSettingSpecific<double> syncSpeed,
            [FromKeyedServices(MotivityDIKey)]IEditableSettingSpecific<double> motivity,
            [FromKeyedServices(GammaDIKey)]IEditableSettingSpecific<double> gamma,
            [FromKeyedServices(SmoothnessDIKey)]IEditableSettingSpecific<double> smoothness)
            : base([syncSpeed, motivity, gamma, smoothness], [])
        {
            SyncSpeed = syncSpeed;
            Motivity = motivity;
            Gamma = gamma;
            Smoothness = smoothness;
        }

        public IEditableSettingSpecific<double> SyncSpeed { get; set; }

        public IEditableSettingSpecific<double> Motivity { get; set; }

        public IEditableSettingSpecific<double> Gamma { get; set; }

        public IEditableSettingSpecific<double> Smoothness { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.synchronous,
                syncSpeed = SyncSpeed.ModelValue,
                motivity = Motivity.ModelValue,
                gamma = Gamma.ModelValue,
                smooth = Smoothness.ModelValue,
            };
        }

        public override SynchronousAccel MapToData()
        {
            return new SynchronousAccel()
            {
                SyncSpeed = SyncSpeed.ModelValue,
                Motivity = Motivity.ModelValue,
                Gamma = Gamma.ModelValue,
                Smoothness = Smoothness.ModelValue,
            };
        }
    }
}
