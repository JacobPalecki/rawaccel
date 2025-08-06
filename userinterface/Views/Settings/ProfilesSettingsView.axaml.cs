using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Microsoft.Extensions.DependencyInjection;
using System;
using userinterface.Services;
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

        var localizationService = App.Services?.GetRequiredService<LocalizationService>() ?? throw new InvalidOperationException("LocalizationService not available");
        var settingsFieldViewModel = new DualColumnLabelFieldViewModel(localizationService);
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