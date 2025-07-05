using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using static userspace_backend.Data.Profiles.Acceleration;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface IAccelerationModel : IEditableSettingsCollectionSpecific<Acceleration>
    {
        IAnisotropyModel Anisotropy { get; }

        ICoalescionModel Coalescion { get; }

        AccelArgs MapToDriver();
    }

    public class AccelerationModel : EditableSettingsSelector<AccelerationDefinitionType, Acceleration>, IAccelerationModel
    {
        public const string SelectionDIKey = $"{nameof(AccelerationModel)}.{nameof(Selection)}";

        public AccelerationModel(
            IServiceProvider serviceProvider,
            [FromKeyedServices(SelectionDIKey)]IEditableSettingSpecific<AccelerationDefinitionType> definitionType,
            IAnisotropyModel anisotropy,
            ICoalescionModel coalescion)
            : base(serviceProvider, definitionType, [], [anisotropy, coalescion])
        {
            Anisotropy = anisotropy;
            Coalescion = coalescion;
        }

        public IAnisotropyModel Anisotropy { get; set; }

        public ICoalescionModel Coalescion { get; set; }

        public FormulaAccelModel FormulaAccel
        {
            get => SelectionLookup.TryGetValue(AccelerationDefinitionType.Formula, out IEditableSettingsCollectionSpecific<Acceleration> value)
                    ? value as FormulaAccelModel
                    : null;
        }

        public LookupTableDefinitionModel LookupTableAccel
        {
            get => SelectionLookup.TryGetValue(AccelerationDefinitionType.LookupTable, out IEditableSettingsCollectionSpecific<Acceleration> value)
                    ? value as LookupTableDefinitionModel
                    : null;
        }

        public override Acceleration MapToData()
        {
            Acceleration acceleration = base.MapToData();
            acceleration.Anisotropy = Anisotropy.MapToData();
            acceleration.Coalescion = Coalescion.MapToData();
            return acceleration;
        }

        public AccelArgs MapToDriver() => ((IAccelDefinitionModel)Selected)?.MapToDriver() ?? new AccelArgs();
    }
}
