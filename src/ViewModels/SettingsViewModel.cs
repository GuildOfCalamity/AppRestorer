using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;

namespace AppRestorer;

public class SettingsViewModel : INotifyPropertyChanged
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

    Window? _window;
    Dictionary<string, string> _modelDependencies = new Dictionary<string, string>();
    Timer? _debounceTimer;

    #region [Properties]
    public ICollectionView? AppsView { get; set; }
    public ObservableCollection<InstalledApp> _apps = new();
    public ObservableCollection<InstalledApp> Apps
    {
        get => _apps;
        set
        {
            _apps = value;
            OnPropertyChanged();
        }
    }

    bool _isBusy = true;
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

    string _statusText = "…";
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

    string? _filterText;
    public string? FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText != value)
            {
                IsBusy = true;
                _filterText = value;
                OnPropertyChanged();
                // Stop/Reset debounce timer
                _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _debounceTimer = new Timer(_ =>
                {
                    // After ~1 second, do refresh on UI thread
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        StatusText = $"Filtered by '{_filterText}'";
                        AppsView?.Refresh();
                        IsBusy = false;
                    });
                }, null, TimeSpan.FromSeconds(1.1d), Timeout.InfiniteTimeSpan);
            }
        }
    }

    bool _isAnimated = false;
    public bool IsAnimated
    {
        get => _isAnimated;
        set
        {
            if (_isAnimated != value)
            {
                _isAnimated = value;
                OnPropertyChanged();
            }
        }
    }
    #endregion

    public ICommand MenuCommand { get; set; }

    public SettingsViewModel(Window window)
    {
        _window = window;

        IsAnimated = ConfigManager.Get("IsAnimated", defaultValue: true);

        MenuCommand = new RelayCommand(() => 
        { 
            App.RootEventBus?.Publish(Constants.EB_ToWindow, $"[TEXT]{DateTime.Now.Ticks}");
            App.RootEventBus?.Publish(Constants.EB_ToSettings, $"{DateTime.Now.Ticks}"); 
        });

        StatusText = $"Reading from registry...";

        window.Loaded += Window_Loaded;
        window.StateChanged += Window_StateChanged;

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

    async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var runner = new TaskRunner(() =>
        {
            Apps = new ObservableCollection<InstalledApp>(GetUninstalls());

        }, "Get Uninstall Apps");
        runner.TaskCompleted += (s, e) =>
        {
            App.Current.Dispatcher.Invoke(() => {
                AppsView = CollectionViewSource.GetDefaultView(Apps);
                StatusText = $"Showing results";
                AppsView.Filter = FilterApps;
            });
        };
        await runner.RunAsync();
    }

    void Window_StateChanged(object? sender, EventArgs e)
    {
        var w = sender as System.Windows.Window;
        if (w == null) { return; }

        Debug.WriteLine($"[INFO] SettingsWindow state changed to {w.WindowState}");
        if (w.WindowState == WindowState.Minimized)
            IsAnimated = false;
        else
            IsAnimated = true;
    }

    bool FilterApps(object obj)
    {
        if (obj is InstalledApp app)
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return true;

            return app.DisplayName?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        return false;
    }

    #region [Registry]
    /// <summary>
    /// Fetches a list of all installed applications in the Win32 registry.<br/>
    /// Each application element stored as <see cref="InstalledApp"/>.<br/>
    /// </summary>
    /// <param name="is64"></param>
    /// <returns><see cref="List{T}"/> with type <see cref="InstalledApp"/></returns>
    public List<InstalledApp> GetUninstalls(bool is64 = true)
    {
        List<InstalledApp> apps = new List<InstalledApp>();

        Dictionary<RegistryKey, List<KeyValuePair<string, bool>>> hiveKeys = new Dictionary<RegistryKey, List<KeyValuePair<string, bool>>>() {
            { Registry.LocalMachine, new List<KeyValuePair<string, bool>>() {
                new KeyValuePair<string, bool>(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", true),
                new KeyValuePair<string, bool>(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)
            } },
            { Registry.CurrentUser, new List<KeyValuePair<string,bool>> {
                new KeyValuePair<string, bool>(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", true),
                new KeyValuePair<string, bool>(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)
            } }
        };

        try
        {
            IsBusy = true;
            foreach (var registry in hiveKeys)
            {
                foreach (var location in registry.Value)
                {
                    // Only check 64-bit KVP
                    if ((location.Value) && (!is64))
                        continue;

                    using (var key = registry.Key.OpenSubKey(location.Key))
                    {
                        if (key == null)
                            continue;

                        foreach (string name in key.GetSubKeyNames())
                        {
                            if (string.IsNullOrWhiteSpace(name))
                                continue;

                            try
                            {
                                using (var subKey = key.OpenSubKey(name))
                                {
                                    if (subKey == null)
                                        continue;

                                    if (string.IsNullOrWhiteSpace($"{subKey.GetValue("DisplayName")}"))
                                        continue;

                                    apps.Add(new InstalledApp
                                    {
                                        DisplayName = $"{subKey.GetValue("DisplayName")}",
                                        DisplayVersion = $"{subKey.GetValue("DisplayVersion")}",
                                        InstallSource = $"{subKey.GetValue("InstallSource")}",
                                        TargetDirectory = $"{subKey.GetValue("InstallLocation")}",
                                        InstallDate = $"{subKey.GetValue("InstallDate")}",
                                        Publisher = $"{subKey.GetValue("Publisher")}",
                                    });
                                }
                            }
                            catch (Exception) { /* ignore */}
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetUninstalls: {ex}");
        }
        finally
        {
            IsBusy = false;
        }

        return apps.OrderByDescending(a => a.InstallDate).ToList();
    }
    #endregion
}