using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
        }

        public string ProfileOneTitle { get; set; } = "Profile1";

        public string ChartTitle { get; } = "Test Chart";

        public void SetLastMouseMove(float x, float y)
        {
        }
    }
}
