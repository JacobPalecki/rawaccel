using Avalonia.Controls;
using userinterface.ViewModels.Settings;

namespace userinterface.Views.Settings;

public partial class DevicesSettingsView : UserControl
{
    public DevicesSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is DevicesSettingsViewModel devicesSettingsViewModel)
        {
        }
    }
}