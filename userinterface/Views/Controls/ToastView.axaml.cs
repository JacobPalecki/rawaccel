using Avalonia.Controls;
using Avalonia.Media.Transformation;

namespace userinterface.Views.Controls
{
    public partial class ToastView : UserControl
    {
        public ToastView()
        {
            InitializeComponent();

            // Start toast below the visible area
            RenderTransform = TransformOperations.Parse("translate(0px, 120px)");
            Opacity = 0;
        }
    }
}