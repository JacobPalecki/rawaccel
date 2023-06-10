using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;
using userinterface.ViewModels;

namespace userinterface.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (ViewModel is not null &&
                (change.Property == TransformedBoundsProperty || change.Property == WindowStateProperty))
            {
               if (WindowState == Avalonia.Controls.WindowState.Minimized)
               {
                   ViewModel.IsVisible = false;
                   System.Diagnostics.Debug.WriteLine($"Visibility set to false");
               }
               else
               {
                   ViewModel.IsVisible = true;
                   System.Diagnostics.Debug.WriteLine($"Visibility set to true");
               }
            }
        }
    }
}
