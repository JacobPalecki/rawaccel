using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Settings;
using userinterface.Views;
using userspace_backend;
using userspace_backend.Hardware;
// using userspace_backend.Logging;
using DATA = userspace_backend.Data;

namespace userinterface;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public static bool IsAppLoaded { get; private set; }
    public static event Action? AppLoadCompleted;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<INotificationService>(provider =>
            new NotificationService(provider.GetRequiredService<LocalizationService>(), provider.GetRequiredService<ISettingsService>()));
        services.AddSingleton<IModalService>(provider =>
            new ModalService(provider.GetRequiredService<LocalizationService>(), provider.GetRequiredService<ISettingsService>()));
        services.AddSingleton<IThemeService>(provider =>
            new ThemeService(provider.GetRequiredService<ISettingsService>()));
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<FrameTimerService>();
        services.AddSingleton<PreviewChartRenderer>();
        services.AddSingleton<IMouseTracker, MouseTracker>();
        services.AddSingleton<IAnimationStateService, AnimationStateService>();

        // Register logging service
        services.AddSingleton<userspace_backend.Logging.ILoggingService>(provider =>
        {
            var bootstrapper = BootstrapBackEnd();
            var settings = bootstrapper.LoadSettings();
            var loggingConfig = settings?.LoggingConfiguration ?? new userspace_backend.Logging.LoggingConfiguration();
            return new userspace_backend.Logging.LoggingService(loggingConfig);
        });

        // Register backend services
        services.AddSingleton<IDeviceInfoProvider, DeviceInfoProvider>();
        services.AddSingleton<Bootstrapper>(provider => BootstrapBackEnd());
        services.AddSingleton<BackEnd>(provider =>
        {
            var bootstrapper = provider.GetRequiredService<Bootstrapper>();
            var deviceInfoProvider = provider.GetRequiredService<IDeviceInfoProvider>();
            var loggingService = provider.GetRequiredService<userspace_backend.Logging.ILoggingService>();
            var backEnd = new BackEnd(bootstrapper, deviceInfoProvider, loggingService);
            backEnd.Load();
            return backEnd;
        });

        // Register settings service that depends on backend
        services.AddSingleton<ISettingsService, SettingsService>();

        RegisterViewModels(services);

        Services = services.BuildServiceProvider();

        // Apply settings from backend after services are built
        ApplyStartupSettings();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            var mainWindow = new MainWindow()
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };

            // Set up the toast control (was already created in MainWindow.axaml)

            desktop.MainWindow = mainWindow;

            // Mark app as loaded after MainWindow is created and assigned
            IsAppLoaded = true;
            AppLoadCompleted?.Invoke();

            // Preload libraries that cause first-page stutter
            _ = PreloadLibrariesAsync();

