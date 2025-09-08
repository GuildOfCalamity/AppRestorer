using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace AppRestorer;

/// <summary>
/// For the annoying "Restoring Network Connections" failure dialog that appears when switching networks on a LAN.
/// </summary>
public class ExplorerDialogCloser
{
    readonly bool _verbose = false;
    readonly string _partialTitle;
    readonly Timer? _timer;

    public ExplorerDialogCloser(string partialTitle = "Restoring Network", int checkIntervalMs = 15000, bool verbose = false)
    {
        _verbose = verbose;
        _partialTitle = partialTitle;
        _timer = new Timer(CheckAndClose, null, 0, checkIntervalMs);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    const uint BM_CLICK = 0x00F5;
    const int SW_HIDE = 0;
    const int SW_SHOWNORMAL = 1;
    const int SW_SHOWMINIMIZED = 2;
    const int SW_MAXIMIZE = 3;
    const int SW_SHOWNOACTIVATE = 4;
    const int SW_SHOW = 5;
    const int SW_MINIMIZE = 6;
    const int SW_SHOWMINNOACTIVE = 7;
    const int SW_SHOWNA = 8;
    const int SW_RESTORE = 9;
    const int SW_SHOWDEFAULT = 10;
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

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public void Cancel()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
    }

    /// <summary>
    /// #32770 is the standard windows dialog class name
    /// https://learn.microsoft.com/en-us/windows/win32/winmsg/about-window-classes#system-classes
    /// </summary>
    void CheckAndClose(object? state)
    {
        try
        {
            // #32770 is the standard windows dialog class name
            //var ptr = FindWindow("#32770", "Restoring Network Connections");

            EnumWindows((hWnd, lParam) =>
            {
                string title = GetWindowTitle(hWnd);
                if (!string.IsNullOrEmpty(title) && title.IndexOf(_partialTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Find the OK button
                    IntPtr hButton = FindWindowEx(hWnd, IntPtr.Zero, "Button", "OK");
                    if (hButton != IntPtr.Zero)
                    {
                        // Send BM_CLICK to button handle
                        PostMessage(hButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                        if (_verbose)
                            Extensions.WriteToLog($"ExplorerDialog: Closed dialog handle {hWnd}");
                    }
                }
                return true; // continue enumeration
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            if (_verbose)
                Extensions.WriteToLog($"ExplorerDialog.CheckAndClose(ERROR): {ex.Message}");
        }
    }

    static string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) { return string.Empty; }
        var sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return $"{sb}";
    }
}

/// <summary>
/// Automation based version of the <see cref="ExplorerDialogCloser"/>.
/// </summary>
public class DialogCloserUsingAutomation
{
    readonly bool _verbose = false;
    readonly string _partialTitle;
    readonly Timer? _timer;

    public DialogCloserUsingAutomation(string partialTitle = "Restoring Network", int checkIntervalMs = 30000, bool verbose = false)
    {
        _partialTitle = partialTitle;
        _timer = new Timer(CheckAndClose, null, 0, checkIntervalMs);
        _verbose = verbose;
    }

    public void Cancel()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
    }

    void CheckAndClose(object? state)
    {
        try
        {
            var root = AutomationElement.RootElement;
            var windows = root.FindAll(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            foreach (AutomationElement win in windows)
            {
                if (!string.IsNullOrWhiteSpace(win.Current.Name) && win.Current.Name.IndexOf(_partialTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var okButton = win.FindAll(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                        .Cast<AutomationElement>()
                        .FirstOrDefault();

                    if (okButton != null && okButton.TryGetClickablePoint(out var point))
                    {
                        if (_verbose)
                            Debug.WriteLine($"[AUTOMATION] Clickable point found: {point}");
                    }

                    if (okButton != null && okButton.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
                    {
                        ((InvokePattern)pattern).Invoke();
                        if (_verbose)
                            Debug.WriteLine($"[AUTOMATION] Closed dialog: {win.Current.Name}");
                    }
                }
                else
                {
                    if (_verbose)
                        Debug.WriteLine($"[AUTOMATION] Skipping: {win.Current.Name} ({win.Current.ClassName})");
                }
            }
        }
        catch (Exception ex)
        {
            // Ignore transient UI Automation errors
            if (_verbose)
                Debug.WriteLine($"[WARNING] CheckAndClose: {ex.Message}");
        }
    }
}
