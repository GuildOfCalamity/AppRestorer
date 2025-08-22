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
        MainViewModel? _vm;
        LinearGradientBrush _lvl0 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(120, 120, 120));
        LinearGradientBrush _lvl1 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(0, 181, 255));
        LinearGradientBrush _lvl2 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 216, 0));
        LinearGradientBrush _lvl3 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 106, 0));
        LinearGradientBrush _lvl4 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 0, 0));
        #endregion

        public MainWindow()
        {
            Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

            InitializeComponent();

            // We'll pass the MainWindow to the VM so common Window events will become simpler to work with.
            this.DataContext = new MainViewModel(this);

            // This simple app doesn't really need a VM, but it's good practice.
            _vm = DataContext as MainViewModel;

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                Debug.WriteLine("[INFO] XAML system is in design mode.");
            else
                Debug.WriteLine("[INFO] XAML system is not in design mode.");

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
                _vm?.BackupAppFile(false);
                if (_appList?.Count > 0)
                {
                    var finalists = PreserveFavoritesWithMerge(_appList, _vm?.CollectRunningApps());
                    _vm?.SaveExistingApps(finalists);
                }
                else
                {
                    _vm?.SaveRunningApps();
                }

                UpdateText(tbStatus, $"Next check will occur {_timer.Interval.DescribeFutureTime()}");
            };
            _timer.Start();
            #endregion
        }

        #region [Events]
        void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (AppList == null || AppList.Items == null || AppList.Items.Count == 0)
            {
                UpdateText(tbStatus, $"No apps to restore at {DateTime.Now.ToLongTimeString()}");
                return;
            }

            int enabled = GetEnabledAppCount();
            if (enabled == 0)
            {
                UpdateText(tbStatus, $"Please select apps to restore");
                return;
            }
            bool answer = App.ShowMessage($"Do you wish to restore {enabled} {(enabled == 1 ? "app?" : "apps?")}", "Confirm", this);
            if (!answer)
            {
                UpdateText(tbStatus, $"User canceled restore at {DateTime.Now.ToLongTimeString()}");
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
                            UpdateText(tbStatus, $"Already running '{ri.Location}'");
                            continue;
                        }
                        // Attempt to start the application
                        UpdateText(tbStatus, $"Restoring application '{ri.Location}'");
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
            UpdateText(tbStatus, $"Restoration process complete {DateTime.Now.ToLongTimeString()}");
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
            // Check for recent backup
            if (!System.IO.File.Exists($"{_vm?.saveFileName}.{DateTime.Now.AddDays(-1):yyyyMMdd}.bak"))
                ButtonBackup.IsEnabled = false;
            else
                ButtonBackup.IsEnabled = true;

            //if (this.WindowState == WindowState.Normal && btnSpin.Visibility == Visibility.Hidden)
            //    btnSpin.Visibility = Visibility.Visible;

            // EventBus demonstration
            App.RootEventBus?.Publish(Constants.EB_Notice, $"MainWindow_Activated: {this.WindowState}");
        }

        void Window_Deactivated(object sender, EventArgs e)
        {
            // EventBus demonstration
            App.RootEventBus?.Publish(Constants.EB_Notice, $"MainWindow_Deactivated: {this.WindowState}");
        }


        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //_vm!.IsBusy = true;
            UpdateText(tbStatus, $"Loading…");

            if (_appList == null)
                InitAndLoadApps();

            if (_appList?.Count == 0)
            {
                _vm?.SaveRunningApps();
                InitAndLoadApps();
            }

            UpdateText(tbStatus, $"Select any of the {_appList?.Count} apps to restore…");

            // Check for previous apps
            if (System.IO.File.Exists(_vm?.saveFileName ?? "apps.json"))
            {
                try
                {
                    var apps = JsonSerializer.Deserialize<List<RestoreItem>>(System.IO.File.ReadAllText(_vm?.saveFileName ?? "apps.json"));
                    if (apps != null && apps.Any())
                    {
                        bool answer = App.ShowMessage($"Do you wish to restore {_appList?.Count} {(_appList?.Count == 1 ? "app?" : "apps?")}", owner: this);
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
                UpdateText(tbStatus, $"Next check will occur {_timer.Interval.DescribeFutureTime()}");
                
            //_vm!.IsBusy = false;
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigManager.Set("FirstRun", value: false);
            ConfigManager.Set("PollIntervalInMinutes", value: _interval);
            ConfigManager.Set("LastUse", value: DateTime.Now);
            if (_appList?.Count > 0)
                _vm?.SaveExistingApps(_appList);
            else
                _vm?.SaveRunningApps();
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
            {
                this.WindowState = WindowState.Minimized;
                btnSpin.Visibility = Visibility.Hidden;
            }
        }

        void LoadBackup_Click(object sender, RoutedEventArgs e)
        {
            bool answer = App.ShowMessage($"Are you sure?", "Load Backup", this);
            if (answer)
            {
                int count = 0;
                var prevFile = $"{_vm?.saveFileName}.{DateTime.Now.AddDays(-1):yyyyMMdd}.bak";
                while (!System.IO.File.Exists(prevFile) && ++count < 30)
                {
                    prevFile = $"{_vm?.saveFileName}.{DateTime.Now.AddDays(-1 * count):yyyyMMdd}.bak";
                }
                try
                {
                    _appList = _vm?.LoadSavedApps(prevFile).OrderBy(o => o.Location).ToList();
                    AppList.ItemsSource = null;
                    AppList.ItemsSource = _appList;
                }
                catch (Exception) 
                { 
                    UpdateText(tbStatus, $"Failed to load from backup ({prevFile})", 3); 
                }
            }
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
                                img.Source = new BitmapImage(Constants.FavDisabled);
                            }
                            else
                            {
                                data.Favorite = true;
                                img.Source = new BitmapImage(Constants.FavEnabled);
                            }
                            //Debug.WriteLine($"{new string('=', 60)}");
                            //foreach (var item in _appList) { Debug.WriteLine($"[INFO] Favorite: {item.Favorite}"); }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reserved for debugging
        /// </summary>
        void Spinner_Click(object sender, RoutedEventArgs e)
        {
            UpdateText(tbStatus, $"{App.RuntimeInfo}", 1);
        }
        #endregion

        #region [Helpers]
        /// <summary>
        /// Thread-safe content update for any <see cref="FrameworkElement"/>.
        /// </summary>
        /// <param name="fe"><see cref="FrameworkElement"/></param>
        /// <param name="text">message to display</param>
        /// <param name="level">0=ordinary, 1=notice, 2=warning, 3=error, 4=critical</param>
        public void UpdateText(FrameworkElement fe, string text, int level = 0)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            fe.Dispatcher.Invoke(delegate ()
            {
                if (fe is TextBlock tbl)
                {
                    switch (level)
                    {
                        case 0: tbl.Foreground  = _lvl0; break;
                        case 1: tbl.Foreground  = _lvl1; break;
                        case 2: tbl.Foreground  = _lvl2; break;
                        case 3: tbl.Foreground  = _lvl3; break;
                        case 4: tbl.Foreground  = _lvl4; break;
                        default: tbl.Foreground = _lvl0; break;
                    }
                    tbl.Text = $"{text}";
                }
                else if (fe is TextBox tbx)
                {
                    switch (level)
                    {
                        case 0: tbx.Foreground  = _lvl0; break;
                        case 1: tbx.Foreground  = _lvl1; break;
                        case 2: tbx.Foreground  = _lvl2; break;
                        case 3: tbx.Foreground  = _lvl3; break;
                        case 4: tbx.Foreground  = _lvl4; break;
                        default: tbx.Foreground = _lvl0; break;
                    }
                    tbx.Text = $"{text}";
                }
                else if (fe is CheckBox cbx)
                {
                    switch (level)
                    {
                        case 0: cbx.Foreground  = _lvl0; break;
                        case 1: cbx.Foreground  = _lvl1; break;
                        case 2: cbx.Foreground  = _lvl2; break;
                        case 3: cbx.Foreground  = _lvl3; break;
                        case 4: cbx.Foreground  = _lvl4; break;
                        default: cbx.Foreground = _lvl0; break;
                    }
                    cbx.Content = $"{text}";
                }
            });
        }

        void InitAndLoadApps()
        {
            _app = (App)Application.Current;
            _appList = _vm?.LoadSavedApps().OrderBy(o => o.Location).ToList();
            AppList.ItemsSource = null;
            AppList.ItemsSource = _appList;
        }

        /// <summary>
        /// In the event that a favorite is closed, we should remember it in the data.
        /// </summary>
        public List<RestoreItem>? PreserveFavoritesWithMerge(List<RestoreItem> sourceList, List<RestoreItem>? targetList)
        {
            if (targetList == null || targetList.Count == 0)
                targetList = _vm?.CollectRunningApps();

            var lookup = sourceList
                .Where(s => s.Location != null)
                .ToDictionary(s => s.Location!, s => s.Favorite, StringComparer.OrdinalIgnoreCase);

            // Start with updated targetList
            var result = targetList?
                .Select(t =>
                {
                    if (t.Location != null && lookup.TryGetValue(t.Location, out var fav))
                        t.Favorite = fav;
                    return t;
                })
                .ToList();

            // Add any missing Favorites from sourceList
            var existingLocations = new HashSet<string?>(result?.Select(r => r.Location) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var missingFavorites = sourceList
                .Where(s => s.Favorite && !existingLocations.Contains(s.Location))
                .ToList();

            result?.AddRange(missingFavorites);

            return result;
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

        void TestingJoinPreserve()
        {
            #region [Testing preserve favorites even if closed]
            var list1 = new List<RestoreItem>();
            list1.Add(new RestoreItem { Location = @"D:\cache\thing1.exe", Favorite = false });
            list1.Add(new RestoreItem { Location = @"D:\cache\thing2.exe", Favorite = true });
            var list2 = new List<RestoreItem>();
            list2.Add(new RestoreItem { Location = @"D:\cache\thing1.exe", Favorite = false });
            var finalists = PreserveFavoritesWithMerge(list1, list2);

            // Using a join
            List<RestoreItem>? result = list2.Join(list1, t => t.Location, s => s.Location,
                (t, s) =>
                {
                    t.Favorite = s.Favorite;
                    return t;
                }).ToList();
            #endregion

        }
        #endregion
    }
}