using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Device;

namespace userinterface.Views.Device;

public partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
    }

    private void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
    {

        // Stop the event from propagating first
        e.Handled = true;

        // Manually execute the delete command
        if (DataContext is DeviceViewModel deviceViewModel)
        {
            if (deviceViewModel.DeleteCommand.CanExecute(null))
            {
                deviceViewModel.DeleteCommand.Execute(null);
            }
            else
            {
            }
        }
        else
        {
        }
    }
}