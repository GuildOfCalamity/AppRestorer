using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace AppRestorer;

public partial class TestingWindow : Window
{
    public TestingWindow()
    {
        InitializeComponent();
        this.Loaded += (s, e) => this.WindowState = WindowState.Maximized;
    }

    void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) 
            this.Close();
    }

    /// <summary>
    /// Drag/Move support.
    /// NOTE: Make sure the background for the control is not equal to transparent, otherwise this event will not be picked up.
    /// e.g. Background="#00111111" will work, but Background="Transparent" will not.
    /// </summary>
    void Control_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Cursor = Cursors.Hand;
            DragMove();
        }
        Cursor = Cursors.Arrow;
    }
}
