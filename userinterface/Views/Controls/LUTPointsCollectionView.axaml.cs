using Avalonia.Controls;
using Avalonia.Input;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls
{
    public partial class LUTPointsCollectionView : UserControl
    {
        public LUTPointsCollectionView()
        {
            InitializeComponent();
            this.PointerWheelChanged += OnPointerWheelChanged;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (DataContext is LUTPointsCollectionViewModel viewModel)
            {
                var delta = e.Delta.Y;

                if (delta > 0 && viewModel.CanNavigatePrevious)
                {
                    viewModel.NavigatePreviousCommand.Execute(null);
                }
                else if (delta < 0 && viewModel.CanNavigateNext)
                {
                    viewModel.NavigateNextCommand.Execute(null);
                }

                e.Handled = true;
            }
        }
    }
}