using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using userinterface.Services;

namespace userinterface.Views.Profile;

public partial class ProfilesPageView : UserControl
{
    private readonly FrameTimerService? frameTimer;

    public ProfilesPageView()
    {
        frameTimer = App.Services?.GetService<FrameTimerService>();
        InitializeComponent();
        
        var contentControl = this.FindControl<ContentControl>("ProfileContentControl");
        if (contentControl != null)
        {
            contentControl.PropertyChanged += OnContentControlPropertyChanged;
        }
    }

    private void OnContentControlPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Content" && frameTimer != null)
        {
            var stopwatch = Stopwatch.StartNew();
            
            frameTimer.StartMonitoring("ProfilesPageView ContentControl content change");
            
            // Stop monitoring after a longer delay to capture full rendering cycle
            _ = System.Threading.Tasks.Task.Delay(200).ContinueWith(_ => 
            {
                frameTimer.StopMonitoring("ProfilesPageView ContentControl content change");
            });
        }
    }
}