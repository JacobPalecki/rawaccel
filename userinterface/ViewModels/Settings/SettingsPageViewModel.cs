using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    private readonly INotificationService? notificationService;

    public SettingsPageViewModel()
    {
        notificationService = App.Services?.GetService<INotificationService>();

        GeneralSettingsViewModel = App.Services!.GetRequiredService<GeneralSettingsViewModel>();
        SupportViewModel = App.Services!.GetRequiredService<SupportViewModel>();
        DevicesSettingsViewModel = App.Services!.GetRequiredService<DevicesSettingsViewModel>();
        MappingsSettingsViewModel = App.Services!.GetRequiredService<MappingsSettingsViewModel>();
        ProfilesSettingsViewModel = App.Services!.GetRequiredService<ProfilesSettingsViewModel>();

        GeneralSettingsViewModel.PropertyChanged += OnGeneralSettingsChanged;
    }

    public GeneralSettingsViewModel GeneralSettingsViewModel { get; }

    public SupportViewModel SupportViewModel { get; }

    public DevicesSettingsViewModel DevicesSettingsViewModel { get; }

    public MappingsSettingsViewModel MappingsSettingsViewModel { get; }

    public ProfilesSettingsViewModel ProfilesSettingsViewModel { get; }

    private void OnGeneralSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Language change notification is now handled in ChangeLanguage method (leave this here for future usage)
    }
}