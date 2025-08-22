using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace AppRestorer
{
    /**
    △△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△△
     A more advanced approach to evaluate all start locations: task scheduler, registry, and startup folder.
    ▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽▽
    **/

    // ---------------- Data Model ----------------
    public sealed class StartupEntry
    {
        public string? Name { get; init; }       // Display name or value name
        public string? Command { get; init; }    // Command or resolved shortcut target
        public string? Source { get; init; }     // Registry | StartupFolder | TaskScheduler
        public string? Scope { get; init; }      // User | Machine | AnyUser | User:<sid/name>
        public string? Location { get; init; }   // Registry path, file path, or task path
        public bool? Enabled { get; init; }      // true/false when known; null if unknown
    }


    public static class StartupAnalyzer
    {
        /// <summary>
        /// Main method
        /// </summary>
        /// <returns><see cref="List{T}"/></returns>
        public static List<StartupEntry> GetAllStartupEntries()
        {
            var results = new List<StartupEntry>();

            #region [Registry: HKCU + HKLM (both 32/64 views)]
            CollectRunKeyEntries(RegistryHive.CurrentUser, RegistryView.Default, results);
            CollectRunKeyEntries(RegistryHive.LocalMachine, RegistryView.Registry64, results);
            CollectRunKeyEntries(RegistryHive.LocalMachine, RegistryView.Registry32, results);
            #endregion

            #region [Startup folders: user + common]
            var userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            CollectStartupFolder(userStartup, "User", RegistryHive.CurrentUser, results);
            var commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            CollectStartupFolder(commonStartup, "Machine", RegistryHive.LocalMachine, results);
            #endregion

            #region [Task Scheduler: logon-triggered tasks]
            CollectLogonTasks(results);
            #endregion

            return results;
        }

        /// <summary>
        /// Registry: Run keys
        /// </summary>
        static void CollectRunKeyEntries(RegistryHive hive, RegistryView view, List<StartupEntry> results)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var runKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                if (runKey == null) return;

                foreach (var valueName in runKey.GetValueNames())
                {
                    var kind = runKey.GetValueKind(valueName);
                    var raw = runKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (raw == null) continue;

                    var data = raw.ToString();
                    var expanded = kind == RegistryValueKind.ExpandString
                        ? Environment.ExpandEnvironmentVariables(data)
                        : data;

                    bool? enabled =
                        TryGetStartupApprovedEnabled(hive, "Run", valueName, view) ??
                        // Some systems may store only in the default 64-bit view
                        TryGetStartupApprovedEnabled(hive, "Run", valueName, RegistryView.Registry64) ??
                        TryGetStartupApprovedEnabled(hive, "Run", valueName, RegistryView.Registry32);

                    results.Add(new StartupEntry
                    {
                        Name = valueName,
                        Command = expanded,
                        Source = "Registry",
                        Scope = hive == RegistryHive.CurrentUser ? "User" : "Machine",
                        Location = $@"{baseKey.Name}\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                        Enabled = enabled
                    });
                }
            }
            catch
            {
                // Ignore keys we cannot read (permission or policy)
            }
        }

        /// <summary>
        /// Windows tracks enable/disable state for Startup items here:
        /// HKCU/HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\(Run|StartupFolder)
        /// Value data: first byte 0x02 => enabled, 0x03 => disabled (other values = unknown).
        /// </summary>
        /// <returns><c>true</c> if state is enabled, <c>false</c> otherwise</returns>
        static bool? TryGetStartupApprovedEnabled(RegistryHive hive, string bucket, string itemName, RegistryView view)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\{bucket}");
                if (key == null) return null;

                var data = key.GetValue(itemName) as byte[];
                if (data == null || data.Length == 0) return null;

                return data[0] switch
                {
                    2 => true,   // enabled
                    3 => false,  // disabled
                    _ => (bool?)null
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Startup extensions
        /// </summary>
        static readonly HashSet<string> StartupFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".lnk", 
            ".exe", 
            ".bat", 
            ".cmd", 
            ".ps1"
        };

        static void CollectStartupFolder(string folderPath, string scope, RegistryHive hiveForApproval, List<StartupEntry> results)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                    return;

                var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(p => StartupFileExtensions.Contains(Path.GetExtension(p)));

                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var cmd = Path.GetExtension(file).Equals(".lnk", StringComparison.OrdinalIgnoreCase)
                              ? TryResolveShortcut(file)
                              : $"\"{file}\"";

                    // StartupApproved uses file name for StartupFolder bucket
                    var valueName = Path.GetFileName(file);

                    bool? enabled =
                        TryGetStartupApprovedEnabled(hiveForApproval, "StartupFolder", valueName, RegistryView.Registry64) ??
                        TryGetStartupApprovedEnabled(hiveForApproval, "StartupFolder", valueName, RegistryView.Registry32);

                    results.Add(new StartupEntry
                    {
                        Name = name,
                        Command = cmd,
                        Source = "StartupFolder",
                        Scope = scope,
                        Location = file,
                        Enabled = enabled
                    });
                }
            }
            catch
            {
                // Ignore folders we cannot enumerate
            }
        }

        static string TryResolveShortcut(string shortcutPath)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return shortcutPath;

                // create the System.__ComObject
                dynamic? shell = Activator.CreateInstance(shellType);
                dynamic? lnk = shell?.CreateShortcut(shortcutPath);

                string? target = lnk?.TargetPath as string;
                string? args = lnk?.Arguments as string;

                if (!string.IsNullOrWhiteSpace(target))
                {
                    var cmd = $"\"{target}\"";
                    if (!string.IsNullOrWhiteSpace(args))
                        cmd += " " + args;
                    return cmd;
                }
            }
            catch
            {
                // Fall back to returning the .lnk path
            }
            return shortcutPath;
        }

        static string TryResolveShortcutReturnEmptyIfFails(string shortcutPath)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return shortcutPath;

                // create the System.__ComObject
                dynamic? shell = Activator.CreateInstance(shellType);
                dynamic? lnk = shell?.CreateShortcut(shortcutPath);

                string? target = lnk?.TargetPath as string;
                string? args = lnk?.Arguments as string;

                if (!string.IsNullOrWhiteSpace(target))
                {
                    var cmd = $"\"{target}\"";
                    if (!string.IsNullOrWhiteSpace(args))
                        cmd += " " + args;
                    return cmd;
                }
            }
            catch { /* Ignore */ }
            return string.Empty;
        }

        /// <summary>
        /// Task Scheduler (logon triggers)
        /// </summary>
        static void CollectLogonTasks(List<StartupEntry> results)
        {
            try
            {
                var tsType = Type.GetTypeFromProgID("Schedule.Service");
                if (tsType == null)
                    return;

                dynamic? service = Activator.CreateInstance(tsType);
                service?.Connect();
                dynamic? root = service?.GetFolder("\\");
                if (root == null)
                    return;

                EnumerateTaskFolder(root, results);
            }
            catch
            {
                // Task Scheduler service not available or access denied
            }
        }

        /// <summary>
        /// Collects tasks in the given <paramref name="folder"/>.
        /// </summary>
        static void EnumerateTaskFolder(dynamic folder, List<StartupEntry> results)
        {
            dynamic? tasks = null;
            try 
            { 
                tasks = folder.GetTasks(1); 
            } 
            catch { tasks = null; }

            if (tasks != null)
            {
                int count = 0;
                try 
                { 
                    count = (int)tasks.Count; 
                } 
                catch { count = 0; }

                for (int i = 1; i <= count; i++)
                {
                    dynamic? task = null;
                    try 
                    { 
                        task = tasks[i]; 
                    } 
                    catch { continue; }

                    try
                    {
                        string path = task.Path;
                        string name = task.Name;
                        bool enabled = false;
                        try 
                        { 
                            enabled = (bool)task.Enabled; 
                        } 
                        catch { }

                        dynamic def = task.Definition;
                        dynamic triggers = def.Triggers;

                        bool hasLogon = false;
                        string scope = "AnyUser"; // modified below if trigger has a specific user
                        string? user = null;

                        int tCount = 0;
                        try 
                        { 
                            tCount = (int)triggers.Count; 
                        } 
                        catch { tCount = 0; }

                        for (int t = 1; t <= tCount; t++)
                        {
                            dynamic tr = triggers[t];
                            int type = -1;
                            try 
                            { 
                                type = (int)tr.Type; 
                            } 
                            catch { }

                            // TASK_TRIGGER_LOGON == 9
                            if (type == 9)
                            {
                                hasLogon = true;
                                try 
                                { 
                                    user = tr.UserId as string; 
                                } 
                                catch { }
                                if (!string.IsNullOrWhiteSpace(user))
                                    scope = $"User:{user}";
                            }
                        }

                        if (!hasLogon) continue;

                        // Prefer first Exec action
                        string command = "[non-exec or no action]";
                        try
                        {
                            dynamic actions = def.Actions;
                            int aCount = 0;
                            try 
                            { 
                                aCount = (int)actions.Count; 
                            } 
                            catch { aCount = 0; }

                            for (int a = 1; a <= aCount; a++)
                            {
                                dynamic act = actions[a];
                                int at = -1;
                                try 
                                { 
                                    at = (int)act.Type; 
                                } 
                                catch { }

                                // TASK_ACTION_EXEC == 0
                                if (at == 0)
                                {
                                    string? p = null;
                                    string? args = null;
                                    try 
                                    { 
                                        p = act.Path as string; 
                                    } 
                                    catch { }
                                    try 
                                    { 
                                        args = act.Arguments as string; 
                                    } 
                                    catch { }

                                    if (!string.IsNullOrWhiteSpace(p))
                                    {
                                        command = $"\"{p}\"" + (string.IsNullOrWhiteSpace(args) ? "" : " " + args);
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }

                        results.Add(new StartupEntry
                        {
                            Name = name,
                            Command = command,
                            Source = "TaskScheduler",
                            Scope = scope,
                            Location = path,
                            Enabled = enabled
                        });
                    }
                    catch { /* Ignore tasks we can't fully read */ }
                }
            }

            // Recurse into subfolders
            dynamic? subs = null;
            try
            {
                subs = folder.GetFolders(0);
            }
            catch { subs = null; }

            if (subs != null)
            {
                int sc = 0;
                try
                {
                    sc = (int)subs.Count;
                }
                catch { sc = 0; }

                for (int i = 1; i <= sc; i++)
                {
                    dynamic? sub = null;
                    try
                    {
                        sub = subs[i];
                    }
                    catch { continue; }
                    EnumerateTaskFolder(sub, results);
                }
            }
        }

        /// <summary>
        /// Reads the Startup folder and returns each file's name and contents.
        /// </summary>
        /// <returns>Dictionary with file name as key and contents as value.</returns>
        public static Dictionary<string, string> GetShellStartupFilesAndContents()
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Get current user's Startup folder
                string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup));

                if (!Directory.Exists(startupPath))
                    return results;

                foreach (var file in Directory.GetFiles(startupPath))
                {
                    string fileName = Path.GetFileName(file);
                    string content = string.Empty;

                    try
                    {
                        // Try to read as text
                        var enc = SniffEncoding(file);
                        if (Path.GetExtension(file).Contains(".lnk", StringComparison.OrdinalIgnoreCase))
                        {
                            content = TryResolveShortcutReturnEmptyIfFails(file);
                            if (string.IsNullOrEmpty(content)) 
                            {
                                content = ExtractUsableText(file);
                                //List<string> lines = ExtractUtf16LeStrings(file);
                            }
                        }
                        else
                            content = File.ReadAllText(file, enc);
                    }
                    catch (Exception ex)
                    {
                        // If not a text file, note it
                        content = $"[Unable to read: {ex.Message}]";
                    }

                    results[fileName] = content;
                }
            }
            catch (Exception ex)
            {
                results["[Error]"] = $"Failed to analyze Startup folder: {ex.Message}";
            }

            return results;
        }

        /// <summary>
        /// Skips the ctrl chars and high‑ASCII noise, then collapses the survivors into a string.
        /// </summary>
        static string ExtractUsableText(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var filtered = bytes
                    .Where(b => b >= 0x20 && b <= 0x7E || b == 0x0A || b == 0x0D) // printable ASCII + CR/LF
                    .Select(b => (char)b)
                    .ToArray();
                return new string(filtered);
            }
            catch (Exception) { return string.Empty; }
        }

        /// <summary>
        /// Helpful when dealing with ".lnk" UTF16LE shortcut files.
        /// </summary>
        static List<string> ExtractUtf16LeStrings(string filePath, int minChars = 4)
        {
            var results = new List<string>();
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);

                int i = 0;
                while (i < bytes.Length - 1)
                {
                    int start = i;
                    int charCount = 0;
                    // Look for sequences: printable ASCII (0x20–0x7E) followed by 0x00
                    while (i < bytes.Length - 1 && bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] == 0x00)
                    {
                        charCount++;
                        i += 2;
                    }
                    // Do we have enough?
                    if (charCount >= minChars)
                    {
                        // Decode the slice as UTF-16LE
                        int lengthBytes = charCount * 2;
                        string s = Encoding.Unicode.GetString(bytes, start, lengthBytes);
                        results.Add(s);
                    }
                    i += 2; // move past the last checked pair
                }
            }
            catch (Exception) { /* Ignore */ }
            return results;
        }

        public static Encoding SniffEncoding(FileInfo file) => SniffEncoding(file.FullName);
        public static Encoding SniffEncoding(string filePath)
        {
            try
            {   // detectEncodingFromByteOrderMarks = true (enables BOM sniffing)
                using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    return reader.CurrentEncoding;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR]: {ex.Message}");
            }
            return Encoding.Default;
        }


        /// <summary>
        /// Example usage
        /// </summary>
        public static void RunTest()
        {
            var entries = StartupAnalyzer.GetAllStartupEntries();
            foreach (var e in entries.OrderBy(x => x.Source).ThenBy(x => x.Scope).ThenBy(x => x.Name))
            {
                    Debug.WriteLine($"[{e.Source}] ({e.Scope}) {e.Name}");
                    Debug.WriteLine($"  Enabled: {e.Enabled?.ToString() ?? "Unknown"}");
                    Debug.WriteLine($"  Command: {e.Command}");
                    Debug.WriteLine($"  Location: {e.Location}");
                    Debug.WriteLine(new string('—',50));
            }
        }

    }
}
