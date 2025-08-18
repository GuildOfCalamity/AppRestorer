using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.Win32; // registry access

namespace AppRestorer
{
    public partial class App : Application
    {
        public string saveFileName = "apps.json";

        #region [Overrides]
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            Debug.WriteLine($"[INFO] AppDomainFullTrust: {AppDomain.CurrentDomain.IsFullyTrusted}");

            base.OnStartup(e);

            // Run minimized, no window shown at startup
            //Current.MainWindow = new MainWindow { Visibility = Visibility.Hidden };

            // Save immediately on startup as well
            //SaveRunningApps();

            //StartupAnalyzer.Test();
        }


        protected override void OnExit(ExitEventArgs e)
        {
            SaveRunningApps();
            base.OnExit(e);
        }
        #endregion

        #region [Public Methods]
        public void SaveRunningApps()
        {
            var runningApps = new List<string>();

            // Ignore any apps that already start on login
            var registryApps = GetStartupApps();

            // Traverse all running processes
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero &&
                        !string.IsNullOrEmpty(proc.MainModule?.FileName) &&
                        !proc.MainModule.FileName.ToLower().Contains("\\sonicwall") &&   // VPN client
                        !proc.MainModule.FileName.ToLower().Contains("\\windowsapps") && // Outlook, Teams, etc
                        !proc.MainModule.FileName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
                    {
                        // De-dupe entries and check for registry startup apps
                        if (!runningApps.Contains(proc.MainModule.FileName) && 
                            !proc.MainModule.FileName.EndsWith(GetSelfName(), StringComparison.OrdinalIgnoreCase) &&
                            !registryApps.Any(ra => ra.Path.ToLower().Contains(proc.MainModule.FileName.ToLower())))
                        {
                            runningApps.Add(proc.MainModule.FileName);
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore processes we can't access
                }
            }

            try
            {
                File.WriteAllText(saveFileName, JsonSerializer.Serialize(runningApps, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception) { /* Ignore */ }
        }

        public List<string> LoadSavedApps()
        {
            if (!File.Exists(saveFileName))
                return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(saveFileName)) ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        public void BackupAppFile()
        {
            if (!File.Exists(saveFileName))
                return;

            try
            {
                File.Copy(saveFileName, $"{saveFileName}.{DateTime.Now:yyyyMMdd}.bak", true);
            }
            catch { /* ignore exceptions */ }
        }

        public static bool ShowMessage(string message, Window? owner = null)
        {
            var msgBox = new MessageBoxWindow(message);
            if (owner != null) { msgBox.Owner = owner; }
            bool? result = msgBox.ShowDialog();
            return result == true;
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
            catch (UnauthorizedAccessException) { /* Ignore access denied */ }
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
            catch (UnauthorizedAccessException) { /* Ignore access denied */ }
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
            catch (UnauthorizedAccessException) { /* Ignore access denied */ }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to read registry key '{subKey}': {ex.Message}");
            }
        }
        #endregion

        #region [Domain Exceptions]
        void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Exception.Message) &&
                !e.Exception.Message.StartsWith("A task was canceled", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"[WARNING] First chance exception: {e.Exception.Message}");
            }
        }

        void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine($"[WARNING] Unhandled exception: {((Exception)e.ExceptionObject).Message}");
                MessageBox.Show(((Exception)e.ExceptionObject).Message, "AppRestore UnhandledException");
                //System.Diagnostics.EventLog.WriteEntry(SystemTitle, $"Unhandled exception thrown:\r\n{((Exception)e.ExceptionObject).ToString()}");
            }
            catch (Exception) { }
        }

        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"[ERROR] Unhandled exception: {e.Exception.Message}");
            e.Handled = true; // Prevent application crash
        }
        #endregion

        public string GetSelfName() => $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.exe";
    }
}
