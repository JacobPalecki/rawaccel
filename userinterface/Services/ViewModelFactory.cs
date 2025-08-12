using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using userinterface.ViewModels.Mapping;
using userinterface.ViewModels.Profile;
using BE = userspace_backend.Model;

namespace userinterface.Services
{
    public class ViewModelFactory : IViewModelFactory
    {

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private IServiceProvider ServiceProvider { get; }

        public ProfileViewModel CreateProfileViewModel(BE.ProfileModel profileModel)
        {
            var stopwatch = Stopwatch.StartNew();

            var viewModel = ServiceProvider.GetRequiredService<ProfileViewModel>();

            stopwatch.Restart();
            viewModel.Initialize(profileModel);

            return viewModel;
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(BE.ProfileModel profileModel)
        {
            var stopwatch = Stopwatch.StartNew();

            var viewModel = ServiceProvider.GetRequiredService<ProfileSettingsViewModel>();

            stopwatch.Restart();
            viewModel.Initialize(profileModel);

            return viewModel;
        }

        public ProfileChartViewModel CreateProfileChartViewModel(BE.ProfileModel profileModel)
        {
            var stopwatch = Stopwatch.StartNew();

            var viewModel = ServiceProvider.GetRequiredService<ProfileChartViewModel>();

            stopwatch.Restart();
            viewModel.Initialize(profileModel);

            return viewModel;
        }


        public MappingViewModel CreateMappingViewModel(BE.MappingModel mappingModel, BE.MappingsModel mappingsModel, bool isActive, Action<MappingViewModel> onActivationRequested)
        {
            var viewModel = ServiceProvider.GetRequiredService<MappingViewModel>();
            var modalService = ServiceProvider.GetRequiredService<IModalService>();
            viewModel.Initialize(mappingModel, mappingsModel, modalService, isActive, onActivationRequested);
            return viewModel;
        }

    }
}