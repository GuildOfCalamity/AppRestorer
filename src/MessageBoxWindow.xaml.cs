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

namespace AppRestorer
{
    public partial class MessageBoxWindow : Window
    {
        public bool Result { get; private set; }

        public MessageBoxWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true; // closes window
        }

        void No_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false; // closes window
        }

        void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Cursor = Cursors.Hand;
                DragMove();
            }
            Cursor = Cursors.Arrow;
        }
    }
}
