using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppRestorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> _apps;
        App _app;

        public MainWindow()
        {
            InitializeComponent();
            LoadApps();
        }

        void LoadApps()
        {
            _app = (App)Application.Current;
            _apps = _app.LoadSavedApps();
            AppList.ItemsSource = _apps;
        }

        void RestoreSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in AppList.Items)
            {
                if (item is null)
                    continue;

                var container = AppList.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.ListBoxItem;
                if (container != null)
                {
                    var checkBox = FindVisualChild<System.Windows.Controls.CheckBox>(container);
                    if (checkBox != null && checkBox.IsChecked == true)
                    {
                        if (IsAppRunning($"{item}"))
                        {
                            tbStatus.Text = $"The application '{item}' is already running.";
                            continue;
                        }
                        // Attempt to start the application

                        tbStatus.Text = $"Restoring application '{item}'…";
                        try { Process.Start($"{item}"); }
                        catch { }
                    }
                }
            }
        }

        void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                    return t;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        bool IsAppRunning(string exePath)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(exePath);
            return Process.GetProcessesByName(fileName).Any();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check for previous apps
            if (System.IO.File.Exists(_app._saveFile))
            {
                try
                {
                    var apps = JsonSerializer.Deserialize<List<string>>(System.IO.File.ReadAllText(_app._saveFile));
                    if (apps != null && apps.Any())
                    {
                        //var result = MessageBox.Show($"Restore {apps.Count} apps from last session?", "App Restorer", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        //if (result == MessageBoxResult.Yes)
                        //{
                        //    foreach (var app in apps)
                        //    {
                        //        try { Process.Start(app); }
                        //        catch { /* ignore if fails */ }
                        //    }
                        //}
                        bool answer = ShowMessage($"Do you wish to restore {_apps.Count} apps?", this);
                        if (answer)
                        {
                            foreach (var app in apps)
                            {
                                try { Process.Start(app); }
                                catch { /* ignore if fails */ }
                            }
                        }
                    }
                }
                catch { /* ignore parse errors */ }
            }
            
        }

        public static bool ShowMessage(string message, Window? owner = null)
        {
            var msgBox = new MessageBoxWindow(message);
            if (owner != null) { msgBox.Owner = owner; }
            bool? result = msgBox.ShowDialog();
            return result == true;
        }

        void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
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