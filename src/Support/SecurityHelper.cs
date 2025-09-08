using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace AppRestorer
{
    [Flags]
    public enum SECURITY_INFORMATION : uint
    {
        OWNER_SECURITY_INFORMATION = 0x00000001,
        GROUP_SECURITY_INFORMATION = 0x00000002,
        DACL_SECURITY_INFORMATION = 0x00000004,
        SACL_SECURITY_INFORMATION = 0x00000008
    }

    /// <summary>
    ///  An access control list (ACL) is a list of access control entries (ACE). 
    ///  Each ACE in an ACL identifies a trustee and specifies the access rights allowed, denied, or audited for that trustee. 
    ///  The security descriptor for a securable object can contain two types of ACLs: a DACL and an SACL.
    ///  ACL  = Access Control List
    ///  ACE  = Access Control Entry
    ///  DACL = Discretionary Access Control List
    ///  SACL = System Access Control List
    /// </summary>
    /// <remarks>
    ///  https://learn.microsoft.com/en-us/windows/win32/secauthz/access-control-lists
    /// </remarks>
    public static class SecurityHelper
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetFileSecurity(string lpFileName, SECURITY_INFORMATION SecurityInformation, IntPtr pSecurityDescriptor);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetFileSecurity(string lpFileName, SECURITY_INFORMATION RequestedInformation, IntPtr pSecurityDescriptor, uint nLength, out uint lpnLengthNeeded);

        /// <summary>
        /// Grant everyone full control (DACL change)
        /// </summary>
        public static void Test(string filePath = @"C:\Temp\Test.txt")
        {
            // Build a FileSecurity object and add "Everyone" full control to a file's DACL (Discretionary Access Control List)
            var fs = new FileSecurity();
            fs.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.FullControl,
                AccessControlType.Allow));

            // Get the raw security descriptor bytes
            byte[] sdBytes = fs.GetSecurityDescriptorBinaryForm();

            // Allocate unmanaged memory and copy the SD
            IntPtr pSD = Marshal.AllocHGlobal(sdBytes.Length);
            try
            {
                Marshal.Copy(sdBytes, 0, pSD, sdBytes.Length);
                // Call SetFileSecurity
                bool ok = SetFileSecurity(filePath, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, pSD);
                if (!ok)
                {
                    int err = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"[WARNING] SetFileSecurity failed: {err}");
                }
                else
                {
                    Debug.WriteLine("[INFO] Security updated successfully.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pSD);
            }

        }

        public static byte[] ReadSecurityDescriptor(string path, SECURITY_INFORMATION info)
        {
            // First call to get required buffer size
            GetFileSecurity(path, info, IntPtr.Zero, 0, out uint needed);
            if (needed == 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            IntPtr pSD = Marshal.AllocHGlobal((int)needed);
            try
            {
                if (!GetFileSecurity(path, info, pSD, needed, out needed))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

                // Copy unmanaged SD to managed byte array
                byte[] sdBytes = new byte[needed];
                Marshal.Copy(pSD, sdBytes, 0, (int)needed);
                return sdBytes;
            }
            finally
            {
                Marshal.FreeHGlobal(pSD);
            }
        }
    }

    /// <summary>
    /// Unified UpdateSecurityDescriptor Helper
    /// </summary>
    public static class UnifiedSecurityHelper
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileSecurity(
            string lpFileName,
            SECURITY_INFORMATION RequestedInformation,
            IntPtr pSecurityDescriptor,
            uint nLength,
            out uint lpnLengthNeeded
        );

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileSecurity(
            string lpFileName,
            SECURITY_INFORMATION SecurityInformation,
            IntPtr pSecurityDescriptor
        );

        /// <summary>
        /// Reads, modifies, and writes back a file's security descriptor.
        /// </summary>
        public static void UpdateSecurityDescriptor(string path, SECURITY_INFORMATION info, Action<FileSecurity> modifyAction)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            if (modifyAction == null)
                throw new ArgumentNullException(nameof(modifyAction));

            // 1️ Get current SD size
            GetFileSecurity(path, info, IntPtr.Zero, 0, out uint needed);
            if (needed == 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            // 2️ Allocate and fetch SD
            IntPtr pSD = Marshal.AllocHGlobal((int)needed);
            try
            {
                if (!GetFileSecurity(path, info, pSD, needed, out needed))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

                // 3️ Convert to managed FileSecurity
                byte[] sdBytes = new byte[needed];
                Marshal.Copy(pSD, sdBytes, 0, (int)needed);
                var fs = new FileSecurity();
                fs.SetSecurityDescriptorBinaryForm(sdBytes);

                // 4️ Let caller modify it
                modifyAction(fs);

                // 5️ Convert back to unmanaged
                byte[] newSdBytes = fs.GetSecurityDescriptorBinaryForm();
                Marshal.FreeHGlobal(pSD);
                pSD = Marshal.AllocHGlobal(newSdBytes.Length);
                Marshal.Copy(newSdBytes, 0, pSD, newSdBytes.Length);

                // 6️ Write back
                if (!SetFileSecurity(path, info, pSD))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                Marshal.FreeHGlobal(pSD);
            }
        }

        public static void RunTests(string filePath = @"C:\Temp\Test.txt")
        {
            UpdateSecurityDescriptor(filePath, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, fs =>
            {
                fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            });

            // Add "Everyone" full control to a file's DACL
            UnifiedSecurityHelper.UpdateSecurityDescriptor(@"C:\Temp\Test.txt", SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                fs =>
                {
                    fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
                });

            Debug.WriteLine("[INFO] DACL updated successfully.");
        }
    }
}
