using ReactiveUI;
namespace userinterface.ViewModels
{
    public class MouseListenViewModel : ViewModelBase
    {
        protected int _displayedLastX = 0;
        protected int _displayedLastY = 0;

        public MouseListenViewModel()
        {
        }

        public int LastX { get; set; }

        public int LastY { get; set; }

        public int DisplayedLastX
        {
            get => _displayedLastX;
            set => this.RaiseAndSetIfChanged(ref _displayedLastX, value);
        }

        public int DisplayedLastY
        {
            get => _displayedLastY;
            set => this.RaiseAndSetIfChanged(ref _displayedLastY, value);
        }

        public void UpdateDisplay()
        {
            System.Diagnostics.Debug.WriteLine($"Setting DisplayedLastX and Y to {LastX} and {LastY}");
            DisplayedLastX = LastX;
            DisplayedLastY = LastY;
        }
    }
}
