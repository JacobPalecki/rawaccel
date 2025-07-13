using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using userspace_backend.Model;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;

        [ObservableProperty]
        public BE.ProfileModel? currentSelectedProfile;

        private BE.IProfilesModel profilesModel { get; }

        public ProfileListViewModel(BE.IProfilesModel profiles, Action selectionChangeAction)
        {
            profilesModel = profiles;
            SelectionChangeAction = selectionChangeAction;
        }

        public ReadOnlyObservableCollection<BE.IProfileModel> Profiles => profilesModel.Elements;
        public Action SelectionChangeAction { get; }

        partial void OnCurrentSelectedProfileChanged(BE.ProfileModel? value)
        {
            SelectionChangeAction.Invoke();
        }

        public bool TryAddProfile()
        {
            return profilesModel.TryAddNewDefault();
        }

        public void RemoveSelectedProfile()
        {
            if (CurrentSelectedProfile != null)
            {
                _ = profilesModel.TryRemoveElement(CurrentSelectedProfile);
            }
        }
    }
}
