using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface IAccelDefinitionModel : IEditableSettingsCollectionV2
    {
        AccelArgs MapToDriver();
    }

    public interface IAccelDefinitionModelSpecific<T> : IAccelDefinitionModel, IEditableSettingsCollectionSpecific<T> where T : Acceleration
    {
    }    
}
