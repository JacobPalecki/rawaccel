using userinterface.Services;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    private readonly INotificationService notificationService;

    public SettingsPageViewModel(
        INotificationService notificationService,
        GeneralSettingsViewModel generalSettingsViewModel,
        SupportViewModel supportViewModel,
        DevicesSettingsViewModel devicesSettingsViewModel,
        MappingsSettingsViewModel mappingsSettingsViewModel,
        ProfilesSettingsViewModel profilesSettingsViewModel)
    {
        this.notificationService = notificationService;
        GeneralSettingsViewModel = generalSettingsViewModel;
        SupportViewModel = supportViewModel;
        DevicesSettingsViewModel = devicesSettingsViewModel;
        MappingsSettingsViewModel = mappingsSettingsViewModel;
        ProfilesSettingsViewModel = profilesSettingsViewModel;

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