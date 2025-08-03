using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;

namespace userinterface.Views.Controls
{
    public partial class DeviceConfigurationPromptView : UserControl
    {
        public bool? DialogResult { get; private set; }
        
        public DeviceConfigurationPromptView(string deviceName)
        {
            InitializeComponent();
            
            var localizationService = App.Services?.GetService<LocalizationService>();
            if (localizationService != null)
            {
                var messageTemplate = localizationService.GetText("UnconfiguredDeviceMessage");
                var formattedMessage = string.Format(messageTemplate, deviceName);
                MessageTextBlock.Text = formattedMessage;
            }
        }

        private void OnCreateClicked(object? sender, RoutedEventArgs e)
        {
            DialogResult = true;
            var modalService = App.Services?.GetService<IModalService>();
            modalService?.CloseCurrentModalWithResult(true);
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            var modalService = App.Services?.GetService<IModalService>();
            modalService?.CloseCurrentModalWithResult(false);
        }
    }
}