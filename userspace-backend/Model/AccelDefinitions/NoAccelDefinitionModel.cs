using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface INoAccelDefinitionModel : IAccelDefinitionModelSpecific<NoAcceleration>
    {
    }

    public class NoAccelDefinitionModel : EditableSettingsCollectionV2<NoAcceleration>, INoAccelDefinitionModel
    {
        public NoAccelDefinitionModel()
            : base([], [])
        {
            NoAcceleration = new NoAcceleration();
        }

        public NoAcceleration NoAcceleration { get; protected set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs()
            {
                mode = AccelMode.noaccel,
            };
        }

        public override NoAcceleration MapToData()
        {
            return NoAcceleration;
        }
    }
}
