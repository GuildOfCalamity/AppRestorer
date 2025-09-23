using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppRestorer;

/// <summary>
/// May need to add admin privileges to the app's manifest:
/// <code>
///   <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
/// </code>
/// </summary>
public static class FirewallHelper
{
    #region [CLI Version]
    public static void AddInboundFirewallRuleCLI(string appPath, string ruleName)
    {
        string command = $"advfirewall firewall add rule name=\"{ruleName}\" " +
                         $"dir=in action=allow program=\"{appPath}\" enable=yes";

        ProcessStartInfo psi = new ProcessStartInfo("netsh", command)
        {
            Verb = "runas", // Run as administrator
            CreateNoWindow = true,
            UseShellExecute = true
        };

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Failed to add firewall rule: {ex.Message}");
        }
    }

    public static void AddOutboundRuleCLI(string appPath, string ruleName)
    {
        string command = $"advfirewall firewall add rule name=\"{ruleName}\" " +
                         $"dir=out action=allow program=\"{appPath}\" enable=yes";

        ProcessStartInfo psi = new ProcessStartInfo("netsh", command)
        {
            Verb = "runas", // Run as administrator
            CreateNoWindow = true,
            UseShellExecute = true
        };

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Failed to add outbound rule: {ex.Message}");
        }
    }

    public static void RemoveRuleCLI(string ruleName)
    {
        string command = $"advfirewall firewall delete rule name=\"{ruleName}\"";
        ProcessStartInfo psi = new ProcessStartInfo("netsh", command)
        {
            Verb = "runas", // Run as administrator
            CreateNoWindow = true,
            UseShellExecute = true
        };

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Failed to remove rule: {ex.Message}");
        }
    }

    public static void RunTest()
    {
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        FirewallHelper.AddInboundFirewallRuleCLI(exePath, $"{App.GetCurrentAssemblyName()} Inbound Rule");
    }
    #endregion

    #region [PowerShell Version]
    public static void AddInboundFirewallRulePS(string appPath, string displayName)
    {
        string script = $"New-NetFirewallRule -DisplayName \"{displayName}\" " +
                        $"-Direction Inbound -Program \"{appPath}\" -Action Allow";

        ProcessStartInfo psi = new ProcessStartInfo("powershell", $"-Command \"{script}\"")
        {
            Verb = "runas",
            CreateNoWindow = true,
            UseShellExecute = true
        };

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Failed to add outbound rule: {ex.Message}");
        }
    }

    public static void AddOutboundFirewallRulePS(string appPath, string displayName)
    {
        string script = $"New-NetFirewallRule -DisplayName \"{displayName}\" " +
                $"-Direction Outbound -Program \"{appPath}\" -Action Allow";

        ProcessStartInfo psi = new ProcessStartInfo("powershell", $"-Command \"{script}\"")
        {
            Verb = "runas",
            CreateNoWindow = true,
            UseShellExecute = true
        };

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Failed to add inbound rule: {ex.Message}");
        }
    }

    public static void RunTestPS()
    {
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        FirewallHelper.AddInboundFirewallRulePS(exePath, $"{App.GetCurrentAssemblyName()} Inbound Rule");
    }
    #endregion
}
