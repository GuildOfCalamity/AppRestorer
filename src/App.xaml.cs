using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        bool _deepDive = false;
        public string saveFileName = "apps.json";
        public List<StartupEntry> startupEntries = new List<StartupEntry>();
        public static Uri FavEnabled = new Uri(@"Assets\FavoriteIcon3.png", UriKind.Relative);
        public static Uri FavDisabled = new Uri(@"Assets\FavoriteIcon4.png", UriKind.Relative);

        #region [Overrides]
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            Debug.WriteLine($"[INFO] AppDomainFullTrust: {AppDomain.CurrentDomain.IsFullyTrusted}");

            base.OnStartup(e);

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

            //Current.MainWindow = new MainWindow { Visibility = Visibility.Hidden };
        }


        protected override void OnExit(ExitEventArgs e)
        {
            // Moved to MainWindow's closing event.
            //SaveRunningApps();
            base.OnExit(e);
        }
        #endregion

        #region [Public Methods]
        public List<RestoreItem> CollectRunningApps()
        {
            //var runningApps = new List<string>();
            var runningApps = new List<RestoreItem>();

            // Ignore any apps that already start on login
            var registryApps = GetStartupApps();

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
                            !proc.MainModule.FileName.EndsWith(GetSelfName(), StringComparison.OrdinalIgnoreCase) &&
                            !registryApps.Any(ra => ra.Path.ToLower().Contains(proc.MainModule.FileName.ToLower())))
                        {
                            if (_deepDive)
                            {
                                if (startupEntries.Any(ent => !string.IsNullOrEmpty(ent.Command) && !ent.Command.Contains("non-exec or no action") && !ent.Command.Contains(proc.MainModule.FileName)))
                                    runningApps.Add(new RestoreItem { Location = proc.MainModule.FileName, Favorite = false });
                                else
                                    Debug.WriteLine($"[WARNING] {proc.MainModule.FileName} was found as part of the StartupEntries catalog, skipping module.");
                            }
                            else
                            {
                                //runningApps.Add(proc.MainModule.FileName);
                                runningApps.Add(new RestoreItem { Location = proc.MainModule.FileName, Favorite = false });
                            }
                        }
                    }
                }
                catch (Exception) { /* Ignore processes we can't access */ }
            }
            
            return runningApps;
        }


        public void SaveRunningApps()
        {
            //var runningApps = new List<string>();
            var runningApps = new List<RestoreItem>();

            // Ignore any apps that already start on login
            var registryApps = GetStartupApps();

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
                            !proc.MainModule.FileName.EndsWith(GetSelfName(), StringComparison.OrdinalIgnoreCase) &&
                            !registryApps.Any(ra => ra.Path.ToLower().Contains(proc.MainModule.FileName.ToLower())))
                        {
                            if (_deepDive)
                            {
                                if (startupEntries.Any(ent => !string.IsNullOrEmpty(ent.Command) && !ent.Command.Contains("non-exec or no action") && !ent.Command.Contains(proc.MainModule.FileName)))
                                    runningApps.Add(new RestoreItem { Location = proc.MainModule.FileName, Favorite = false });
                                else
                                    Debug.WriteLine($"[WARNING] {proc.MainModule.FileName} was found as part of the StartupEntries catalog, skipping module.");
                            }
                            else
                            {
                                //runningApps.Add(proc.MainModule.FileName);
                                runningApps.Add(new RestoreItem { Location = proc.MainModule.FileName, Favorite = false });
                            }
                        }
                    }
                }
                catch (Exception) { /* Ignore processes we can't access */ }
            }

            try
            {
                File.WriteAllText(saveFileName, JsonSerializer.Serialize(runningApps, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception) { /* Ignore */ }
        }

        public void SaveExistingApps(List<RestoreItem> appList)
        {
            try
            {
                File.WriteAllText(saveFileName, JsonSerializer.Serialize(appList, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception) { /* Ignore */ }
        }

        public List<RestoreItem> LoadSavedApps(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = saveFileName;

            if (!File.Exists(saveFileName))
                return new List<RestoreItem>();
            try
            {
                return JsonSerializer.Deserialize<List<RestoreItem>>(File.ReadAllText(saveFileName)) ?? new List<RestoreItem>();
            }
            catch { return new List<RestoreItem>(); }
        }

        public void BackupAppFile()
        {
            if (!File.Exists(saveFileName))
                return;

            try
            {
                File.Copy(saveFileName, $"{saveFileName}.{DateTime.Now:yyyyMMdd}.bak", true);
            }
            catch { /* Ignore */ }
        }

        public static bool ShowMessage(string message, Window? owner = null)
        {
            var msgBox = new MessageBoxWindow(message);
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
        
        public string GetSelfName() => $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.exe";
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

    /// <summary>
    /// Basic data model
    /// </summary>
    public class RestoreItem
    {
        public string? Location { get; set; }
        public bool Favorite { get; set; }
    }
}
