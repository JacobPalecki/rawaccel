using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace gui
{
    public sealed class BgLabelControl : Control
    {
        public BgLabelControl()
        {
            this.DefaultStyleKey = typeof(BgLabelControl);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(BgLabelControl),
            new PropertyMetadata(default(string)));

        public bool HasLabelValue { get; set; }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            {
                BgLabelControl labelControl = d as BgLabelControl; //null checks omitted
                String s = e.NewValue as String; //null checks omitted
                if (s == String.Empty)
                {
                    labelControl.HasLabelValue = false;
                }
                else
                {
                    labelControl.HasLabelValue = true;
                }
            }
        }
    }
}
