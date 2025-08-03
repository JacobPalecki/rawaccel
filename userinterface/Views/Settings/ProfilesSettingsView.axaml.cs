using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Settings;
using userinterface.Views.Controls;

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
            SetupSettings(profilesSettingsViewModel);
        }
    }

    private void SetupSettings(ProfilesSettingsViewModel profilesSettingsViewModel)
    {
        SettingsStackPanel.Children.Clear();

        var settingsFieldViewModel = new DualColumnLabelFieldViewModel();
        var settingsField = new DualColumnLabelFieldView(settingsFieldViewModel);

        var forceListOpenCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = profilesSettingsViewModel
        };

        forceListOpenCheckBox.Bind(CheckBox.IsCheckedProperty, new Binding("ForceProfilesListOpen"));

        settingsFieldViewModel.AddField("ForceProfilesListOpen", forceListOpenCheckBox);

        SettingsStackPanel.Children.Add(settingsField);
    }
}