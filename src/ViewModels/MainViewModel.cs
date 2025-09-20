using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32; // registry access

namespace AppRestorer;

/// <summary>
/// Swapping the system over to a MVVM style, reserved for future use.
/// This is overkill for such a simple utility, but it's good practice.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    #region [INotifyProperty]
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public void OnPropertyChangedUI([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher?.Invoke(delegate ()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
    public void OnPropertyChanging(string propertyName) => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    protected virtual bool OnPropertyChanged<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;
        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    #endregion

    #region [Local members]
    Window? _window = null;
    Dictionary<string, string> _modelDependencies = new Dictionary<string, string>();
    bool _deepDive = false;
    public string saveFileName = "apps.json";
    public List<StartupEntry> startupEntries = new List<StartupEntry>();
    DateTime _lastMouse = DateTime.MinValue; // can be used to trigger stale state (security logout)
    #endregion

    #region [Properties]
    string _statusText = "Select app to restore…";
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    string _titleText = App.AssemblyInfo;
    public string TitleText
    {
        get => _titleText;
        set
        {
            if (_titleText != value)
            {
                _titleText = value;
                OnPropertyChanged();
            }
        }
    }

    bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
    }

    CultureInfo? _currentCulture;
    public CultureInfo? CurrentCulture 
    { 
        get { return _currentCulture; }
        set 
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                OnPropertyChanged();
            }
        }
    }

    System.Windows.Media.SolidColorBrush? _binderBrush1;
    public System.Windows.Media.SolidColorBrush? BinderBrush1
    {
        get => _binderBrush1;
        set
        {
            if (_binderBrush1 != value)
            {
                _binderBrush1 = value;
                OnPropertyChanged();
            }
        }
    }
    System.Windows.Media.SolidColorBrush? _binderBrush2;
    public System.Windows.Media.SolidColorBrush? BinderBrush2
    {
        get => _binderBrush2;
        set
        {
            if (_binderBrush2 != value)
            {
                _binderBrush2 = value;
                OnPropertyChanged();
            }
        }
    }
    #endregion

    #region [Commands]
    public ICommand MinimizeCommand { get; set; }
    public ICommand MaximizeCommand { get; set; }
    public ICommand CloseCommand { get; set; }
    public ICommand DebugCommand { get; set; }
    public ICommand MenuCommand { get; set; }
    public ICommand AnalyzeCommand { get; set; }
    #endregion

    public MainViewModel(Window window)
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        _window = window;

        CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;

        CloseCommand = new RelayCommand(() => _window.Close());
        MinimizeCommand = new RelayCommand(() => _window.WindowState = WindowState.Minimized);
        MaximizeCommand = new RelayCommand(() => _window.WindowState ^= WindowState.Maximized);
        DebugCommand = new RelayCommand(() => 
        { 
            App.RootEventBus?.Publish(Constants.EB_ToWindow, $"[TIME]");
            // For testing the ColorPreview control.
            BinderBrush1 = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256)));
            BinderBrush2 = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256)));
        });
        MenuCommand = new RelayCommand(() => 
        { 
            App.RootEventBus?.Publish(Constants.EB_ToWindow, $"[SETTINGS]"); 
        });
        AnalyzeCommand = new RelayCommand(async () => 
        {
            IsBusy = !IsBusy;
            var registryApps = GetStartupApps();
            foreach (var app in registryApps )
            {
                App.RootEventBus?.Publish(Constants.EB_ToWindow, $"[TEXT] Startup ⇨ {app.Name}");
                await Task.Delay(350);
            }
            IsBusy = !IsBusy;
        });

        #region [Control Events]
        EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
        EventManager.RegisterClassHandler(typeof(Window), Window.MouseEnterEvent, new RoutedEventHandler(Window_MouseEnter));
        EventManager.RegisterClassHandler(typeof(Window), Window.MouseLeaveEvent, new RoutedEventHandler(Window_MouseLeave));
        _window.StateChanged += Window_StateChanged;
        #endregion

        if (_deepDive)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                startupEntries = StartupAnalyzer.GetAllStartupEntries();
                foreach (var se in startupEntries.OrderBy(x => x.Source).ThenBy(x => x.Scope).ThenBy(x => x.Name))
                {
                    Debug.WriteLine($"[{se.Source}] ({se.Scope}) {se.Name}");
                    Debug.WriteLine($"  Enabled: {se.Enabled?.ToString() ?? "Unknown"}");
                    Debug.WriteLine($"  Command: {se.Command}");
                    Debug.WriteLine($"  Location: {se.Location}");
                    Debug.WriteLine("");
                }
            });
        }

        // EventBus demonstration
        if (!App.RootEventBus.IsSubscribed(Constants.EB_ToModel))
            App.RootEventBus.Subscribe(Constants.EB_ToModel, EventBusMessageHandler);

        #region [Reflection]
        PropertyInfo[] props = this.GetType().GetProperties();
        foreach (PropertyInfo prop in props)
        {
            if (prop == null)
                continue;
            if (!_modelDependencies.ContainsKey(prop.Name))
                _modelDependencies.Add(prop.Name, prop.PropertyType.FullName ?? string.Empty);
        }
        Debug.WriteLine($"[INFO] Model initialized with {_modelDependencies.Count} dependency properties.");
        #endregion
    }

    #region [Core Methods]
    public List<RestoreItem> CollectRunningApps()
    {
        //var runningApps = new List<string>();
        var runningApps = new List<RestoreItem>();

        try
        {
            // Ignore any apps that already start on login
            var registryApps = GetStartupApps();

            // Collect shell:startup entries
            var shellStart = StartupAnalyzer.GetShellStartupFilesAndContents();

            // Traverse all running processes
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    // Don't include OS modules/services or firewall/vpn clients, as they typically
                    // start on their own or as needed. These strings can be moved to a config.

                    if (proc.MainWindowHandle != IntPtr.Zero && // if no window handle then possibly a service 
                        !string.IsNullOrEmpty(proc.MainModule?.FileName) &&
                        !proc.MainModule.FileName.ToLower().Contains("\\cisco") &&       // VPN client
                        !proc.MainModule.FileName.ToLower().Contains("\\sonicwall") &&   // VPN client
                        !proc.MainModule.FileName.ToLower().Contains("\\fortigate") &&   // VPN client
                        !proc.MainModule.FileName.ToLower().Contains("\\windowsapps") && // Outlook, Teams, etc
                        !proc.MainModule.FileName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
                    {
                        // Self, duplicate, and registry startup checks
                        if (!runningApps.Any(ri => ri.Location.Contains(proc.MainModule.FileName)) &&
                            !proc.MainModule.FileName.EndsWith(App.GetCurrentAssemblyNameWithExtension(), StringComparison.OrdinalIgnoreCase) &&
                            !registryApps.Any(ra => ra.Path.ToLower().Contains(proc.MainModule.FileName.ToLower())))
                        {
                            if (_deepDive)
                            {
                                if (startupEntries.Any(ent => !string.IsNullOrEmpty(ent.Command) && !ent.Command.Contains("non-exec or no action") && !ent.Command.Contains(proc.MainModule.FileName)))
                                    runningApps.Add(new RestoreItem { Location = proc.MainModule.FileName, Favorite = false });
                                else
                                    App.RootEventBus?.Publish(Constants.EB_ToWindow, $"{proc.MainModule.FileName} was found as part of the StartupEntries catalog, skipping module.");
                            }
                            else
                            {
                                var shellFound = shellStart.Any(s => !string.IsNullOrEmpty(s.Value) && s.Value.Contains(proc.MainModule.FileName));
                                if (!shellFound)
                                {
                                    //runningApps.Add(proc.MainModule.FileName);
                                    runningApps.Add(new RestoreItem { Location = proc.MainModule.FileName, Favorite = false });
                                }
                            }
                        }
                    }
                }
                catch (Exception) { /* Ignore processes we can't access */ }
            }
        }
        catch (Exception) { /* Ignore */}

        return runningApps;
    }

    public void SaveRunningApps()
    {
        var runningApps = CollectRunningApps();
        if (runningApps.Count == 0)
            return;

        try
        {
            System.IO.File.WriteAllText(saveFileName, JsonSerializer.Serialize(runningApps, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"SaveRunningApps: {ex.Message}");
        }
    }

    public void SaveExistingApps(List<RestoreItem>? appList)
    {
        if (appList == null || appList.Count == 0)
            return;

        try
        {
            System.IO.File.WriteAllText(saveFileName, JsonSerializer.Serialize(appList, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"SaveExistingApps: {ex.Message}");
        }
    }

    public List<RestoreItem> LoadSavedApps(string filePath = "")
    {
        if (string.IsNullOrEmpty(filePath))
            filePath = saveFileName;

        if (!System.IO.File.Exists(filePath))
            return new List<RestoreItem>();
        try
        {
            return JsonSerializer.Deserialize<List<RestoreItem>>(System.IO.File.ReadAllText(filePath)) ?? new List<RestoreItem>();
        }
        catch (Exception ex)
        { 
            Extensions.WriteToLog($"LoadSavedApps: {ex.Message}");
            return new List<RestoreItem>(); 
        }
    }

    public void BackupAppFile(bool replaceExisting)
    {
        try
        {
            var bkup = $"{saveFileName}.{DateTime.Now:yyyyMMdd}.bak";
            if (System.IO.File.Exists(saveFileName) && (!System.IO.File.Exists(bkup) || replaceExisting))
                System.IO.File.Copy(saveFileName, $"{bkup}", true);
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"BackupAppFile: {ex.Message}");
        }
    }
    #endregion

    #region [Deferred Events]
    /// <summary>
    /// For <see cref="EventBus"/> demonstration. 
    /// Currently this is not used for any real functionality.
    /// </summary>
    void EventBusMessageHandler(object? sender, ObjectEventBusArgs e)
    {
        if (e.Payload == null)
            return;

        if (e.Payload.GetType() == typeof(string))
        {
            Debug.WriteLine($"[EVENTBUS] {e.Payload}");
            if ($"{e.Payload}".Contains("Activated"))
                IsBusy = true;
            else if ($"{e.Payload}".Contains("Deactivated"))
                IsBusy = false;
        }
        else if (e.Payload.GetType() == typeof(RestoreItem))
            Debug.WriteLine($"[EVENTBUS] {((RestoreItem)e.Payload).Location}");
        else if (e.Payload.GetType() == typeof(System.Exception))
            Extensions.WriteToLog($"{((Exception)e.Payload).Message}");
        else
            Debug.WriteLine($"[EVENTBUS] Received event bus message of type '{e.Payload.GetType()}'");
    }

    void Window_MouseEnter(object sender, RoutedEventArgs e)
    {
        //var win = sender as System.Windows.Window;
        _lastMouse = DateTime.Now;
    }

    void Window_MouseLeave(object sender, RoutedEventArgs e)
    {
        //var win = sender as System.Windows.Window;
        _lastMouse = DateTime.Now;
    }

    void Window_StateChanged(object? sender, EventArgs e)
    {
        var w = sender as System.Windows.Window;
        if (w == null) { return; }

        Debug.WriteLine($"[INFO] WindowStateChanged: {w.WindowState}");
    }

    void TextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var tb = sender as System.Windows.Controls.TextBox;
            if (tb != null)
                tb.SelectAll();
        }
        catch { }
    }
    #endregion

    #region [Registry]
    public List<(string Name, string Path)> GetStartupApps()
    {
        var results = new List<(string, string)>();

        // Registry paths for startup entries
        string[] keys = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",             // Current User
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",             // Local Machine (64‑bit)
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"  // Local Machine (32‑bit on 64‑bit OS)
        };

        try
        {
            // CurrentUser hive
            using (var cuKey = Registry.CurrentUser)
            {
                CollectFromKey(cuKey, keys[0], results);
            }
        }
        catch (UnauthorizedAccessException) { /* Ignore */ }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to read CurrentUser registry: {ex.Message}");
        }

        try
        {
            // LocalMachine hive
            using (var lmKey = Registry.LocalMachine)
            {
                CollectFromKey(lmKey, keys[1], results);
                CollectFromKey(lmKey, keys[2], results);
            }
        }
        catch (UnauthorizedAccessException) { /* Ignore */ }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to read LocalMachine registry: {ex.Message}");
        }

        return results;
    }

    static void CollectFromKey(RegistryKey root, string subKey, List<(string, string)> results)
    {
        try
        {
            using (var key = root.OpenSubKey(subKey))
            {
                if (key == null) return;

                foreach (var valueName in key.GetValueNames())
                {
                    var valueData = key.GetValue(valueName)?.ToString();
                    results.Add((valueName, valueData ?? string.Empty));
                }
            }
        }
        catch (UnauthorizedAccessException) { /* Ignore */ }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to read registry key '{subKey}': {ex.Message}");
        }
    }
    #endregion
}
