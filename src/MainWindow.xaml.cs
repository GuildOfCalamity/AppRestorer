using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AppRestorer
{
    /// <summary>
    /// The App Restorer's Main Window
    /// </summary>
    /// <remarks>
    /// Add support for passing command switches during restored process launch
    /// </remarks>
    public partial class MainWindow : Window
    {
        App? _app;
        List<string>? _appList;
        DispatcherTimer? _timer;

        public MainWindow()
        {
            InitializeComponent();

            // Start timer for recording apps
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(60);
            _timer.Tick += (s, ev) =>
            {
                _app?.BackupAppFile();
                _app?.SaveRunningApps();
            };
            _timer.Start();
        }

        void InitAndLoadApps()
        {
            _app = (App)Application.Current;
            _appList = _app.LoadSavedApps().OrderBy(o => o).ToList();
            AppList.ItemsSource = _appList;
        }

        void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (AppList == null || AppList.Items == null || AppList.Items.Count == 0)
            {
                tbStatus.Text = $"No applications to restore at {DateTime.Now.ToLongTimeString()}";
                return;
            }

            int enabled = GetEnabledAppCount();
            bool answer = App.ShowMessage($"Do you wish to restore {enabled} {(enabled == 1 ? "app?" : "apps?")}", this);
            if (!answer)
            {
                tbStatus.Text = $"User canceled restore at {DateTime.Now.ToLongTimeString()}";
                return;
            }

            foreach (var item in AppList.Items)
            {
                if (item is null)
                    continue;

                var container = AppList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container != null)
                {
                    var checkBox = FindVisualChild<CheckBox>(container);
                    if (checkBox != null && checkBox.IsChecked == true)
                    {
                        if (IsAppRunning($"{item}"))
                        {
                            tbStatus.Text = $"Already running '{item}'";
                            continue;
                        }
                        // Attempt to start the application
                        tbStatus.Text = $"Restoring application '{item}'";
                        try { Process.Start($"{item}"); }
                        catch { }
                    }
                }
            }
        }

        int GetEnabledAppCount()
        {
            int total = 0;

            if (AppList == null || AppList.Items == null)
                return total;

            foreach (var item in AppList.Items)
            {
                if (item is null)
                    continue;

                var container = AppList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container != null)
                {
                    var checkBox = FindVisualChild<CheckBox>(container);
                    if (checkBox != null && checkBox.IsChecked == true)
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        void Invert_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in AppList.Items)
            {
                if (item is null)
                    continue;

                var container = AppList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container != null)
                {
                    var checkBox = FindVisualChild<CheckBox>(container);
                    if (checkBox != null == true)
                    {
                        checkBox.IsChecked = !(checkBox.IsChecked == true);
                    }
                }
            }
        }

        void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitAndLoadApps();
            if (_appList?.Count == 0)
            {
                _app?.SaveRunningApps();
                InitAndLoadApps();
            }
            tbStatus.Text = $"Select any of the {_appList?.Count} apps to restore…";

            // Check for previous apps
            if (System.IO.File.Exists(_app.saveFileName))
            {
                try
                {
                    var apps = JsonSerializer.Deserialize<List<string>>(System.IO.File.ReadAllText(_app.saveFileName));
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
                        bool answer = App.ShowMessage($"Do you wish to restore {_appList?.Count} apps?", this);
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

        void MainControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Cursor = Cursors.Hand;
                DragMove();
            }
            Cursor = Cursors.Arrow;
        }

        bool IsAppRunning(string exePath)
        {
            try
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(exePath);
                return Process.GetProcessesByName(fileName).Any();
            }
            catch { return false; }
        }

        /// <summary>
        /// Extracts a child of a specific type from a parent
        /// </summary>
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

        void Minimize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Minimized;
        }
    }

    //public static class NaturalSortExtensions
    //{
    //    static readonly Regex _re = new(@"\d+", RegexOptions.Compiled);
    //
    //    public static IEnumerable<string> OrderNaturally(this IEnumerable<string> source)
    //    {
    //        return source.OrderBy(s => _re.Split(s), StringComparer.OrdinalIgnoreCase)
    //                     .ThenBy(s => _re.Matches(s).Cast<Match>()
    //                                      .Select(m => int.Parse(m.Value))
    //                                      .DefaultIfEmpty(0),
    //                             Comparer<IEnumerable<int>>.Create(CompareSequences));
    //    }
    //
    //    static int CompareSequences(IEnumerable<int> a, IEnumerable<int> b)
    //    {
    //        using var ea = a.GetEnumerator();
    //        using var eb = b.GetEnumerator();
    //        while (ea.MoveNext())
    //        {
    //            if (!eb.MoveNext()) 
    //                return 1;
    //
    //            int cmp = ea.Current.CompareTo(eb.Current);
    //
    //            if (cmp != 0) 
    //                return cmp;
    //        }
    //        return eb.MoveNext() ? -1 : 0;
    //    }
    //}

}