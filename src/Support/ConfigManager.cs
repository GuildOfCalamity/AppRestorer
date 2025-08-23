using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AppRestorer;

/// <summary>
///   Defines a simple configuration manager that reads and writes settings to an XML file.
/// </summary>
/// <remarks>
///   Does not support complex types or arrays directly.
/// </remarks>
public static class ConfigManager
{
    public static event EventHandler<Exception>? OnError;
    static ConfigData? _data = Load();
    static string _path = string.Empty;
    public static string FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_path))
                _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xml");
            return _path;
        }
    }

    static ConfigData? Load()
    {
        // No existing file? Start fresh.
        if (!File.Exists(FilePath))
            return new ConfigData();

        try
        {
            #region [Simple XML deserialization]
            //using (var stream = File.OpenRead(FilePath))
            //{
            //    var serializer = new XmlSerializer(typeof(ConfigData));
            //    return (ConfigData)serializer.Deserialize(stream);
            //}
            #endregion

            #region [More robust XML deserialization with error handling]
            var xml = File.ReadAllText(FilePath);
            using (StringReader stringReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ConfigData));
                    if (serializer.CanDeserialize(xmlReader))
                        return (ConfigData?)serializer.Deserialize(xmlReader);
                    else
                        OnError?.Invoke(null, new Exception($"I can't deserialize this: \"{xml}\""));
                }
            }
            #endregion
        }
        catch (Exception ex)
        {
            OnError?.Invoke(null, ex);
            Debug.WriteLine($"[ERROR] ConfigManager.Load() ⇒ {ex.Message}");
        }

        // Corrupted file? Start fresh.
        return new ConfigData();
    }

    static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var serializer = new XmlSerializer(typeof(ConfigData));
            using (var writer = new StreamWriter(FilePath, false, Encoding.UTF8))
            {
                //new XmlSerializer(typeof(ConfigData)).Serialize(writer, _data);
                serializer.Serialize(writer, _data);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(null, ex);
            Debug.WriteLine($"[ERROR] ConfigManager.Save() ⇒ {ex.Message}");
        }
    }

    #region [Get]
    /// <summary>
    /// Non-generic getter
    /// </summary>
    public static string? Get(string key, string? defaultValue = null)
    {
        var setting = _data?.Settings.FirstOrDefault(s => s.Key == key);
        return setting != null ? setting.Value : defaultValue;
    }

    /// <summary>
    /// Generic getter
    /// </summary>
    public static T? Get<T>(string key, T? defaultValue = default)
    {
        string? raw = Get(key, null);
        if (raw == null)
            return defaultValue;

        try
        {
            if (typeof(T).IsEnum) // handle enums specially
                return (T)Enum.Parse(typeof(T), raw, ignoreCase: true);

            // convert primitives and structs
            return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue;
        }
    }
    #endregion

    #region [Set]
    /// <summary>
    /// Non-generic setter: accepts strings
    /// </summary>
    public static void Set(string key, string value, bool saveAfterUpdate = true)
    {
        var setting = _data?.Settings.FirstOrDefault(s => s.Key == key);
        if (setting == null) // if the setting does not exist, create it
            _data?.Settings.Add(new Setting { Key = key, Value = value, TypeName = value?.GetType()?.AssemblyQualifiedName ?? "" });
        else
            setting.Value = value;

        if (saveAfterUpdate)
            Save();
    }

    /// <summary>
    /// Generic setter: accepts any T, converts it to a string and persists
    /// </summary>
    public static void Set<T>(string key, T value, bool saveAfterUpdate = true)
    {
        string? str;
        if (value == null)
            str = null;
        else if (value is IFormattable f)
            str = f.ToString(null, CultureInfo.InvariantCulture);
        else
            str = $"{value}";

        var setting = _data?.Settings.FirstOrDefault(s => s.Key == key);
        if (setting == null) // if the setting does not exist, create it
            _data?.Settings.Add(new Setting { Key = key, Value = str, TypeName = value?.GetType()?.AssemblyQualifiedName ?? "" });
        else
            setting.Value = str;

        if (saveAfterUpdate)
            Save();
    }
    #endregion
}

#region [XML configuration structure]
[XmlRoot("configuration")]
public class ConfigData
{
    [XmlElement("add")]
    public List<Setting> Settings { get; set; } = new List<Setting>();
}

/// <summary>
/// A simple key/value pair for configuration settings.
/// </summary>
public class Setting
{
    [XmlAttribute("key")]
    public string? Key { get; set; }

    [XmlAttribute("value")]
    public string? Value { get; set; }

    [XmlAttribute("type")]
    public string? TypeName { get; set; } // for supporting more complex types in the future
}
#endregion
