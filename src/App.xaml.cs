using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace AppRestorer
{
    public partial class App : Application
    {
        DispatcherTimer _timer;
        public string _saveFile = "apps.json";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Run minimized, no window shown at startup
            //Current.MainWindow = new MainWindow { Visibility = Visibility.Hidden };

            // Start timer for recording apps
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromHours(1);
            _timer.Tick += (s, ev) => SaveRunningApps();
            _timer.Start();

            // Save immediately on startup as well
            SaveRunningApps();
        }

        public void SaveRunningApps()
        {
            var runningApps = new List<string>();

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero &&
                        !string.IsNullOrEmpty(proc.MainModule?.FileName) &&
                        !proc.MainModule.FileName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
                    {
                        if (!runningApps.Contains(proc.MainModule.FileName))
                            runningApps.Add(proc.MainModule.FileName);
                    }
                }
                catch (Exception)
                {
                    // Ignore processes we can't access
                }
            }

            try
            {
                File.WriteAllText(_saveFile, JsonSerializer.Serialize(runningApps, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception) { /* Ignore */ }
        }

        public List<string> LoadSavedApps()
        {
            if (!File.Exists(_saveFile)) 
                return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_saveFile)) ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveRunningApps();
            base.OnExit(e);
        }

        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"[ERROR] Unhandled exception: {e.Exception.Message}");
            e.Handled = true; // Prevent application crash
        }
    }
}
