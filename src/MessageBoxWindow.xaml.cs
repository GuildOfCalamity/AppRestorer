using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AppRestorer
{
    public partial class MessageBoxWindow : Window
    {
        DispatcherTimer? _timer;

        public bool Result { get; private set; }

        public MessageBoxWindow(string message, string title = "Notice", string yes = "Yes", string no = "No")
        {
            InitializeComponent();
            MessageText.Text = message;
            TitleText.Text = title;
            ButtonYes.Content = yes;
            ButtonNo.Content = no;

        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // If window gets buried, then re-activate it for the user.
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30d);
            _timer.Tick += (s, ev) =>
            {
                this.Activate();
                this.Focus();
            };
            _timer.Start();
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer?.Stop();
            _timer = null;
        }

        void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = Result = true; // closes window
        }

        void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = Result = false; // closes window
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
