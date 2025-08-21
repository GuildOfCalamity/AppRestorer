using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic;

namespace AppRestorer
{
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

        Window? _window = null;
        Dictionary<string, string> _modelDependencies = new Dictionary<string, string>();

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
        #endregion

        public ICommand MinimizeCommand { get; set; }
        public ICommand MaximizeCommand { get; set; }

        public MainViewModel(Window window)
        {
            _window = window;
            CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;

            MinimizeCommand     = new RelayCommand(() => _window.WindowState = WindowState.Minimized);
            MaximizeCommand     = new RelayCommand(() => _window.WindowState ^= WindowState.Maximized);

            #region [Control Events]
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
            EventManager.RegisterClassHandler(typeof(Window), Window.MouseEnterEvent, new RoutedEventHandler(Window_MouseEnter));
            EventManager.RegisterClassHandler(typeof(Window), Window.MouseLeaveEvent, new RoutedEventHandler(Window_MouseLeave));
            _window.StateChanged += Window_StateChanged;
            #endregion

            PropertyInfo[] props = this.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (prop == null)
                    continue;
                if (!_modelDependencies.ContainsKey(prop.Name))
                    _modelDependencies.Add(prop.Name, prop.PropertyType.FullName ?? string.Empty);
            }
        }

        void Window_MouseEnter(object sender, RoutedEventArgs e)
        {
            //var w = sender as System.Windows.Window;
            Debug.WriteLine($"[INFO] Window Mouse Enter");
            IsBusy = false;
        }
        
        void Window_MouseLeave(object sender, RoutedEventArgs e)
        {
            //var w = sender as System.Windows.Window;
            Debug.WriteLine($"[INFO] Window Mouse Leave");
            IsBusy = true;
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
                    tb.SelectAll(); // thread safe?
            }
            catch { }
        }
    }
}
