using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AppRestorer;

public partial class MessageBoxWindow : Window
{
    #region [Properties]
    DispatcherTimer? _timer;
    bool _autoClose = false;
    public bool Result { get; private set; }
    #endregion

    public MessageBoxWindow(string message) : this(message, "Notice", "Yes", "No", false) { }
    public MessageBoxWindow(string message, string title = "Notice") : this(message, title, "Yes", "No", false) { }
    public MessageBoxWindow(string message, string title = "Notice", string yes = "Yes", string no = "No", bool autoClose = false)
    {
        InitializeComponent();
        MessageText.Text = message;
        TitleText.Text = title;
        ButtonYes.Content = yes;
        ButtonNo.Content = no;
        _autoClose = autoClose;
    }

    #region [Events]
    void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // If window gets buried, then re-activate or close it for the user.
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(15D);
        _timer.Tick += (s, ev) =>
        {
            if (_autoClose)
            {
                _timer?.Stop();
                DialogResult = Result = false; // auto-close window
            }
            else
            {
                this.Activate();
                this.Focus();
            }
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
        DialogResult = Result = true; // close window
    }

    void No_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = Result = false; // close window
    }

    void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = Result = false; // close window
        }
    }

    /// <summary>
    /// Drag/Move support
    /// NOTE: Make sure the background for the control is not equal to transparent, otherwise this event will not be picked up.
    /// e.g. Background="#00111111" will work, but Background="Transparent" will not.
    /// </summary>
    void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Cursor = Cursors.Hand;
            DragMove();
        }
        Cursor = Cursors.Arrow;
    }
    #endregion
}
