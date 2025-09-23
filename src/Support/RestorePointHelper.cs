using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AppRestorer;

/// <summary>
/// May need to add admin privileges to the app's manifest:
/// <code>
///   <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
/// </code>
/// </summary>
public static class RestorePointHelper
{
    public static bool CreateRestorePoint(string description)
    {
        try
        {
            ManagementClass srClass = new ManagementClass("//./root/default:SystemRestore");
            ManagementBaseObject inParams = srClass.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = description;
            inParams["RestorePointType"] = 0;   // APPLICATION_INSTALL
            inParams["EventType"] = 100;        // BEGIN_SYSTEM_CHANGE
            ManagementBaseObject outParams = srClass.InvokeMethod("CreateRestorePoint", inParams, null);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Restore point creation failed: {ex.Message}");
            return false;
        }
    }

    public static bool RunTest()
    {
        return RestorePointHelper.CreateRestorePoint($"Restore point before {App.GetCurrentAssemblyName()} changes.");
    }
}

/// <summary>
/// Without the need for <see cref="System.Management.ManagementClass"/>.
/// </summary>
public static class InteropRestorePoint
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct RESTOREPOINTINFO
    {
        public int dwEventType;       // 100 = BEGIN_SYSTEM_CHANGE
        public int dwRestorePtType;   // 0 = APPLICATION_INSTALL
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STATEMGRSTATUS
    {
        public int nStatus;
        public long llSequenceNumber;
    }

    [DllImport("srclient.dll", CharSet = CharSet.Unicode)]
    static extern bool SRSetRestorePoint(ref RESTOREPOINTINFO pRestorePtSpec, out STATEMGRSTATUS pSMgrStatus);

    public static bool CreateRestorePoint(string description)
    {
        RESTOREPOINTINFO rpInfo = new RESTOREPOINTINFO
        {
            dwEventType = 100, // BEGIN_SYSTEM_CHANGE
            dwRestorePtType = 0, // APPLICATION_INSTALL
            llSequenceNumber = 0,
            szDescription = description
        };

        STATEMGRSTATUS status;
        bool result = SRSetRestorePoint(ref rpInfo, out status);

        return result && status.nStatus == 0;
    }

    public static bool RunTest()
    {
        return InteropRestorePoint.CreateRestorePoint($"Restore point before {App.GetCurrentAssemblyName()} changes.");
    }
}