#if DEBUG
            desktop.MainWindow.AttachDevTools();
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterViewModels(IServiceCollection services)
    {
        // Main ViewModels
        services.AddSingleton<MainWindowViewModel>(provider =>
            new MainWindowViewModel(
                provider.GetRequiredService<BackEnd>(),
                provider.GetRequiredService<IThemeService>(),
                provider.GetRequiredService<ISettingsService>(),
                provider.GetRequiredService<FrameTimerService>(),
                provider.GetRequiredService<INotificationService>()));
        services.AddSingleton<ToastContainerViewModel>();

        // Device ViewModels
        services.AddTransient<ViewModels.Device.DevicesPageViewModel>(provider =>
            new ViewModels.Device.DevicesPageViewModel(
                provider.GetRequiredService<BackEnd>(),
                provider.GetRequiredService<IModalService>(),
                provider.GetRequiredService<LocalizationService>()));
        services.AddTransient<ViewModels.Device.DevicesListViewModel>(provider =>
            new ViewModels.Device.DevicesListViewModel(
                provider.GetRequiredService<BackEnd>().Devices,
                provider.GetRequiredService<IModalService>(),
                provider.GetRequiredService<LocalizationService>()));
        services.AddTransient<ViewModels.Device.DeviceGroupsViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupSelectorViewModel>();
        services.AddTransient<ViewModels.Device.DeviceViewModel>();

        // Profile ViewModels
        services.AddTransient<ViewModels.Profile.ProfilesPageViewModel>();
        services.AddSingleton<ViewModels.Profile.ProfileListViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileSettingsViewModel>(provider =>
            new ViewModels.Profile.ProfileSettingsViewModel(
                provider.GetRequiredService<INotificationService>(),
                provider.GetRequiredService<LocalizationService>(),
                provider.GetRequiredService<IModalService>()));
        services.AddTransient<ViewModels.Profile.ProfileChartViewModel>();
        services.AddTransient<ViewModels.Profile.AccelerationFormulaSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.AccelerationLUTSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.AccelerationProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.AnisotropyProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.CoalescionProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.HiddenProfileSettingsViewModel>();

        // Mapping ViewModels
        services.AddTransient<ViewModels.Mapping.MappingsPageViewModel>();
        services.AddTransient<ViewModels.Mapping.MappingViewModel>();
        services.AddTransient<ViewModels.Mapping.MappingListElementViewModel>();

        // Settings ViewModels
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<ViewModels.Settings.GeneralSettingsViewModel>();
        services.AddTransient<ViewModels.Settings.SupportViewModel>();
        services.AddTransient<ViewModels.Settings.DevicesSettingsViewModel>();
        services.AddTransient<ViewModels.Settings.MappingsSettingsViewModel>();
        services.AddTransient<ViewModels.Settings.ProfilesSettingsViewModel>();

        // Control ViewModels
        services.AddTransient<ViewModels.Controls.DualColumnLabelFieldViewModel>(provider =>
            new ViewModels.Controls.DualColumnLabelFieldViewModel(
                provider.GetRequiredService<LocalizationService>()));
        services.AddTransient<ViewModels.Controls.EditableFieldViewModel>();
    }

    protected static Bootstrapper BootstrapBackEnd()
    {
        return new Bootstrapper()
        {
            BackEndLoader = new BackEndLoader(System.AppDomain.CurrentDomain.BaseDirectory),
            DevicesToLoad =
            [
                new DATA.Device() { Name = "Superlight 2", DPI = 32000, HWID = @"HID\VID_046D&PID_C54D&MI_00", PollingRate = 1000, DeviceGroup = "Logitech Mice" },
                new DATA.Device() { Name = "Outset AX", DPI = 1200, HWID = @"HID\VID_3057&PID_0001", PollingRate = 1000, DeviceGroup = "Testing" },
                new DATA.Device() { Name = "Razer Viper 8K", DPI = 1200, HWID = @"HID\VID_31E3&PID_1310", PollingRate = 1000, DeviceGroup = "Testing" },
            ],
            ProfilesToLoad =
            [
                new DATA.Profile()
                {
                    Name = "Favorite", OutputDPI = 1600,
                    YXRatio = 1.333,
                    Acceleration = new DATA.Profiles.Accel.Formula.SynchronousAccel()
                    {
                        SyncSpeed = 25.85,
                        Motivity = 1.1333,
                        Gamma = 0.063,
                        Smoothness = 0.5,
                        Anisotropy = new DATA.Profiles.Anisotropy()
                        {
                            CombineXYComponents = false,
                            Domain = new DATA.Profiles.Vector2() { X = 1, Y = 4 },
                            Range = new DATA.Profiles.Vector2() { X = 1, Y = 1 },
                            LPNorm = 2,
                        },
                        Coalescion = new DATA.Profiles.Coalescion()
                        {
                            InputSmoothingHalfLife = 10,
                            ScaleSmoothingHalfLife = 0,
                        },
                    },
                    Hidden = new DATA.Profiles.Hidden() { RotationDegrees = 8, },
                },
                new DATA.Profile() { Name = "Test", OutputDPI = 1200, YXRatio = 1.0 },
                new DATA.Profile() { Name = "SpecificGame", OutputDPI = 3200, YXRatio = 1.333 },
            ],
            MappingsToLoad = new DATA.MappingSet()
            {
                Mappings =
                [
                    new DATA.Mapping() {
                        Name = "Usual",
                        GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping()
                        {
                            { "Logitech Mice", "Favorite" },
                            { "Testing", "Default" },
                            { "Default", "Default" },
                        },
                    },
                    new DATA.Mapping() {
                        Name = "ForSpecificGame",
                        GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping()
                        {
                            { "Logitech Mice", "SpecificGame" },
                            { "Testing", "SpecificGame" },
                        },
                    },
                ],
            },
            SettingsToLoad = new DATA.Settings()
            {
                ShowToastNotifications = true,
                ShowConfirmModals = true,
                Theme = "Dark",
                Language = "en-US"
            },
        };
    }


    public static void OpenBugReportUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/RawAccelOfficial/rawaccel/issues",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Services?.GetService<userspace_backend.Logging.ILoggingService>()?.LogError(userspace_backend.Logging.LogSource.System, ex, "Failed to open bug report URL");
        }
    }

    public static void OpenDiscordUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/7pQh8zH",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Services?.GetService<userspace_backend.Logging.ILoggingService>()?.LogError(userspace_backend.Logging.LogSource.System, ex, "Failed to open Discord URL");
        }
    }

    /* 
     * This was originally intended to preload libraries that cause stutter 
     * but it seems to not have much effect. Will leave it here for now.
     * 
     * Could also do these
     * System.Runtime.Intrinsics
     * System.Text.Json
     * System.Text.Encodings.Web
     * System.Text.Encoding.Extensions
     * System.IO.Pipelines
    */
    private async Task PreloadLibrariesAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                try
                {
                    _ = typeof(LiveChartsCore.SkiaSharpView.Avalonia.CartesianChart).Assembly;

                    _ = typeof(SkiaSharp.HarfBuzz.SKShaper).Assembly;

                    _ = typeof(SkiaSharp.SKCanvas).Assembly;

                    _ = typeof(LiveChartsCore.CartesianChart<>).Assembly;

                    _ = typeof(Avalonia.Controls.ItemsRepeater).Assembly;

                    _ = typeof(System.Security.Cryptography.MD5).Assembly;

                    _ = typeof(Avalonia.Media.Imaging.Bitmap).Assembly;
                }
                catch (Exception ex)
                {
                    Services?.GetService<userspace_backend.Logging.ILoggingService>()?.LogError(userspace_backend.Logging.LogSource.System, ex, "Failed to preload library during async initialization");
                }
            });

        }
        catch (Exception ex)
        {
            Services?.GetService<userspace_backend.Logging.ILoggingService>()?.LogError(userspace_backend.Logging.LogSource.System, ex, "Failed during PreloadLibrariesAsync");
        }
    }

    private void ApplyStartupSettings()
    {
        try
        {
            var settingsService = Services?.GetService<ISettingsService>();
            var localizationService = Services?.GetService<LocalizationService>();
            var themeService = Services?.GetService<IThemeService>();

            if (settingsService != null && localizationService != null)
            {
                // Apply language setting from backend
                localizationService.ChangeLanguage(settingsService.Language);
            }

            if (themeService != null)
            {
                // Apply theme setting from backend
                themeService.ApplyThemeFromSettings();
            }
        }
        catch (Exception ex)
        {
            Services?.GetService<userspace_backend.Logging.ILoggingService>()?.LogError(userspace_backend.Logging.LogSource.System, ex, "Failed to apply startup settings");
        }
    }
}