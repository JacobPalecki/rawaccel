using Avalonia.Controls;
using userinterface.ViewModels.Settings;

namespace userinterface.Views.Settings;

public partial class ProfilesSettingsView : UserControl
{
    public ProfilesSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ProfilesSettingsViewModel profilesSettingsViewModel)
        {
        }
    }
}