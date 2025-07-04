using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

namespace userspace_backend.Model
{
    public class ProfilesModel : EditableSettingsList<ProfileModel, DATA.Profile>
    {
        // TODO: DI - hand default profile to profiles model
        public static readonly ProfileModel DefaultProfile = new ProfileModel(
            GenerateNewDefaultProfile("Default"), ModelValueValidators.AllChangesInvalidStringValidator);

        public ProfilesModel(IServiceProvider serviceProvider)
            : base(serviceProvider, [], [])
        {
        }


        protected override string DefaultNameTemplate => "Profile";

        protected override string GetNameFromElement(ProfileModel element)
        {
            return element.Name.ModelValue;
        }

        protected override void SetElementName(ProfileModel element, string name)
        {
            element.Name.InterfaceValue = name;
        }
    }
}
