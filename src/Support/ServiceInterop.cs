using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Con = System.Diagnostics.Debug;

namespace AppRestorer;

/// <summary>
/// https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager
/// </summary>
public static class ServiceInterop
{
    #region [Service Flags]
    [Flags]
    public enum SERVICE_ACCESS : uint
    {
        STANDARD_RIGHTS_REQUIRED = 0xF0000,
        SERVICE_QUERY_CONFIG = 0x00001,
        SERVICE_CHANGE_CONFIG = 0x00002,
        SERVICE_QUERY_STATUS = 0x00004,
        SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
        SERVICE_START = 0x00010,
        SERVICE_STOP = 0x00020,
        SERVICE_PAUSE_CONTINUE = 0x00040,
        SERVICE_INTERROGATE = 0x00080,
        SERVICE_USER_DEFINED_CONTROL = 0x00100,
        SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
                             SERVICE_QUERY_CONFIG |
                             SERVICE_CHANGE_CONFIG |
                             SERVICE_QUERY_STATUS |
                             SERVICE_ENUMERATE_DEPENDENTS |
                             SERVICE_START |
                             SERVICE_STOP |
                             SERVICE_PAUSE_CONTINUE |
                             SERVICE_INTERROGATE |
                             SERVICE_USER_DEFINED_CONTROL)
    }

    [Flags]
    public enum SCM_ACCESS : uint
    {
        STANDARD_RIGHTS_REQUIRED = 0xF0000,
        SC_MANAGER_CONNECT = 0x00001,
        SC_MANAGER_CREATE_SERVICE = 0x00002,
        SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
        SC_MANAGER_LOCK = 0x00008,
        SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
        SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
        SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
                                SC_MANAGER_CONNECT |
                                SC_MANAGER_CREATE_SERVICE |
                                SC_MANAGER_ENUMERATE_SERVICE |
                                SC_MANAGER_LOCK |
                                SC_MANAGER_QUERY_LOCK_STATUS |
                                SC_MANAGER_MODIFY_BOOT_CONFIG
    }

    [Flags]
    public enum SERVICE_CONTROL : uint
    {
        STOP = 0x00000001,
        PAUSE = 0x00000002,
        CONTINUE = 0x00000003,
        INTERROGATE = 0x00000004,
        SHUTDOWN = 0x00000005,
        PARAMCHANGE = 0x00000006,
        NETBINDADD = 0x00000007,
        NETBINDREMOVE = 0x00000008,
        NETBINDENABLE = 0x00000009,
        NETBINDDISABLE = 0x0000000A,
        DEVICEEVENT = 0x0000000B,
        HARDWAREPROFILECHANGE = 0x0000000C,
        POWEREVENT = 0x0000000D,
        SESSIONCHANGE = 0x0000000E
    }

    public enum SERVICE_STATE : uint
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007
    }

    [Flags]
    public enum SERVICE_ACCEPT : uint
    {
        STOP = 0x00000001,
        PAUSE_CONTINUE = 0x00000002,
        SHUTDOWN = 0x00000004,
        PARAMCHANGE = 0x00000008,
        NETBINDCHANGE = 0x00000010,
        HARDWAREPROFILECHANGE = 0x00000020,
        POWEREVENT = 0x00000040,
        SESSIONCHANGE = 0x00000080,
    }

    [Flags]
    public enum SERVICE_TYPE : int
    {
        SERVICE_KERNEL_DRIVER = 0x00000001,
        SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,
        SERVICE_WIN32_OWN_PROCESS = 0x00000010,
        SERVICE_WIN32_SHARE_PROCESS = 0x00000020,
        SERVICE_INTERACTIVE_PROCESS = 0x00000100
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SERVICE_STATUS
    {
        public SERVICE_TYPE ServiceType;
        public SERVICE_STATE CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
    }
    #endregion

    #region [PInvokes]
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteService(IntPtr hService);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ControlService(IntPtr hService, SERVICE_CONTROL dwControl, ref SERVICE_STATUS lpServiceStatus);

    [DllImport("advapi32.dll", EntryPoint = "QueryServiceStatus", CharSet = CharSet.Auto)]
    internal static extern bool QueryServiceStatus(IntPtr hService, ref SERVICE_STATUS dwServiceStatus);

    [DllImport("advapi32.dll")]
    internal static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS lpServiceStatus);
    #endregion

    #region [Public Methods]
    public static void CheckService(string serviceName, bool stopIfRunning = false, string? machineName = null)
    {
        try
        {
            IntPtr schSCManager = OpenSCManager(machineName ?? Environment.MachineName, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (schSCManager != IntPtr.Zero)
            {
                bool isStopped = false; bool isRunning = false; bool isPaused = false; bool isPending = false;
                IntPtr schService = OpenService(schSCManager, serviceName, SERVICE_ACCESS.SERVICE_ALL_ACCESS);
                if (schService != IntPtr.Zero)
                {
                    SERVICE_STATUS stat = new SERVICE_STATUS();
                    bool res = QueryServiceStatus(schService, ref stat);
                    if (res)
                    {
                        isStopped = (stat.CurrentState & SERVICE_STATE.SERVICE_STOPPED) != 0;
                        isRunning = (stat.CurrentState & SERVICE_STATE.SERVICE_RUNNING) != 0;
                        isPaused = (stat.CurrentState & SERVICE_STATE.SERVICE_PAUSED) != 0; // reported true when service was running?
                        isPending = (stat.CurrentState & SERVICE_STATE.SERVICE_START_PENDING) != 0 || (stat.CurrentState & SERVICE_STATE.SERVICE_STOP_PENDING) != 0;
                        var tally = string.Format("ServiceName:{0}\r\n isStopped:{1}\r\n isRunning:{2}\r\n isPaused:{3}\r\n isPending:{4}", serviceName, isStopped, isRunning, isPaused, isPending);
                        Con.WriteLine($"[INFO] {tally}");
                    }
                    else
                    {
                        Con.WriteLine("[WARNING] QueryServiceStatus returned false");
                    }
                }
                CloseServiceHandle(schSCManager);
                // If you don't close this handle, Services control panel shows the service as
                // "disabled", and you'll get 1072 error trying to reuse this service's name.
                CloseServiceHandle(schService);

                if (isRunning && stopIfRunning)
                {
                    App.RootEventBus?.Publish(Constants.EB_ToWindow, $"Stopping service {serviceName}");
                    Extensions.WriteToLog($"Stopping service '{serviceName}'");
                    StopService(serviceName, machineName);
                }
            }
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"CheckService: {ex.Message}");
        }
    }

    public static void StopService(string serviceName, string? machineName = null)
    {
        try
        {
            IntPtr schSCManager = OpenSCManager(machineName ?? Environment.MachineName, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (schSCManager != IntPtr.Zero)
            {
                IntPtr schService = OpenService(schSCManager, serviceName, SERVICE_ACCESS.SERVICE_ALL_ACCESS);
                if (schService != IntPtr.Zero)
                {
                    SERVICE_STATUS stat = new SERVICE_STATUS();
                    bool res = ControlService(schService, SERVICE_CONTROL.STOP, ref stat);
                    if (res)
                    {
                        // TODO: Code to halt execution until the service has finally stopped, to continue another task afterwards.
                        Thread.Sleep(3000);
                        bool isStopped = (stat.CurrentState & SERVICE_STATE.SERVICE_STOPPED) != 0;
                        bool isRunning = (stat.CurrentState & SERVICE_STATE.SERVICE_RUNNING) != 0;
                        bool isPaused = (stat.CurrentState & SERVICE_STATE.SERVICE_PAUSED) != 0;
                        bool isPending = (stat.CurrentState & SERVICE_STATE.SERVICE_START_PENDING) != 0 || (stat.CurrentState & SERVICE_STATE.SERVICE_STOP_PENDING) != 0;
                        var tally = string.Format(" isStopped:{0}\r\n isRunning:{1}\r\n isPaused:{2}\r\n isPending:{3}", isStopped, isRunning, isPaused, isPending);
                        Con.WriteLine($"[INFO] {tally}");
                    }
                    else
                    {
                        Con.WriteLine("[WARNING] ControlService returned false");
                    }
                }
                CloseServiceHandle(schSCManager);
                // If you don't close this handle, Services control panel shows the service as
                // "disabled", and you'll get a 1072 error trying to reuse this service's name.
                CloseServiceHandle(schService);
            }
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"StopService: {ex.Message}");
        }
    }

    public static void StartService(string serviceName, string? machineName = null)
    {
        try
        {
            IntPtr schSCManager = OpenSCManager(machineName ?? Environment.MachineName, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (schSCManager != IntPtr.Zero)
            {
                IntPtr schService = OpenService(schSCManager, serviceName, SERVICE_ACCESS.SERVICE_ALL_ACCESS);
                if (schService != IntPtr.Zero)
                {
                    if (StartService(schSCManager, 0, null) == false)
                    {
                        Con.WriteLine(string.Format("StartService failed {0}", Marshal.GetLastWin32Error()));
                    }
                }
                CloseServiceHandle(schSCManager);
                // If you don't close this handle, Services control panel shows the service as
                // "disabled", and you'll get a 1072 error trying to reuse this service's name.
                CloseServiceHandle(schService);
            }
            else
            {
                Con.WriteLine($"[WARNING] The service could not be opened!");
            }
        }
        catch (System.Exception ex)
        {
            Extensions.WriteToLog($"StartService: {ex.Message}");
        }
    }

    public static void DeleteService(string serviceName, string? machineName = null)
    {
        try
        {
            IntPtr schSCManager = OpenSCManager(machineName ?? Environment.MachineName, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (schSCManager != IntPtr.Zero)
            {
                IntPtr schService = OpenService(schSCManager, serviceName, SERVICE_ACCESS.SERVICE_ALL_ACCESS);
                if (schService != IntPtr.Zero)
                {
                    if (DeleteService(schService) == false)
                    {
                        Con.WriteLine(string.Format("DeleteService failed {0}", Marshal.GetLastWin32Error()));
                    }
                }
                CloseServiceHandle(schSCManager);
                // If you don't close this handle, Services control panel shows the service as
                // "disabled", and you'll get a 1072 error trying to reuse this service's name.
                CloseServiceHandle(schService);
            }
            else
            {
                Con.WriteLine("[WARNING] The service could not be opened!");
            }
        }
        catch (System.Exception ex)
        {
            Extensions.WriteToLog($"DeleteService: {ex.Message}");
        }
    }
    #endregion
}


/// <summary>
/// Lightweight service interop helpers using advapi32 functions.
/// Use the disposable handle wrappers to ensure handles are closed.
/// This is my revamped version of <see cref="ServiceInterop"/>.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager
/// </remarks>
public static class ServiceInteropHelper
{
    /* [NOTES]
      During system boot, the SCM starts all auto-start services and the services on which they depend. 
      For example, if an auto-start service depends on a demand-start service, the demand-start service 
      is also started automatically.
      
      The load order is determined by the following:
      
          The order of groups in the load ordering group list. This information is stored in the List value (string) in the following registry key:
      
          HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\ServiceGroupOrder
      
          To specify the load ordering group for a service, use the lpLoadOrderGroup parameter of the CreateService or ChangeServiceConfig function.
      
          The order of services within a group specified in the tags order vector. This information is stored in the following registry key:
      
          HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\GroupOrderList
      
          The dependencies listed for each service.
      
      When the boot is complete, the system executes the boot verification program specified by the 
      ImagePath value of the following registry key: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\BootVerificationProgram.
      
      By default, this value is not set. The system simply reports that the boot was successful after 
      the first user has logged on. You can supply a boot verification program that checks the system 
      for problems and reports the boot status to the SCM using the NotifyBootConfigStatus function.
      
      After a successful boot, the system saves a clone of the database in the last-known-good (LKG) 
      configuration. The system can restore this copy of the database if changes made to the active 
      database cause the system reboot to fail. The following is the registry key for this database:
      
      HKEY_LOCAL_MACHINE\SYSTEM\ControlSetXXX\Services
      
      where XXX is the value saved in the following registry value: HKEY_LOCAL_MACHINE\System\Select\LastKnownGood.
      
      If an auto-start service with a SERVICE_ERROR_CRITICAL error control level fails to start, the 
      SCM reboots the computer using the LKG configuration. If the LKG configuration is already being 
      used, the boot fails.
      
      An auto-start service can be configured as a delayed auto-start service by calling the 
      ChangeServiceConfig2 function with SERVICE_CONFIG_DELAYED_AUTO_START_INFO. This change takes 
      effect after the next system boot. For more information, see SERVICE_DELAYED_AUTO_START_INFO.
    */

    #region [PInvokes]
    /*
      The Win32 API provides two versions for many functions that handle string parameters: 
      an "A" version for ANSI strings and a "W" version for Unicode (wide character) strings.
    */

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-startservicew
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool StartService(
        IntPtr hService, 
        int dwNumServiceArgs, 
        string[] lpServiceArgVectors);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-deleteservice
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteService(
        IntPtr hService);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-openservicew
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr OpenService(
        IntPtr hSCManager, 
        string lpServiceName, 
        SERVICE_ACCESS dwDesiredAccess);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-openscmanagerw
    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr OpenSCManager(
        string machineName, 
        string databaseName, 
        SCM_ACCESS dwDesiredAccess);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-closeservicehandle
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseServiceHandle(
        IntPtr hSCObject);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-controlservice
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ControlService(
        IntPtr hService, 
        SERVICE_CONTROL dwControl, 
        ref SERVICE_STATUS lpServiceStatus);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-queryservicestatus
    [DllImport("advapi32.dll", EntryPoint = "QueryServiceStatus", CharSet = CharSet.Auto)]
    internal static extern bool QueryServiceStatus(
        IntPtr hService, 
        ref SERVICE_STATUS dwServiceStatus);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-queryserviceconfig2w
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryServiceConfig2(
        IntPtr hService,
        uint dwInfoLevel,
        IntPtr lpBuffer,
        uint cbBufSize,
        out uint pcbBytesNeeded);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-setservicestatus
    [DllImport("advapi32.dll")]
    internal static extern bool SetServiceStatus(
        IntPtr hServiceStatus, 
        ref SERVICE_STATUS lpServiceStatus);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-createservicew
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateService(
        IntPtr hSCManager,
        string lpServiceName,
        string lpDisplayName,
        SERVICE_ACCESS dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string lpDependencies,
        string lpServiceStartName,
        string lpPassword);

    /// <summary>
    /// An extended version of ControlService, allowing you to send control codes (stop, pause, continue, interrogate, user-defined) with additional info.
    /// </summary>
    /// <remarks>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-controlserviceexw
    /// </remarks>
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ControlServiceExW(
        IntPtr hService,
        SERVICE_CONTROL dwControl,
        uint dwInfoLevel,
        ref SERVICE_STATUS_PROCESS lpServiceStatus);


    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfig2w
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool ChangeServiceConfig(
        IntPtr hService,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string lpDependencies,
        string lpServiceStartName,
        string lpPassword,
        string lpDisplayName);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfig2w
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ChangeServiceConfig2(
        IntPtr hService,
        uint dwInfoLevel,
        ref SERVICE_DELAYED_AUTO_START_INFO lpInfo);

    // https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-queryserviceconfigw
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool QueryServiceConfig(
        IntPtr hService,
        IntPtr lpServiceConfig,
        int cbBufSize,
        out int pcbBytesNeeded);

    #endregion

    #region [Structs & Enums]

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public SERVICE_STATE dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
        public uint dwProcessId;
        public uint dwServiceFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_DELAYED_AUTO_START_INFO
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool fDelayedAutostart;
    }

    [Flags]
    public enum SCM_ACCESS : uint
    {
        SC_MANAGER_CONNECT = 0x0001,
        SC_MANAGER_CREATE_SERVICE = 0x0002,
        SC_MANAGER_ENUMERATE_SERVICE = 0x0004,
        SC_MANAGER_LOCK = 0x0008,
        SC_MANAGER_QUERY_LOCK_STATUS = 0x0010,
        SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020,
        SC_MANAGER_ALL_ACCESS = 0xF003F
    }

    [Flags]
    public enum SERVICE_ACCESS : uint
    {
        SERVICE_QUERY_CONFIG = 0x0001,
        SERVICE_CHANGE_CONFIG = 0x0002,
        SERVICE_QUERY_STATUS = 0x0004,
        SERVICE_ENUMERATE_DEPENDENTS = 0x0008,
        SERVICE_START = 0x0010,
        SERVICE_STOP = 0x0020,
        SERVICE_PAUSE_CONTINUE = 0x0040,
        SERVICE_INTERROGATE = 0x0080,
        SERVICE_USER_DEFINED_CONTROL = 0x0100,
        SERVICE_ALL_ACCESS = 0xF01FF
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-controlservice
    /// </summary>
    public enum SERVICE_CONTROL : uint
    {
        SERVICE_CONTROL_STOP = 0x00000001,
        SERVICE_CONTROL_PAUSE = 0x00000002,
        SERVICE_CONTROL_CONTINUE = 0x00000003,
        SERVICE_CONTROL_INTERROGATE = 0x00000004,
        SERVICE_CONTROL_SHUTDOWN = 0x00000005,
        SERVICE_CONTROL_PARAMCHANGE = 0x00000006,
        // custom user-defined controls 128-255
    }

    public enum SERVICE_STATE : uint
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct QUERY_SERVICE_CONFIG
    {
        public uint dwServiceType;
        public uint dwStartType;
        public uint dwErrorControl;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpBinaryPathName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpLoadOrderGroup;
        public uint dwTagID;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDependencies;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpServiceStartName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDisplayName;
    }

    // Start types
    public const uint SERVICE_BOOT_START = 0x00000000;
    public const uint SERVICE_SYSTEM_START = 0x00000001;
    public const uint SERVICE_AUTO_START = 0x00000002;
    public const uint SERVICE_DEMAND_START = 0x00000003;
    public const uint SERVICE_DISABLED = 0x00000004;

    // Error control
    public const uint SERVICE_ERROR_IGNORE = 0x00000000;
    public const uint SERVICE_ERROR_NORMAL = 0x00000001;
    public const uint SERVICE_ERROR_SEVERE = 0x00000002;
    public const uint SERVICE_ERROR_CRITICAL = 0x00000003;

    // Service types
    public const uint SERVICE_KERNEL_DRIVER = 0x00000001;
    public const uint SERVICE_FILE_SYSTEM_DRIVER = 0x00000002;
    public const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
    public const uint SERVICE_WIN32_SHARE_PROCESS = 0x00000020;
    // If you specify either SERVICE_WIN32_OWN_PROCESS or
    // SERVICE_WIN32_SHARE_PROCESS, and the service is running
    // in the context of the LocalSystem account, you can also
    // specify the following value:
    public const uint SERVICE_INTERACTIVE_PROCESS = 0x00000100;
    // The LocalSystem account is a predefined local account used by
    // the Service Control Manager. A service that runs in the context
    // of the LocalSystem account inherits the security context of the
    // SCM. The user SID is created from the SECURITY_LOCAL_SYSTEM_RID
    // value. The account is not associated with any logged-on user account.
    // https://learn.microsoft.com/en-us/windows/win32/services/localsystem-account

    // Service config
    public const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;

    // Info levels for ControlServiceExW
    public const uint SERVICE_CONTROL_STATUS_REASON_INFO = 1;

    // For ChangeServiceConfig2
    public const uint SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;

    #endregion

    #region [Disposable Handle Wrappers]

    /// <summary>
    /// Disposable wrapper for Service Control Manager handle.
    /// </summary>
    public sealed class ServiceManagerHandle : IDisposable
    {
        public IntPtr Handle { get; }
        internal ServiceManagerHandle(IntPtr h) => Handle = h;
        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                CloseServiceHandle(Handle);

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Disposable wrapper for service handle.
    /// </summary>
    public sealed class ServiceHandle : IDisposable
    {
        public IntPtr Handle { get; }
        internal ServiceHandle(IntPtr h) => Handle = h;
        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                CloseServiceHandle(Handle);

            GC.SuppressFinalize(this);
        }
    }

    #endregion

    #region [Public Methods]

    /// <summary>
    /// Open Service Control Manager on local machine with the requested access.
    /// </summary>
    public static ServiceManagerHandle OpenSCManagerLocal(SCM_ACCESS desiredAccess = SCM_ACCESS.SC_MANAGER_ALL_ACCESS)
    {
        var h = OpenSCManager(null, null, desiredAccess);
        if (h == IntPtr.Zero)
            ThrowLastWin32("OpenSCManager");
        return new ServiceManagerHandle(h);
    }

    /// <summary>
    /// Open a handle to a service by name.
    /// </summary>
    public static ServiceHandle OpenServiceHandle(ServiceManagerHandle scm, string serviceName, SERVICE_ACCESS desiredAccess = SERVICE_ACCESS.SERVICE_ALL_ACCESS)
    {
        if (scm == null) 
            throw new ArgumentNullException(nameof(scm));
        var h = OpenService(scm.Handle, serviceName, desiredAccess);
        if (h == IntPtr.Zero)
            ThrowLastWin32($"OpenService ({serviceName})");
        return new ServiceHandle(h);
    }

    /// <summary>
    /// Start a service. Optionally pass args.
    /// </summary>
    public static void StartService(ServiceHandle svcHandle, string[] args = null, int timeoutSeconds = 30)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));
        var success = StartService(svcHandle.Handle, args?.Length ?? 0, args);
        if (!success)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"StartService failed (error={err})");
        }

        // wait for running
        WaitForStatus(svcHandle, SERVICE_STATE.SERVICE_RUNNING, TimeSpan.FromSeconds(timeoutSeconds));
    }

    /// <summary>
    /// Stop a service (sends SERVICE_CONTROL_STOP) and waits for stopped.
    /// </summary>
    public static void StopService(ServiceHandle svcHandle, int timeoutSeconds = 30)
    {
        if (svcHandle == null) throw new ArgumentNullException(nameof(svcHandle));

        var status = new SERVICE_STATUS();
        bool sent = ControlService(svcHandle.Handle, SERVICE_CONTROL.SERVICE_CONTROL_STOP, ref status);
        if (!sent)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"ControlService(STOP) failed (error={err})");
        }

        WaitForStatus(svcHandle, SERVICE_STATE.SERVICE_STOPPED, TimeSpan.FromSeconds(timeoutSeconds));
    }

    /// <summary>
    /// Query the service's current status.
    /// </summary>
    public static SERVICE_STATUS QueryStatus(ServiceHandle svcHandle)
    {
        if (svcHandle == null) throw new ArgumentNullException(nameof(svcHandle));
        var ss = new SERVICE_STATUS();
        bool ok = QueryServiceStatus(svcHandle.Handle, ref ss);
        if (!ok)
        {
            int err = Marshal.GetLastWin32Error(); // typically ERROR_ACCESS_DENIED or ERROR_INVALID_HANDLE
            throw new InvalidOperationException($"QueryServiceStatus failed (error={err})");
        }
        return ss;
    }

    /// <summary>
    /// Delete the service from the registry (service must be stopped).
    /// </summary>
    public static void DeleteService(ServiceHandle svcHandle)
    {
        if (svcHandle == null) throw new ArgumentNullException(nameof(svcHandle));
        if (!DeleteService(svcHandle.Handle))
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"DeleteService failed (error={err})");
        }
    }

    /// <summary>
    /// Controls the service with an arbitrary control code and returns updated status.
    /// </summary>
    public static SERVICE_STATUS ControlServiceEx(ServiceHandle svcHandle, SERVICE_CONTROL control)
    {
        if (svcHandle == null) throw new ArgumentNullException(nameof(svcHandle));
        var status = new SERVICE_STATUS();
        bool ok = ControlService(svcHandle.Handle, control, ref status);
        if (!ok)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"ControlService failed (error={err})");
        }
        return status;
    }

    /// <summary>
    /// Wait until the service reaches the <paramref name="desiredState"/> or the <paramref name="timeout"/> expires.
    /// Polls QueryServiceStatus.
    /// </summary>
    public static void WaitForStatus(ServiceHandle svcHandle, SERVICE_STATE desiredState, TimeSpan timeout)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));
        
        const int pollIntervalMs = 500;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var ss = QueryStatus(svcHandle);
            if (ss.dwCurrentState == desiredState) return;

            // if the service is stuck in start/stop pending, allow it to continue waiting until timeout
            Thread.Sleep(pollIntervalMs);
        }

        throw new TimeoutException($"Service did not reach state {desiredState} within {timeout.TotalSeconds} seconds.");
    }

    /// <summary>
    /// Returns true if the service is currently running.
    /// </summary>
    public static bool IsRunning(ServiceHandle svcHandle)
    {
        if (svcHandle == null)
            throw new ArgumentNullException(nameof(svcHandle));

        var ss = QueryStatus(svcHandle);
        return ss.dwCurrentState == SERVICE_STATE.SERVICE_RUNNING;
    }

    /// <summary>
    /// Creates/Installs a new service in the SCM database.
    /// </summary>
    /// <param name="scm">Service Control Manager handle</param>
    /// <param name="serviceName">Internal service name</param>
    /// <param name="displayName">Friendly display name</param>
    /// <param name="binaryPath">Path to the service executable</param>
    /// <param name="startType">Start mode (e.g. auto/manual/delayed)</param>
    /// <param name="serviceType">Service type (default: Win32 own process)</param>
    /// <param name="userName">Optional account name (null for LocalSystem)</param>
    /// <param name="password">Password if account requires it</param>
    public static ServiceHandle CreateServiceHandle(
        ServiceManagerHandle scm,
        string serviceName,
        string displayName,
        string binaryPath,
        uint startType = 2,            // SERVICE_AUTO_START
        uint serviceType = 0x00000010, // SERVICE_WIN32_OWN_PROCESS
        string userName = null,
        string password = null)
    {
        if (scm == null) 
            throw new ArgumentNullException(nameof(scm));

        var hSvc = CreateService(
            scm.Handle,
            serviceName,
            displayName,
            SERVICE_ACCESS.SERVICE_ALL_ACCESS,
            serviceType,
            startType,
            1,            // SERVICE_ERROR_NORMAL
            binaryPath,
            null,
            IntPtr.Zero,
            null,
            userName,
            password);

        if (hSvc == IntPtr.Zero)
            ThrowLastWin32($"CreateService({serviceName})");

        return new ServiceHandle(hSvc);
    }

    /// <summary>
    /// Updates configuration of an existing service.
    /// Pass null for values you don't want to change.
    /// </summary>
    /// <param name="svcHandle">Handle to the open service</param>
    /// <param name="serviceType">New service type (use SERVICE_NO_CHANGE to keep existing)</param>
    /// <param name="startType">New start type (SERVICE_AUTO_START, etc., or SERVICE_NO_CHANGE)</param>
    /// <param name="errorControl">Error control level (or SERVICE_NO_CHANGE)</param>
    /// <param name="binaryPath">New binary path, or null</param>
    /// <param name="startName">User account to run as, or null for no change</param>
    /// <param name="password">Password for account if required</param>
    /// <param name="displayName">New display name, or null</param>
    public static void ChangeServiceConfigHandle(
        ServiceHandle svcHandle,
        uint serviceType,
        uint startType,
        uint errorControl,
        string binaryPath,
        string startName,
        string password,
        string displayName)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));

        bool ok = ChangeServiceConfig(
            svcHandle.Handle,
            serviceType,
            startType,
            errorControl,
            binaryPath,
            null,
            IntPtr.Zero,
            null,
            startName,
            password,
            displayName);

        if (!ok)
            ThrowLastWin32("ChangeServiceConfig");
    }

    /// <summary>
    /// Sends a control code to a service with extended info.
    /// </summary>
    /// <param name="svcHandle">Handle to the service</param>
    /// <param name="control">Control code (stop, pause, interrogate, user-defined)</param>
    /// <returns>The updated service status process</returns>
    public static SERVICE_STATUS_PROCESS ControlServiceExHandle(ServiceHandle svcHandle, SERVICE_CONTROL control)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));

        var status = new SERVICE_STATUS_PROCESS();

        bool ok = ControlServiceExW(
            svcHandle.Handle,
            control,
            SERVICE_CONTROL_STATUS_REASON_INFO,
            ref status);

        if (!ok)
            ThrowLastWin32($"ControlServiceExW({control})");

        return status;
    }

    /// <summary>
    /// Reads the current configuration of a service.
    /// </summary>
    public static QUERY_SERVICE_CONFIG QueryServiceConfigHandle(ServiceHandle svcHandle)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));

        int bytesNeeded;

        // First call with zero buffer to get required size
        QueryServiceConfig(svcHandle.Handle, IntPtr.Zero, 0, out bytesNeeded);

        IntPtr ptr = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            if (!QueryServiceConfig(svcHandle.Handle, ptr, bytesNeeded, out bytesNeeded))
            {
                // Typically ERROR_ACCESS_DENIED, ERROR_INSUFFICIENT_BUFFER or ERROR_INVALID_HANDLE
                ThrowLastWin32("QueryServiceConfig");
            }

            var config = Marshal.PtrToStructure<QUERY_SERVICE_CONFIG>(ptr);
            return config;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// Sets the delayed auto-start flag for an automatic service.
    /// </summary>
    /// <param name="svcHandle">Handle to the service with SERVICE_CHANGE_CONFIG rights</param>
    /// <param name="enable">true = delayed auto-start, false = normal auto-start</param>
    public static void SetDelayedAutoStart(ServiceHandle svcHandle, bool enable)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));

        var info = new SERVICE_DELAYED_AUTO_START_INFO
        {
            fDelayedAutostart = enable
        };

        if (!ChangeServiceConfig2(svcHandle.Handle, SERVICE_CONFIG_DELAYED_AUTO_START_INFO, ref info))
        {
            ThrowLastWin32("ChangeServiceConfig2 (DelayedAutoStart)");
        }
    }

    /// <summary>
    /// Checks if a service is configured for delayed auto-start.
    /// </summary>
    public static bool GetDelayedAutoStart(ServiceHandle svcHandle)
    {
        if (svcHandle == null) 
            throw new ArgumentNullException(nameof(svcHandle));

        uint bytesNeeded;
        // First call to get buffer size
        QueryServiceConfig2(svcHandle.Handle, SERVICE_CONFIG_DELAYED_AUTO_START_INFO, IntPtr.Zero, 0, out bytesNeeded);

        if (bytesNeeded == 0)
            ThrowLastWin32("QueryServiceConfig2 (size)");

        IntPtr buffer = Marshal.AllocHGlobal((int)bytesNeeded);
        try
        {
            if (!QueryServiceConfig2(svcHandle.Handle, SERVICE_CONFIG_DELAYED_AUTO_START_INFO, buffer, bytesNeeded, out bytesNeeded))
                ThrowLastWin32("QueryServiceConfig2");

            var info = Marshal.PtrToStructure<SERVICE_DELAYED_AUTO_START_INFO>(buffer);
            return info.fDelayedAutostart;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    #endregion

    #region [Marshal Helpers]

    static void ThrowLastWin32(string op)
    {
        int err = Marshal.GetLastWin32Error();
        throw new InvalidOperationException($"{op} failed. Win32 error: {err}");
    }

    #endregion

    /// <summary>
    /// Testing method for new class.
    /// </summary>
    public static void RunTests()
    {
        // **** Example Usage (query an existing service config) ****
        using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
        {
            using (var svc = ServiceInteropHelper.OpenServiceHandle(
                scm,
                "BasicWindowService",
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_QUERY_CONFIG))
            {
                var config = ServiceInteropHelper.QueryServiceConfigHandle(svc);
                Con.WriteLine($"Service Type: {config.dwServiceType}");
                Con.WriteLine($"Start Type: {config.dwStartType}");
                Con.WriteLine($"Binary Path: {config.lpBinaryPathName}");
                Con.WriteLine($"Run As: {config.lpServiceStartName}");
                Con.WriteLine($"Display Name: {config.lpDisplayName}");
                /*
                  Service Type: 272 (SERVICE_WIN32_OWN_PROCESS + SERVICE_INTERACTIVE_PROCESS)
                  Start Type: 3
                  Binary Path: D:\source\repos\BasicWindowsService\Debug\CSWindowsService.exe
                  Run As: LocalSystem
                  Display Name: BasicWindowService
                */
            }
        }

        // **** Example checking for auto-start-delayed ****
        using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
        {
            using (var svc = ServiceInteropHelper.OpenServiceHandle(
                scm,
                "BasicWindowService",
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_QUERY_CONFIG))
            {
                bool isDelayed = ServiceInteropHelper.GetDelayedAutoStart(svc);
                Con.WriteLine(isDelayed ? "Service is Automatic (Delayed Start)" : "Service is not delayed");
            }
        }

        // **** Example change to auto-start-delayed ****
        using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
        {
            using (var svc = ServiceInteropHelper.OpenServiceHandle(
                scm,
                "BasicWindowService",
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG))
            {
                ServiceInteropHelper.SetDelayedAutoStart(svc, enable: true);
                Con.WriteLine("Service updated to Automatic (Delayed Start).");
            }
        }

        // **** Example Usage (check if running and then stop/start) ****
        using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
        {
            using (var svc = ServiceInteropHelper.OpenServiceHandle(
                scm, 
                "Spooler", 
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_QUERY_STATUS | 
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_START | 
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_STOP))
            {
                // Query status:
                if (!ServiceInteropHelper.IsRunning(svc))
                {
                    Con.WriteLine("Service not running, starting now...");
                    ServiceInteropHelper.StartService(svc, null, timeoutSeconds: 20);
                }
                else
                {
                    Con.WriteLine("Service is running, no need to start.");
                }

                // Stop:
                // ServiceInterop2.StopService(svc, timeoutSeconds: 20);

                // Delete:
                // ServiceInterop2.DeleteService(svc);
            }
        }

        // **** Example Usage (install service: admin privileges required) ****
        string binPath = @"D:\source\repos\BasicWindowsService\Debug\CSWindowsService.exe";
        if (System.IO.File.Exists(binPath))
        {
            using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
            {
                using (var svc = ServiceInteropHelper.CreateServiceHandle(
                    scm,
                    serviceName: "BasicWindowService",
                    displayName: "BasicWindowService",
                    binaryPath: binPath,
                    startType: ServiceInteropHelper.SERVICE_AUTO_START))
                {
                    Con.WriteLine("Service installed successfully.");

                    // Start it immediately
                    ServiceInteropHelper.StartService(svc);
                }
            }
        }

        // **** Example Usage (change service config) ****
        using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
        {
            using (var svc = ServiceInteropHelper.OpenServiceHandle(
                scm,
                "BasicWindowService", 
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG | 
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_QUERY_CONFIG))
            {
                // Change to Automatic (Delayed Start) and update display name
                ServiceInteropHelper.ChangeServiceConfigHandle(
                    svc,
                    ServiceInteropHelper.SERVICE_NO_CHANGE,
                    ServiceInteropHelper.SERVICE_AUTO_START,
                    ServiceInteropHelper.SERVICE_NO_CHANGE,
                    null,  // keep same binary path
                    null,  // keep same account
                    null,  // no password change
                    "Updated Display Name");

                Con.WriteLine("Service configuration updated.");
            }
        }

        // **** General use case of retrieving extended SERVICE_STATUS_PROCESS. ****
        using (var scm = ServiceInteropHelper.OpenSCManagerLocal())
        {
            using (var svc = ServiceInteropHelper.OpenServiceHandle(
                scm,
                "BasicWindowService",
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_STOP | 
                ServiceInteropHelper.SERVICE_ACCESS.SERVICE_INTERROGATE))
            {
                var status = ServiceInteropHelper.ControlServiceExHandle(svc, ServiceInteropHelper.SERVICE_CONTROL.SERVICE_CONTROL_INTERROGATE);
                Con.WriteLine($"Service state: {status.dwCurrentState}, PID: {status.dwProcessId}");
            }
        }
    }
}
