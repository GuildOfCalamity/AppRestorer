using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        List<RestoreItem>? _appList;
        DispatcherTimer? _timer;
        bool _firstRun;
        double _interval;
        DateTime _lastUse;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region [Testing preserve favorites even if closed]
            var list1 = new List<RestoreItem>();
            list1.Add(new RestoreItem { Location = @"D:\cache\thing1.exe", Favorite = false });
            list1.Add(new RestoreItem { Location = @"D:\cache\thing2.exe", Favorite = true });
            var list2 = new List<RestoreItem>();
            list2.Add(new RestoreItem { Location = @"D:\cache\thing1.exe", Favorite = false });
            var finalists = PreserveFavoritesWithMerge(list1, list2);

            list2 = list1.Join(list1, 
                t => t.Location, 
                s => s.Location,
                (t, s) => 
                { 
                    t.Favorite = s.Favorite; 
                    return t; 
                })
                .ToList();
            #endregion

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
                if (_appList?.Count > 0)
                {
                    var finalists = PreserveFavoritesWithMerge(_appList, _app?.CollectRunningApps());
                    _app?.SaveExistingApps(finalists);
                }
                else
                    _app?.SaveRunningApps();

                // Good practice in the event that timer is changed to different type other than DispatcherTimer.
                tbStatus.Dispatcher.Invoke(delegate ()
                {
                    tbStatus.Text = $"Next check will occur {_timer.Interval.DescribeFutureTime()}";
                });
            };
            _timer.Start();
            #endregion
        }

        /// <summary>
        /// In the event that a favorite is closed, we should remember it in the data.
        /// </summary>
        public List<RestoreItem> PreserveFavoritesWithMerge(List<RestoreItem> sourceList, List<RestoreItem>? targetList)
        {
            if (targetList == null || targetList.Count == 0)
                targetList = _app?.CollectRunningApps();

            var lookup = sourceList
                .Where(s => s.Location != null)
                .ToDictionary(s => s.Location!, s => s.Favorite, StringComparer.OrdinalIgnoreCase);

            // Start with updated targetList
            var result = targetList
                .Select(t =>
                {
                    if (t.Location != null && lookup.TryGetValue(t.Location, out var fav))
                        t.Favorite = fav;
                    return t;
                })
                .ToList();

            // Add any missing Favorites from sourceList
            var existingLocations = new HashSet<string?>(result.Select(r => r.Location), StringComparer.OrdinalIgnoreCase);

            var missingFavorites = sourceList
                .Where(s => s.Favorite && !existingLocations.Contains(s.Location))
                .ToList();

            result.AddRange(missingFavorites);

            return result;
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
                    var ri = item as RestoreItem;
                    var checkBox = FindVisualChild<CheckBox>(container);
                    if (checkBox != null && checkBox.IsChecked == true && ri != null)
                    {
                        if (IsAppRunning($"{ri.Location}"))
                        {
                            tbStatus.Text = $"Already running '{ri.Location}'";
                            continue;
                        }
                        // Attempt to start the application
                        tbStatus.Text = $"Restoring application '{ri.Location}'";
                        extend++;
                        _ = TimedTask.Schedule(() =>
                        {
                            try { Process.Start($"{ri.Location}"); }
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

                // Get the ListBoxItem and then its associated CheckBox.
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

        void Window_Activated(object sender, EventArgs e)
        {
            // Check for backups
            if (!System.IO.File.Exists($"{_app?.saveFileName}.{DateTime.Now.AddDays(-1):yyyyMMdd}.bak"))
                ButtonBackup.IsEnabled = false;
            else
                ButtonBackup.IsEnabled = true;
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_appList?.Count == 0)
                _app?.SaveRunningApps();

            InitAndLoadApps();

            tbStatus.Text = $"Select any of the {_appList?.Count} apps to restore…";

            // Check for previous apps
            if (System.IO.File.Exists(_app?.saveFileName))
            {
                try
                {
                    var apps = JsonSerializer.Deserialize<List<RestoreItem>>(System.IO.File.ReadAllText(_app.saveFileName));
                    if (apps != null && apps.Any())
                    {
                        bool answer = App.ShowMessage($"Do you wish to restore {_appList?.Count} {(_appList?.Count == 1 ? "app?" : "apps?")}", this);
                        if (answer)
                        {
                            int extend = 0;
                            foreach (var app in apps)
                            {
                                extend++;
                                string fullPath = app.Location ?? string.Empty;
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
            ConfigManager.Set("PollIntervalInMinutes", value: _interval);
            ConfigManager.Set("LastUse", value: DateTime.Now);
            if (_appList?.Count > 0)
                _app?.SaveExistingApps(_appList);
            else
                _app?.SaveRunningApps();
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

        void LoadBackup_Click(object sender, RoutedEventArgs e)
        {
            var prevFile = $"{_app?.saveFileName}.{DateTime.Now.AddDays(-1):yyyyMMdd}.bak";
            _appList = _app?.LoadSavedApps(prevFile).OrderBy(o => o).ToList();
            AppList.ItemsSource = null;
            AppList.ItemsSource = _appList;
        }

        void Favorite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DependencyObject dep)
            {
                var lbi = FindAncestor<ListBoxItem>(dep);
                if (lbi != null)
                {
                    // Now you have the ListBoxItem
                    // You can change the image, access lbi.DataContext, etc.
                    var data = lbi.DataContext as RestoreItem;
                    if (data != null)
                    {
                        var img = sender as Image;
                        if (img != null)
                        {
                            if (data.Favorite)
                            {
                                data.Favorite = false;
                                img.Source = new BitmapImage(App.FavDisabled);
                            }
                            else
                            {
                                data.Favorite = true;
                                img.Source = new BitmapImage(App.FavEnabled);
                            }
                            //Debug.WriteLine($"{new string('=', 60)}");
                            //foreach (var item in _appList) { Debug.WriteLine($"[INFO] Favorite: {item.Favorite}"); }
                        }
                    }
                }
            }
        }
        #endregion

        #region [Helpers]
        void InitAndLoadApps()
        {
            _app = (App)Application.Current;
            _appList = _app.LoadSavedApps().OrderBy(o => o.Location).ToList();
            AppList.ItemsSource = null;
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

                // Get the ListBoxItem and then its associated CheckBox.
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
        /// Extracts a child of a specific type from a parent <see cref="DependencyObject"/>.
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

        /// <summary>
        /// Extracts a parent of a specific type from the child <see cref="DependencyObject"/>.
        /// </summary>
        static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                    return target;

                current = VisualTreeHelper.GetParent(current);
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