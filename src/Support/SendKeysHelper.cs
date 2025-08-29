using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace AppRestorer;

public static class SendKeysHelper
{
    #region [Imports]
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetWindowText(IntPtr hWnd, string lpString);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    const int SW_RESTORE = 9;
    const uint BM_CLICK = 0x00F5;
    const int CB_ERR = -1;
    const int CB_SELECTSTRING = 0x014D;
    const int WM_SETTEXT = 0x000C;
    const int WM_CLOSE = 0x0010;
    const int WM_COMMAND = 0x0111;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_LBUTTONUP = 0x0202;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_RBUTTONUP = 0x0205;
    const int MK_LBUTTON = 0x0001;
    const int MK_RBUTTON = 0x0002;
    #endregion

    /// <summary>
    /// Sends keystrokes to a window by process name.
    /// </summary>
    public static void SendToProcess(string processName, string keys, int waitForWindowMs = 6000)
    {
        var proc = WaitForProcess(processName, waitForWindowMs);
        if (proc == null || !IsHandleValid(proc.MainWindowHandle))
            throw new InvalidOperationException($"No window found for process '{processName}'.");

        FocusWindow(proc.MainWindowHandle);
        SendKeys(keys);
    }

    /// <summary>
    /// Sends keystrokes to a window by exact title match.
    /// </summary>
    public static void SendToWindowTitle(string windowTitle, string keys, int waitForWindowMs = 5000)
    {
        var proc = WaitForWindowTitle(windowTitle, waitForWindowMs);
        if (proc == null || !IsHandleValid(proc.MainWindowHandle))
            throw new InvalidOperationException($"No window found with title '{windowTitle}'.");

        FocusWindow(proc.MainWindowHandle);
        SendKeys(keys);
    }

    public static string GetText(IntPtr handle)
    {
        int length = GetWindowTextLength(handle);
        StringBuilder windowText = new StringBuilder(length + 1);
        GetWindowText(handle, windowText, windowText.Capacity);
        return $"{windowText}";
    }

    public static bool ClickOkButton(string windowTitle)
    {
        // Find the main dialog window
        IntPtr hWnd = FindWindow(null, windowTitle);
        if (hWnd == IntPtr.Zero)
            return false;

        // Find the OK button (class name "Button", text "OK")
        IntPtr hButton = FindWindowEx(hWnd, IntPtr.Zero, "Button", "OK");
        if (hButton == IntPtr.Zero)
            return false;

        // Send a click message
        return PostMessage(hButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
    }

    static Process? WaitForProcess(string name, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var procs = Process.GetProcessesByName(name);
            foreach (var p in procs)
            {
                if (IsHandleValid(p.MainWindowHandle))
                    return p;
            }
            Thread.Sleep(200);
        }
        return null;
    }

    static Process? WaitForWindowTitle(string title, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            foreach (var p in Process.GetProcesses())
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle) &&
                    string.Equals(p.MainWindowTitle, title, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            Thread.Sleep(200);
        }
        return null;
    }

    static void FocusWindow(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SW_RESTORE);
        SetForegroundWindow(hWnd);
        Thread.Sleep(100); // small buffer for focus change
    }

    static void SendKeys(string keys)
    {
        try
        {
            // late-bind to the Windows Script Host COM automation object
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) { return; }

            // create the System.__ComObject
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) { return; }

            shell.SendKeys(keys);

            // Cleanup
            if (shell != null && System.Runtime.InteropServices.Marshal.IsComObject(shell))
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
        }
        catch { /* Ignore */ }
    }

    static bool IsHandleValid(IntPtr handle) => handle != IntPtr.Zero;

    #region [Windows Automation]
    public static bool ClickOkButton(string windowTitle, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            // Get the root automation element for the desktop
            var root = AutomationElement.RootElement;
            // Find the top-level window
            var window = root.FindFirst(TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, windowTitle));

            if (window != null)
            {
                // Find the first button with ControlType.Button and Name containing "OK"
                var okButton = window
                    .FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                    .Cast<AutomationElement>()
                    .FirstOrDefault(b => b.Current.Name.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0);

                if (okButton != null)
                {
                    if (okButton.TryGetClickablePoint(out var point))
                    {
                        Debug.WriteLine($"[AUTOMATION] Clickable point found: {point}");
                    }

                    if (okButton.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
                    {
                        ((InvokePattern)pattern).Invoke();
                        return true;
                    }
                }
            }

            Thread.Sleep(200);
        }
        Debug.WriteLine($"[AUTOMATION] {sw.Elapsed.ToReadableTime()}");
        sw.Stop();
        return false;
    }

    public static bool ClickOkButtonFlexible(string partialWindowTitle, string? preferredButtonText = "OK", string? automationId = null, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var root = AutomationElement.RootElement;

            // Find window whose title contains the partial text
            var window = root
                .FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window))
                .Cast<AutomationElement>()
                .FirstOrDefault(w =>
                    w.Current.Name.IndexOf(partialWindowTitle, StringComparison.OrdinalIgnoreCase) >= 0);

            if (window != null)
            {
                AutomationElement? targetButton = null;

                // Try AutomationId if provided
                if (!string.IsNullOrEmpty(automationId))
                {
                    targetButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
                }

                // Try preferred button text if provided
                if (targetButton == null && !string.IsNullOrEmpty(preferredButtonText))
                {
                    targetButton = window.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                        .Cast<AutomationElement>()
                        .FirstOrDefault(b =>
                            b.Current.Name.IndexOf(preferredButtonText, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // Fallback: first button in the window
                if (targetButton == null)
                {
                    targetButton = window.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                        .Cast<AutomationElement>()
                        .FirstOrDefault();
                }

                // Invoke if found
                if (targetButton != null &&
                    targetButton.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
                {
                    ((InvokePattern)pattern).Invoke();
                    return true;
                }
            }

            Thread.Sleep(200);
        }
        Debug.WriteLine($"[AUTOMATION] {sw.Elapsed.ToReadableTime()}");
        sw.Stop();
        return false;
    }

    #endregion
}
