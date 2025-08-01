using Avalonia.Controls;
using Avalonia.Input;

namespace userinterface.Views.Mapping;

public partial class MappingListElementView : UserControl
{
    public MappingListElementView()
    {
        InitializeComponent();
    }

    private void OnInteractiveElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}