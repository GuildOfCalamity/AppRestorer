using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AppRestorer;

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
    bool _dialogWatcher;
    double _interval;
    DateTime _lastUse;
    string? _voiceName;
    MainViewModel? _vm;
    LinearGradientBrush _lvl0 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(120, 120, 120));
    LinearGradientBrush _lvl1 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(0, 181, 255));
    LinearGradientBrush _lvl2 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 216, 0));
    LinearGradientBrush _lvl3 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 106, 0));
    LinearGradientBrush _lvl4 = Extensions.CreateGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 0, 0));
    #endregion

    ExplorerDialogCloser? _closer;

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
        _interval = ConfigManager.Get("PollIntervalInMinutes", defaultValue: 60D);
        _lastUse = ConfigManager.Get("LastUse", defaultValue: DateTime.Now);
        _voiceName = ConfigManager.Get("VoiceName", defaultValue: "Hortense");
        _dialogWatcher = ConfigManager.Get("DialogWatcher", defaultValue: false);
        if (_dialogWatcher)
            _closer = new ExplorerDialogCloser();
        #endregion

        #region [Timer for recording apps]
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMinutes(_interval);
        _timer.Tick += (s, ev) =>
        {
            UpdateText(tbStatus, $"Scanning…");
            _vm?.BackupAppFile(false);
            if (_appList?.Count > 0)
            {
                var finalists = PreserveFavoritesWithMerge(_appList, _vm?.CollectRunningApps());
                // If there's a difference then update the ItemsSource and save.
                if (finalists?.Count != _appList?.Count)
                {
                    AppList.ItemsSource = null;
                    AppList.ItemsSource = finalists;
                    _vm?.SaveExistingApps(finalists);
                }
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
    void Window_Activated(object sender, EventArgs e)
    {
        // Check for recent backup (up to two weeks old)
        int trys = 0;
        var prevFile = $"{_vm?.saveFileName}.{DateTime.Now.AddDays(-1):yyyyMMdd}.bak";
        while (!System.IO.File.Exists(prevFile) && ++trys < 15) { prevFile = $"{_vm?.saveFileName}.{DateTime.Now.AddDays(-1 * trys):yyyyMMdd}.bak"; }
        if (System.IO.File.Exists($"{prevFile}"))
            ButtonBackup.IsEnabled = true;
        else
            ButtonBackup.IsEnabled = false;

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

        //App.ShowDialog($"{Environment.NewLine}Initializing…", "Notice",
        //    autoFocus: false,
        //    autoClose: TimeSpan.FromSeconds(3),
        //    assetName: "AlertIcon2.png", assetOpacity: 0.3,
        //    owner: null);

        UpdateText(tbStatus, $"Loading…");

        if (_firstRun)
        {
            AnnounceMessage("Welcome to App Restorer.");
            if (StartupAnalyzer.CreateDesktopShortcut("AppRestorer.lnk", App.GetCurrentLocation()))
                AnnounceMessage("I have created a desktop shortcut for you.");
        }
        else
            AnnounceMessage("Loading App Restorer");


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
        if (_dialogWatcher)
            _closer?.Cancel();
        ConfigManager.Set("DialogWatcher", value: _dialogWatcher);
        ConfigManager.Set("FirstRun", value: false);
        ConfigManager.Set("PollIntervalInMinutes", value: _interval);
        ConfigManager.Set("LastUse", value: DateTime.Now);
        ConfigManager.Set("VoiceName", value: _voiceName ?? "Hortense");
        if (_appList?.Count > 0)
            _vm?.SaveExistingApps(_appList);
        else
            _vm?.SaveRunningApps();
    }

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
        bool answer = App.ShowMessage($"Do you wish to restore {enabled} {(enabled == 1 ? "app?" : "apps?")}", "Confirm", owner: this);
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
            //btnSpin.Visibility = Visibility.Hidden;
        }
    }

    void LoadBackup_Click(object sender, RoutedEventArgs e)
    {
        bool answer = App.ShowMessage($"Are you sure?", "Load Backup", owner: this);
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
                if (_appList != null && _appList.Count > 0)
                {
                    AppList.ItemsSource = null;
                    AppList.ItemsSource = _appList;
                }
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

        //var missingNonFavorites = result?.Where(s => !string.IsNullOrEmpty(s.Location) && !missingFavorites.Contains(s)).ToList();

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
    void AnnounceMessage(string message)
    {
        try
        {
            var voice = (ISpVoice)new SpVoice();
            voice.Rate = 0;     // -10 (slow) to +10 (fast)
            voice.Volume = 70;  // 0 to 100
            if (voice.SetVoiceByName(_voiceName ?? "Hazel"))
                voice.Speak(message, SpeechVoiceSpeakFlags.SVSFDefault);
            else
                voice.Speak(message, SpeechVoiceSpeakFlags.SVSFDefault);

            // If using SVSFlagsAsync, or create voice as global object so GC won't occur.
            //voice.WaitUntilDone(2000);
        }
        catch { }
    }

    /// <summary>
    /// SAPI still works in Win10/11
    /// </summary>
    /// <param name="window">For displaying the old SAPI settings window - may or may not work on your system.</param>
    void RunVoiceTests(Window? window)
    {
        #region [using dynamic]
        try
        {
            const int SVSFlagsAsync = 1;        // speak asynchronously
            const int SVSFPurgeBeforeSpeak = 2; // clear the queue before speaking
            const int SVSFIsFilename = 4;       // treat the text as a filename to read
            const int SVSFIsXML = 8;            // treat the text as XML markup
            const int SVSFPersistXML = 16;      // persist XML state changes across calls

            Type? comType = Type.GetTypeFromProgID("SAPI.SpVoice");
            if (comType != null)
            {
                dynamic? voice1 = Activator.CreateInstance(comType);
                if (voice1 != null)
                {
                    // Change voice 
                    voice1.Voice = voice1.GetVoices().Item(1);

                    // Change rate and volume
                    voice1.Rate = 1;     // Slightly faster
                    voice1.Volume = 70;  // 70% volume

                    voice1.Speak("Loading App Restorer", SVSFlagsAsync);

                    // List available voices
                    foreach (var token in voice1.GetVoices())
                    {
                        Debug.WriteLine($"[VOICE] {token.GetDescription()}");
                        /*
                        Microsoft David Desktop - English (United States)
                        Microsoft Hazel Desktop - English (Great Britain)
                        Microsoft Hedda Desktop - German
                        Microsoft Zira Desktop - English (United States)
                        Microsoft Hortense Desktop - French
                        Microsoft Elsa Desktop - Italian (Italy)
                        Microsoft Haruka Desktop - Japanese
                        Microsoft Heami Desktop - Korean
                        Microsoft Maria Desktop - Portuguese(Brazil)
                        Microsoft Huihui Desktop - Chinese (Simplified)
                        */
                    }


                    // Use a different voice
                    //voice.Voice = voice.GetVoices().Item(1);
                    //voice.Speak("This is a different voice.");

                }
            }

            
            //dynamic? wmplayer = Activator.CreateInstance(Type.GetTypeFromProgID("WMPlayer.OCX"));
            //wmplayer.URL = @"D:\Audio\Sound.mp3";
            //wmplayer.Controls.play();


            //dynamic? excel = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application"));
            //if (excel != null)
            //{
            //    excel.Visible = true;
            //    dynamic? wb = excel.Workbooks.Add();
            //    dynamic? ws = wb.Sheets[1];
            //    ws.Cells[1, 1].Value = "Hello from App Restorer";
            //}

        }
        catch { }
        #endregion

        #region [using wrapper]
        var voice = (ISpVoice)new SpVoice();

        // Synchronous speech
        voice.Speak("Hello world!", SpeechVoiceSpeakFlags.SVSFDefault);

        // Asynchronous speech
        voice.Speak("Speaking asynchronously...", SpeechVoiceSpeakFlags.SVSFlagsAsync);

        // Change rate and volume
        voice.Rate = 1;    // -10 (slow) to +10 (fast)
        voice.Volume = 70; // 0 to 100

        //voice.Pause();
        //Thread.Sleep(1000);
        //voice.Resume();

        // Wait until done (helpful when using SVSFlagsAsync)
        voice.WaitUntilDone(2000);

        if (voice.SetVoiceByName("Hazel"))
            voice.Speak("Hello, this is Microsoft Hazel.", SpeechVoiceSpeakFlags.SVSFDefault);
        else
            voice.Speak("Requested voice not found.", SpeechVoiceSpeakFlags.SVSFDefault);

        if (window != null)
        {
            // [Common SpVoice.DisplayUI Values]
            //-------------------------------------------------------------------
            // "AudioProperties"  - Shows audio format/properties dialog.
            // "AudioOutput"      - Lets the user choose the audio output device.
            // "VoiceProperties"  - Lets the user choose and configure the voice.
            // "EngineProperties" - Engine specific settings dialog.
            string uiName = "VoiceProperties";
            object? extraData = null;
            if (voice.IsUISupported(uiName, ref extraData))
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                voice.DisplayUI(hwnd, "Select Audio Output", uiName, ref extraData);
            }
            else
            {
                App.ShowMessage($"{uiName} UI not supported by this voice engine.", "Warning", "OK", "Cancel", owner: window);
            }
        }
        #endregion
    }

    /// <summary>
    /// dlgType 0 = [OK] <br/>
    /// dlgType 1 = [OK] [Cancel] <br/>
    /// dlgType 2 = [Abort] [Retry] [Cancel] <br/>
    /// dlgType 3 = [Yes] [No] [Cancel] <br/>
    /// dlgType 4 = [Yes] [No] <br/>
    /// dlgType 5 = [Retry] [Cancel] <br/>
    /// dlgType 6 = [Cancel] [Try Again] [Continue] <br/>
    /// </summary>
    void ShellShowPopup(string message, int dlgType = 1)
    {
        try
        {
            // late-bind to the Windows Script Host COM automation object
            var comObj = Type.GetTypeFromProgID("WScript.Shell");
            if (comObj == null) { return; }

            // create the System.__ComObject
            dynamic? shell = Activator.CreateInstance(comObj);
            if (shell == null) { return; }

            // Arguments: message, timeout(sec), title, type
            var rez = shell?.Popup(message, 3, "Notice", dlgType);
            if (rez != null)
            {
                if ((int)rez == 1)
                    Debug.WriteLine($"Button 1 pressed");
                else if ((int)rez == 2)
                    Debug.WriteLine($"Button 2 pressed");
            }
            // Cleanup
            if (shell != null && System.Runtime.InteropServices.Marshal.IsComObject(shell))
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
        }
        catch { /* Ignore */ }
    }

    void ShellSendKeys(string keys)
    {

        SendKeysHelper.SendToProcess("notepad", "Hello from AppRestorer!{ENTER}This was automated.");
        SendKeysHelper.SendToWindowTitle("Untitled - Notepad", "^s"); // Ctrl+S
        SendKeysHelper.ClickOkButton("Dialog Title");

        try
        {
            // late-bind to the Windows Script Host COM automation object
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) { return; }

            // create the System.__ComObject
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) { return; }

            shell?.SendKeys(keys);

            // Cleanup
            if (shell != null && System.Runtime.InteropServices.Marshal.IsComObject(shell))
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
        }
        catch { /* Ignore */ }
    }

    async void RunTimedTaskTests()
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
        List<RestoreItem>? result = list2
            .Join(list1, t => t.Location, s => s.Location,
            (t, s) =>
            {
                t.Favorite = s.Favorite;
                return t;
            }).ToList();
        #endregion

    }
    #endregion
}