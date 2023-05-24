using System;
using userinterface.Models.Mouse;

namespace userinterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMouseMoveDisplayer
    {
        public MainWindowViewModel()
        {
            Profiles = new ProfilesViewModel();
            MouseListen = new MouseListenViewModel();
            MouseWindow = new MouseWindow(this);
        }

        public ProfilesViewModel Profiles { get; }

        public MouseListenViewModel MouseListen { get; }

        public MouseWindow MouseWindow { get; }

        public string Test => "Is this working?";

        public void SetLastMouseMove(float x, float y)
        {
            MouseListen.LastY = (int)y;
            MouseListen.LastX = (int)x;
        }

        public void ShowLastMouseMove()
        {
            MouseListen.UpdateDisplay();

            var size = MathF.Sqrt(MouseListen.LastX * MouseListen.LastX + MouseListen.LastY * MouseListen.LastY);
            Profiles.SetLastMouseMove(size, 1);
        }
    }
}
