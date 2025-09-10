using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppRestorer.Controls
{
    /// <summary>
    /// Interaction logic for TestControl.xaml
    /// </summary>
    public partial class TestControl : UserControl
    {
        public TestControl()
        {
            InitializeComponent();
        }
    }

    public static class PressBehavior
    {
        public static readonly DependencyProperty IsPressedProperty = DependencyProperty.RegisterAttached(
                "IsPressed", 
                typeof(bool), 
                typeof(PressBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsPressed(DependencyObject d) => (bool)d.GetValue(IsPressedProperty);
        public static void SetIsPressed(DependencyObject d, bool value) => d.SetValue(IsPressedProperty, value);

        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool), 
                typeof(PressBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject d) => (bool)d.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject d, bool value) => d.SetValue(EnableProperty, value);

        static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement el)
            {
                if ((bool)e.NewValue)
                {
                    el.PreviewMouseDown += (_, __) =>
                    {
                        Debug.WriteLine($"[EVENT] PreviewMouseDown for {el.GetType()}");
                        SetIsPressed(el, true);
                    };
                    el.PreviewMouseUp += (_, __) =>
                    {
                        Debug.WriteLine($"[EVENT] PreviewMouseUp for {el.GetType()}");
                        SetIsPressed(el, false);
                    };
                    el.MouseLeave += (_, __) =>
                    {
                        Debug.WriteLine($"[EVENT] MouseLeave for {el.GetType()}");
                        SetIsPressed(el, false);
                    };
                }
            }
        }
    }

}
