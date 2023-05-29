using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using userinterface.ViewModels;

namespace userinterface.Views
{
    public partial class MouseListenView : UserControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> RawMouseDeltaEvent =
            RoutedEvent.Register<MouseListenView, RoutedEventArgs>(nameof(RawMouseDelta), RoutingStrategies.Direct);

        public MouseListenView()
        {
            InitializeComponent();
        }

        public event EventHandler<RoutedEventArgs> RawMouseDelta
        {
            add => AddHandler(RawMouseDeltaEvent, value);
            remove => RemoveHandler(RawMouseDeltaEvent, value);
        }
    }
}
