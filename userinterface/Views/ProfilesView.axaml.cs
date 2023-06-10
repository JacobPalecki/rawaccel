using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ScottPlot.Avalonia;
using userinterface.ViewModels;

namespace userinterface.Views
{
    public partial class ProfilesView : ReactiveUserControl<ProfilesViewModel>
    {
        public ProfilesView()
        {
            InitializeComponent();
        }

        protected override void OnInitialized()
        {
            ViewModel!.InitializeSensitivity(this.Get<AvaPlot>(ProfilesViewModel.SensitivityChartName));

            base.OnInitialized();
        }
    }
}
