using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using userinterface.ViewModels;
using userinterface.Views;
using userspace_backend;
using DATA = userspace_backend.Data;

namespace userinterface;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BackEnd backEnd = new BackEnd(BootstrapBackEnd());
        backEnd.Load();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(backEnd),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    protected Bootstrapper BootstrapBackEnd()
    {
        return new Bootstrapper()
        {
            DevicesToLoad =
            [
                new DATA.Device() { Name = "Superlight 2", DPI = 32000, HWID = @"HID\VID_046D&PID_C54D&MI_00", PollingRate = 1000, DeviceGroup = "Logitech Mice" },
                new DATA.Device() { Name = "Outset AX", DPI = 1200, HWID = @"HID\VID_3057&PID_0001", PollingRate = 1000, DeviceGroup = "Testing" },
                new DATA.Device() { Name = "Razer Viper 8K", DPI = 1200, HWID = @"HID\VID_31E3&PID_1310", PollingRate = 1000, DeviceGroup = "Testing" },
            ],

            ProfilesToLoad =
            [
                new DATA.Profile() { Name = "Favorite", OutputDPI = 1600, YXRatio = 1.333 },
                new DATA.Profile() { Name = "Test", OutputDPI = 1200, YXRatio = 1.0, Hidden = new DATA.Profiles.Hidden() { RotationDegrees = 8, }, },
            ],
        };
    }
}