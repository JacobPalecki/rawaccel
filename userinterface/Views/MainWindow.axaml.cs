using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using userinterface.Converters;
using userinterface.Extensions;
using userinterface.Models;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.Views.Controls;
using userspace_backend.Hardware;

namespace userinterface.Views;

public partial class MainWindow : Window
{
    private Button? ApplyButtonControl;
    private ProgressBar? LoadingProgressBar;

    public MainWindow()
    {
        InitializeComponent();

        InitializeControls();
        UpdateThemeToggleButton();
        UpdateSelectedButton(NavigationPage.Devices);
        
        // Subscribe to theme changes
        ThemeService.ThemeChanged += OnThemeChanged;
        
        // Set up mouse tracking when window is loaded
        this.Opened += OnWindowOpened;
    }
    
    private void OnWindowOpened(object? sender, EventArgs e)
    {
        try
        {
            if (TryGetPlatformHandle()?.Handle is IntPtr hwnd && hwnd != IntPtr.Zero)
            {
                MouseTracker.SetWindowHandle(hwnd);
                SetupWindowProcHook(hwnd);
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    private const int WM_INPUT = 0x00FF;
    private IntPtr originalWndProc = IntPtr.Zero;
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    
    private const int GWL_WNDPROC = -4;
    
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate? wndProcDelegate;
    
    private void SetupWindowProcHook(IntPtr hwnd)
    {
        try
        {
            wndProcDelegate = new WndProcDelegate(WindowProc);
            IntPtr newWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
            originalWndProc = SetWindowLongPtr(hwnd, GWL_WNDPROC, newWndProc);
        }
        catch (Exception ex)
        {
        }
    }
    
    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == WM_INPUT)
            {
                MouseTracker.ProcessRawInput(lParam);
            }
        }
        catch (Exception ex)
        {
        }
        
        return CallWindowProc(originalWndProc, hWnd, msg, wParam, lParam);
    }

    private INotificationService NotificationService =>
        App.Services!.GetRequiredService<INotificationService>();
    
    private ISettingsService SettingsService =>
        App.Services!.GetRequiredService<ISettingsService>();
    
    private IThemeService ThemeService =>
        App.Services!.GetRequiredService<IThemeService>();
    
    private IMouseTracker MouseTracker =>
        App.Services!.GetRequiredService<IMouseTracker>();

    private void InitializeControls()
    {
        ApplyButtonControl = this.FindControl<Button>("ApplyButton");
        LoadingProgressBar = this.FindControl<ProgressBar>("LoadingProgress");

        if (ApplyButtonControl != null)
        {
            ApplyButtonControl.Click += ApplyButtonHandler;
        }

        if (this.TryFindControl<Button>("SettingsButton", out var settingsButton))
        {
            settingsButton.Click += OnSettingsClick;
        }

        if (this.TryFindControl<ToggleButton>("ThemeToggleButton", out var themeToggleButton))
        {
            themeToggleButton.Click += ToggleTheme;
        }

        if (this.TryFindControl<Button>("DevicesButton", out var devicesButton))
        {
            devicesButton.Click += OnNavigationClick;
        }

        if (this.TryFindControl<Button>("MappingsButton", out var mappingsButton))
        {
            mappingsButton.Click += OnNavigationClick;
        }

        if (this.TryFindControl<Button>("ProfilesButton", out var profilesButton))
        {
            profilesButton.Click += OnNavigationClick;
        }
    }

    public async void ApplyButtonHandler(object? sender, RoutedEventArgs args)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (ApplyButtonControl != null)
            {
                ApplyButtonControl.IsEnabled = false;
            }
            if (LoadingProgressBar != null)
            {
                LoadingProgressBar.IsVisible = true;
            }

            bool applySuccess = false;
            if (viewModel.ApplyCommand.CanExecute(null))
            {
                applySuccess = viewModel.Apply();
            }

            await Task.Delay(1000);

            if (LoadingProgressBar != null)
            {
                LoadingProgressBar.IsVisible = false;
            }

            if (applySuccess)
            {
                NotificationService.ShowSuccessToast("MainWindowSettingsAppliedSuccess");
            }

            if (ApplyButtonControl != null)
            {
                ApplyButtonControl.IsEnabled = true;
            }
        }
    }

    public async void OnNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is string pageNameString &&
            Enum.TryParse<NavigationPage>(pageNameString, out var page) &&
            DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.SelectPageAsync(page);
            UpdateSelectedButton(page);
        }
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.SelectPageAsync(NavigationPage.Settings);
            UpdateSelectedButton(NavigationPage.Settings);
        }
    }

    private void ToggleTheme(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (viewModel.ToggleThemeCommand.CanExecute(null))
            {
                viewModel.ToggleThemeCommand.Execute(null);
            }
        }
        UpdateThemeToggleButton();
    }

    private void UpdateSelectedButton(NavigationPage selectedPage)
    {
        this.TryFindControl<Button>("DevicesButton", out var devicesButton);
        this.TryFindControl<Button>("MappingsButton", out var mappingsButton);
        this.TryFindControl<Button>("ProfilesButton", out var profilesButton);
        this.TryFindControl<Button>("SettingsButton", out var settingsButton);

        devicesButton?.Classes.Remove("Selected");
        mappingsButton?.Classes.Remove("Selected");
        profilesButton?.Classes.Remove("Selected");
        settingsButton?.Classes.Remove("Selected");

        switch (selectedPage)
        {
            case NavigationPage.Devices:
                devicesButton?.Classes.Add("Selected");
                break;

            case NavigationPage.Mappings:
                mappingsButton?.Classes.Add("Selected");
                break;

            case NavigationPage.Profiles:
                profilesButton?.Classes.Add("Selected");
                break;

            case NavigationPage.Settings:
                settingsButton?.Classes.Add("Selected");
                break;
        }
    }
    
    public void UpdateNavigationSelection(NavigationPage page)
    {
        UpdateSelectedButton(page);
    }


    private void UpdateThemeToggleButton()
    {
        if (this.TryFindControl<PathIcon>("ThemeIcon", out var themeIcon) &&
            this.TryFindControl<ToggleButton>("ThemeToggleButton", out var toggleButton))
        {
            var currentTheme = SettingsService.Theme;
            var actualTheme = ThemeVariantConverter.GetActualTheme(currentTheme);
            
            if (actualTheme == ThemeVariant.Dark)
            {
                themeIcon.Data = (Avalonia.Media.Geometry?)this.FindResource("weather_moon_regular");
                toggleButton.IsChecked = true;
            }
            else
            {
                themeIcon.Data = (Avalonia.Media.Geometry?)this.FindResource("weather_sunny_regular");
                toggleButton.IsChecked = false;
            }
        }
    }


    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateThemeToggleButton();
    }
}