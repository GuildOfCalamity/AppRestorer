using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;


namespace AppRestorer
{
    public partial class App : Application
    {
        public static string AssemblyInfo { get; set; } = string.Empty;
        public static string RuntimeInfo { get; set; } = string.Empty;
        public static string BuildConfig { get; set; } = string.Empty;
        public static EventBus RootEventBus { get; set; } = new();

        #region [Overrides]
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            
            //Debug.WriteLine($"[INFO] AppDomainFullTrust: {AppDomain.CurrentDomain.IsFullyTrusted}");
            
            RuntimeInfo = $"{Extensions.GetRuntimeInfo()}";
            BuildConfig = $"{typeof(App).GetBuildConfig()}";
            AssemblyInfo = $"{typeof(App).ReflectAssemblyFramework()}";

            base.OnStartup(e);

            //Current.MainWindow = new MainWindow { Visibility = Visibility.Hidden };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine($"[INFO] App OnExit Event: {e.ApplicationExitCode}");
            // Moved to MainWindow's closing event.
            //SaveRunningApps();
            base.OnExit(e);
        }
        #endregion

        #region [Public Methods]
        public static bool ShowMessage(string message, string title = "Notice", Window? owner = null)
        {
            var msgBox = new MessageBoxWindow(message, title);
            if (owner != null) 
            { 
                msgBox.Owner = owner;
                msgBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else 
            { 
                msgBox.WindowStartupLocation = WindowStartupLocation.CenterScreen; 
            }
            bool? result = msgBox.ShowDialog();
            return result == true;
        }
        
        public static string GetSelfName() => $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.exe";
        #endregion

        #region [Domain Exceptions]
        void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Exception.Message) &&
                !e.Exception.Message.StartsWith("A task was canceled", StringComparison.OrdinalIgnoreCase) &&
                !e.Exception.Message.Contains($"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.XmlSerializers"))
            {
                var str = $"[WARNING] First chance exception: {e.Exception.Message}";
                Debug.WriteLine(str);
                str.WriteToLog();
            }
        }

        void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var str = $"[ERROR] Unhandled exception: {((Exception)e.ExceptionObject).Message}";
                Debug.WriteLine(str);
                str.WriteToLog();
                //MessageBox.Show(((Exception)e.ExceptionObject).Message, "AppRestore UnhandledException");
                //System.Diagnostics.EventLog.WriteEntry(SystemTitle, $"Unhandled exception thrown:\r\n{((Exception)e.ExceptionObject).ToString()}");
            }
            catch (Exception) { }
        }

        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var str = $"[ERROR] Unhandled exception: {e.Exception.Message}";
            Debug.WriteLine(str);
            e.Handled = true; // Prevent crash
            str.WriteToLog();
        }
        #endregion
    }
}
