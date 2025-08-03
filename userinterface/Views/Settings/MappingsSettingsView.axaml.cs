using Avalonia.Controls;
using userinterface.ViewModels.Settings;

namespace userinterface.Views.Settings;

public partial class MappingsSettingsView : UserControl
{
    public MappingsSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MappingsSettingsViewModel mappingsSettingsViewModel)
        {
        }
    }
}