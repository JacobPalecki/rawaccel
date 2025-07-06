using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

namespace userspace_backend.Model
{
    public class ProfilesModel : EditableSettingsList<IProfileModel, DATA.Profile>
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

        protected override void TryMapEditableSettingsFromData(IEnumerable<DATA.Profile> data)
        {
            // No editable settings in this class
        }

        protected override void SetElementName(IProfileModel element, string name)
        {
            element.Name.InterfaceValue = name;
        }
    }
}
