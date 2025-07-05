using System.Text.Json.Serialization;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;

namespace userspace_backend.Data.Profiles
{
    [JsonDerivedType(typeof(SynchronousAccel))]
    [JsonDerivedType(typeof(LinearAccel))]
    [JsonDerivedType(typeof(ClassicAccel))]
    [JsonDerivedType(typeof(NaturalAccel))]
    [JsonDerivedType(typeof(PowerAccel))]
    [JsonDerivedType(typeof(JumpAccel))]
    [JsonDerivedType(typeof(LookupTableAccel))]
    [JsonDerivedType(typeof(NoAcceleration))]
    public class Acceleration
    {
        public enum AccelerationDefinitionType
        {
            None,
            Formula,
            LookupTable,
        }

        public virtual AccelerationDefinitionType Type { get; init; }

        public Anisotropy Anisotropy { get; set; }

        public Coalescion Coalescion { get; set; }
    }
}
