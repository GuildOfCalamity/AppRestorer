using System;
using System.IO;
using Microsoft.Win32;

namespace AppRestorer;

public static class RegistryStartupHelper
{
    const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Enables or disables startup for this app with one call.
    /// </summary>
    /// <param name="appName">The registry key name (usually your app name).</param>
    /// <param name="enable"><c>true</c> to add, <c>false</c> to remove</param>
    public static void SetStartup(string appName, bool enable)
    {
        if (enable)
        {
            if (!IsApplicationInStartup(appName))
                AddApplicationToStartup(appName);
        }
        else
        {
            if (IsApplicationInStartup(appName))
                RemoveApplicationFromStartup(appName);
        }
    }

    /// <summary>
    /// Checks whether the application is already set to run on startup.
    /// </summary>
    /// <param name="appName">The registry key name (usually your app name).</param>
    public static bool IsApplicationInStartup(string appName)
    {
        try
        {
            // If you switch to Registry.LocalMachine, it applies system-wide but requires admin.
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
            {
                if (key == null)
                    return false;

                var value = key.GetValue(appName) as string;
                return !string.IsNullOrEmpty(value);
            }
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"RegistryHelper: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds the application to startup with the current executable path.
    /// </summary>
    /// <param name="appName">The registry key name (usually your app name).</param>
    public static void AddApplicationToStartup(string appName)
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess()?.MainModule?.FileName;

            if (string.IsNullOrEmpty(exePath))
                throw new InvalidOperationException("Unable to determine the executable path.");

            // If you switch to Registry.LocalMachine, it applies system-wide but requires admin.
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
            {
                if (key == null)
                    throw new InvalidOperationException("Unable to open registry key for startup.");

                key.SetValue(appName, exePath, RegistryValueKind.String);
            }
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"RegistryHelper: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes the application from startup.
    /// </summary>
    /// <param name="appName">The registry key name (usually your app name).</param>
    public static void RemoveApplicationFromStartup(string appName)
    {
        try
        {
            // If you switch to Registry.LocalMachine, it applies system-wide but requires admin.
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
            {
                if (key == null)
                    return;

                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName);
                }
            }
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"RegistryHelper: {ex.Message}");
        }
    }
}
