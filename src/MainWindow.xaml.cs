using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace AppRestorer
{
    /// <summary>
    /// The App Restorer's Main Window.
    /// </summary>
    /// <remarks>
    /// Add support for passing command switches to restored process during launch.
    /// </remarks>
    public partial class MainWindow : Window
    {
        #region [Local members]
        App? _app;
        List<string>? _appList;
        DispatcherTimer? _timer;
        bool _firstRun;
        double _interval;
        DateTime _lastUse;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region [Persistent settings]
            ConfigManager.OnError += (sender, ex) => 
            { 
                MessageBox.Show(
                    $"Error: {ex.Message}", 
                    "ConfigManager", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            };

            _firstRun = ConfigManager.Get("FirstRun", defaultValue: true);
            _interval = ConfigManager.Get("PollIntervalInMinutes", defaultValue: 60d);
            _lastUse = ConfigManager.Get("LastUse", defaultValue: DateTime.Now);
            #endregion

            #region [Timer for recording apps]
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(_interval);
            _timer.Tick += (s, ev) =>
            {
                _app?.BackupAppFile();
                _app?.SaveRunningApps();
                // Good practice in the event that timer is changed to different type other than DispatcherTimer.
                tbStatus.Dispatcher.Invoke(delegate() 
                {
                    tbStatus.Text = $"Next check will occur {_timer.Interval.DescribeFutureTime()}";
                });
            };
            _timer.Start();
            #endregion
        }

        #region [Events]
        void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (AppList == null || AppList.Items == null || AppList.Items.Count == 0)
            {
                tbStatus.Text = $"No apps to restore at {DateTime.Now.ToLongTimeString()}";
                return;
            }

            int enabled = GetEnabledAppCount();
            if (enabled == 0)
            {
                tbStatus.Text = $"Please select apps to restore";
                return;
            }
            bool answer = App.ShowMessage($"Do you wish to restore {enabled} {(enabled == 1 ? "app?" : "apps?")}", this);
            if (!answer)
            {
                tbStatus.Text = $"User canceled restore at {DateTime.Now.ToLongTimeString()}";
                return;
            }

            int extend = 0;
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
                        extend++;
                        _ = TimedTask.Schedule(() =>
                        {
                            try { Process.Start($"{item}"); }
                            catch { /* ignore if fails */ }
                        },
                        DateTime.Now.AddSeconds(extend));
                    }
                }
            }
            tbStatus.Text = $"Restoration process complete {DateTime.Now.ToLongTimeString()}";
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
            if (System.IO.File.Exists(_app?.saveFileName))
            {
                try
                {
                    var apps = JsonSerializer.Deserialize<List<string>>(System.IO.File.ReadAllText(_app.saveFileName));
                    if (apps != null && apps.Any())
                    {
                        bool answer = App.ShowMessage($"Do you wish to restore {_appList?.Count} {(_appList?.Count == 1 ? "app?" : "apps?")}", this);
                        if (answer)
                        {
                            int extend = 0;
                            foreach (var app in apps)
                            {
                                extend++;
                                string fullPath = app;
                                _ = TimedTask.Schedule(() =>
                                {
                                    try { Process.Start(fullPath); }
                                    catch { /* ignore if fails */ }
                                },
                                DateTime.Now.AddSeconds(extend));
                            }
                        }
                    }
                }
                catch { /* ignore parse errors */ }
            }

            if (_timer != null)
                tbStatus.Text = $"Next check will occur {_timer.Interval.DescribeFutureTime()}";
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigManager.Set("FirstRun", value: false);
            ConfigManager.Set("PollIntervalInMinutes", _interval);
            ConfigManager.Set("LastUse", value: DateTime.Now);
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

        void Minimize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Minimized;
        }
        #endregion

        #region [Helpers]
        void InitAndLoadApps()
        {
            _app = (App)Application.Current;
            _appList = _app.LoadSavedApps().OrderBy(o => o).ToList();
            AppList.ItemsSource = _appList;
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
        #endregion

        #region [Superfluous]
        static async void RunTimedTaskTests()
        {
            #region [Without a return value]
            var tt1 = TimedTask.Schedule(() =>
            {
                Debug.WriteLine($"Task 1 executed at {DateTime.Now.ToLongTimeString()}");
            },
            DateTime.Now.AddSeconds(2));

            var tt2 = TimedTask.Schedule(() =>
            {
                Console.WriteLine($"Task 2 executed at {DateTime.Now.ToLongTimeString()}");
            },
            TimeSpan.FromSeconds(4));

            Debug.WriteLine("Pending tasks: " + TimedTask.GetPendingCount());
            Debug.WriteLine("Completed tasks: " + TimedTask.GetCompletedCount());

            // Cancel second task after 1 second
            Thread.Sleep(1000);
            tt2.Cancel();

            Debug.WriteLine("Wait enough time for tasks to run (1st part)");
            Thread.Sleep(5000);

            Debug.WriteLine("Pending tasks: " + TimedTask.GetPendingCount());
            Debug.WriteLine("Completed tasks: " + TimedTask.GetCompletedCount());
            #endregion

            #region [With a return value]
            var voidTask = TimedTask.Schedule(() =>
            {
                Debug.WriteLine("No result task");
            },
            DateTime.Now.AddSeconds(1));

            var resultTask = TimedTask<DateTime>.Schedule(() =>
            {
                Debug.WriteLine("Returning current time");
                return DateTime.Now;
            },
            TimeSpan.FromSeconds(2));

            Debug.WriteLine("Pending tasks without a return value: " + TimedTask.GetPendingCount());
            Debug.WriteLine("Pending tasks with a return value: " + TimedTask<DateTime>.GetPendingCount());

            Debug.WriteLine("Wait enough time for tasks to run (2nd part)");
            Thread.Sleep(3000);

            Debug.WriteLine($"Result from resultTask: {resultTask.Result.ToLongTimeString()}");
            Debug.WriteLine("Completed (no result): " + TimedTask.GetCompletedCount());
            Debug.WriteLine("Completed (with result): " + TimedTask<int>.GetCompletedCount());
            #endregion

            #region [Asynchronous with events]
            var asyncTimedTask = TimedTask<long>.Schedule(async () =>
            {
                await Task.Delay(500); // Simulate work
                Debug.WriteLine("Async work done");
                return DateTime.Now.Ticks;
            }, TimeSpan.FromSeconds(2));

            // Utilizing the built-in events
            asyncTimedTask.OnStarted += () => Debug.WriteLine("[AsyncTask] Started");
            asyncTimedTask.OnCompleted += longResult => Debug.WriteLine($"[AsyncTask] Completed with result {longResult}");
            asyncTimedTask.OnCanceled += () => Debug.WriteLine("[AsyncTask] Canceled");

            // Dump result after completed
            var result = await asyncTimedTask.ResultTask;
            Debug.WriteLine("Final Async Result: " + result);
            #endregion
        }
        #endregion
    }
}