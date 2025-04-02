using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface IAccelDefinitionModel : IEditableSettingsCollectionSpecific<Acceleration>
    {
        AccelArgs MapToDriver();
    }

    public abstract class AccelDefinitionModel<T> : EditableSettingsCollection<Acceleration>, IAccelDefinitionModel where T : Acceleration
    {
        protected AccelDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        protected override void InitEditableSettingsAndCollections(Acceleration dataObject)
        {
            T dataAccel = dataObject as T ?? GenerateDefaultDataObject();
            InitSpecificSettingsAndCollections(dataAccel);
        }

        protected abstract void InitSpecificSettingsAndCollections(T dataObject);

        protected abstract T GenerateDefaultDataObject();

        public abstract AccelArgs MapToDriver();
    }
}
