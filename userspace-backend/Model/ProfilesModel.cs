using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

namespace userspace_backend.Model
{
    public interface IProfilesModel : IEditableSettingsList<IProfileModel, DATA.Profile>
    {
    }

    public class ProfilesModel : EditableSettingsList<IProfileModel, DATA.Profile>, IProfilesModel
    {
        // TODO: DI - hand default profile to profiles model
        // public static readonly ProfileModel DefaultProfile = new ProfileModel(
           // GenerateNewDefaultProfile("Default"), ModelValueValidators.AllChangesInvalidStringValidator);

        public ProfilesModel(IServiceProvider serviceProvider)
            : base(serviceProvider, [], [])
        {
        }


        protected override string DefaultNameTemplate => "Profile";

        protected override string GetNameFromElement(IProfileModel element)
        {
            return element.Name.ModelValue;
        }

        protected override bool TryMapEditableSettingsFromData(IEnumerable<DATA.Profile> data)
        {
            return true;
        }

        protected override void SetElementName(IProfileModel element, string name)
        {
            element.Name.InterfaceValue = name;
        }

        protected override string GetNameFromData(DATA.Profile data)
        {
            return data.Name;
        }
    }
}
