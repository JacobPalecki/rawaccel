using Avalonia.Controls;
using Avalonia.Input;
using LiveChartsCore.SkiaSharpView.Avalonia;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileChartView : UserControl
{
    private bool isChartInitialized = false;

    public ProfileChartView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (isChartInitialized || DataContext is not ProfileChartViewModel viewModel)
            return;

        isChartInitialized = true;

        try
        {
            if (!viewModel.IsInitialized)
            {
                await viewModel.InitializeAsync();
            }

            // Add click handler to the chart
            var chart = this.FindControl<CartesianChart>("Chart");
            if (chart != null)
            {
                chart.PointerPressed += OnChartPointerPressed;
            }
        }
        catch (System.Exception ex)
        {
        }
    }

    private void OnChartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not CartesianChart chart || DataContext is not ProfileChartViewModel viewModel)
            return;

        var position = e.GetPosition(chart);
        viewModel.HandleChartClick(chart, position.X, position.Y);
    }
}