﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class ProfilesPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        public ProfileViewModel selectedProfileView;

        public ProfilesPageViewModel(BE.ProfilesModel profileModels)
        {
            ProfileModels = profileModels.Profiles;
            ProfileViewModels = new ObservableCollection<ProfileViewModel>();
            UpdatedProfileViewModels();
            SelectedProfileView = ProfileViewModels.FirstOrDefault();
            ProfileListView = new ProfileListViewModel(profileModels, UpdateSelectedProfileView);
            ActiveProfilesListView = new ActiveProfilesListViewModel();
        }

        protected IEnumerable<BE.ProfileModel> ProfileModels { get; }

        protected ObservableCollection<ProfileViewModel> ProfileViewModels { get; }

        public ProfileListViewModel ProfileListView { get; }

        public ActiveProfilesListViewModel ActiveProfilesListView { get; }

        protected void UpdateSelectedProfileView()
        {
            SelectedProfileView = ProfileViewModels.FirstOrDefault(
                p => string.Equals(p.CurrentName, ProfileListView.CurrentSelectedProfile?.CurrentNameForDisplay, StringComparison.InvariantCultureIgnoreCase));
        }

        protected void UpdatedProfileViewModels()
        {
            ProfileViewModels.Clear();

            foreach(BE.ProfileModel profileModelBE in ProfileModels)
            {
                ProfileViewModels.Add(new ProfileViewModel(profileModelBE));
            }
        }
    }
}