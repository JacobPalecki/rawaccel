using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;

namespace userinterface.ViewModels.Settings;

public class ProfilesSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService settingsService;

    public ProfilesSettingsViewModel()
    {
        settingsService = App.Services!.GetRequiredService<ISettingsService>();
    }

    public bool ForceProfilesListOpen
    {
        get => settingsService.ForceProfilesListOpen;
        set
        {
            settingsService.ForceProfilesListOpen = value;
            OnPropertyChanged();
        }
    }
}