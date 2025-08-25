using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AppRestorer;

// Flags for Speak method
[Flags]
public enum SpeechVoiceSpeakFlags
{
    SVSFDefault = 0,
    SVSFlagsAsync = 1,
    SVSFPurgeBeforeSpeak = 2,
    SVSFIsFilename = 4,
    SVSFIsXML = 8,
    SVSFIsNotXML = 16,
    SVSFPersistXML = 32
}

[ComImport]
[Guid("269316D8-57BD-11D2-9EEE-00C04F797396")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface ISpVoice
{
    #region [Properties]
    object Voice { get; set; }
    object AudioOutput { get; set; }
    object AudioOutputStream { get; set; }
    int Rate { get; set; }
    int Volume { get; set; }
    object Priority { get; set; }
    object AlertBoundary { get; set; }
    object EventInterests { get; set; }
    #endregion

    /// <summary>
    /// Speak text
    /// </summary>
    /// <param name="text"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    int Speak([MarshalAs(UnmanagedType.BStr)] string text, SpeechVoiceSpeakFlags flags);

    /// <summary>
    /// Speak from a stream
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    int SpeakStream(IStream stream, SpeechVoiceSpeakFlags flags);

    /// <summary>
    /// Pause speaking
    /// </summary>
    void Pause();

    /// <summary>
    /// Resume speaking
    /// </summary>
    void Resume();

    /// <summary>
    /// Skip items
    /// </summary>
    /// <param name="type"></param>
    /// <param name="numItems"></param>
    /// <returns></returns>
    int Skip([MarshalAs(UnmanagedType.BStr)] string type, int numItems);

    /// <summary>
    /// Get available voices
    /// </summary>
    /// <param name="requiredAttributes"></param>
    /// <param name="optionalAttributes"></param>
    /// <returns></returns>
    object GetVoices([MarshalAs(UnmanagedType.BStr)] string requiredAttributes = "", [MarshalAs(UnmanagedType.BStr)] string optionalAttributes = "");

    /// <summary>
    /// Get audio outputs
    /// </summary>
    /// <param name="requiredAttributes"></param>
    /// <param name="optionalAttributes"></param>
    /// <returns></returns>
    object GetAudioOutputs([MarshalAs(UnmanagedType.BStr)] string requiredAttributes = "", [MarshalAs(UnmanagedType.BStr)] string optionalAttributes = "");

    /// <summary>
    /// Wait until done
    /// </summary>
    /// <param name="msTimeout"></param>
    /// <returns></returns>
    bool WaitUntilDone(int msTimeout);

    /// <summary>
    /// UI support
    /// </summary>
    /// <param name="typeOfUI"></param>
    /// <param name="extraData"></param>
    /// <returns></returns>
    bool IsUISupported([MarshalAs(UnmanagedType.BStr)] string typeOfUI, ref object extraData);

    void DisplayUI(IntPtr hWndParent, [MarshalAs(UnmanagedType.BStr)] string title, [MarshalAs(UnmanagedType.BStr)] string typeOfUI, ref object extraData);
}

[ComImport]
[Guid("C74A3ADC-B727-4500-A84A-B526721C8B8C")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface ISpeechObjectToken
{
    [return: MarshalAs(UnmanagedType.BStr)]
    string GetDescription(int locale = 0);
}

[ComImport]
[Guid("9285B776-2E7B-4BC0-B53E-580EB6FA967F")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface ISpeechObjectTokens
{
    int Count { get; }
    ISpeechObjectToken Item(int index);
}


[ComImport]
[Guid("96749377-3391-11D2-9EE3-00C04F797396")]
public class SpVoice
{
}


public static class SpVoiceExtensions
{
    /*
    Microsoft David Desktop    - English (United States)
    Microsoft Hazel Desktop    - English (Great Britain)
    Microsoft Hedda Desktop    - German
    Microsoft Zira Desktop     - English (United States)
    Microsoft Hortense Desktop - French
    Microsoft Elsa Desktop     - Italian (Italy)
    Microsoft Haruka Desktop   - Japanese
    Microsoft Heami Desktop    - Korean
    Microsoft Maria Desktop    - Portuguese (Brazil)
    Microsoft Huihui Desktop   - Chinese (Simplified)
    */
    public static bool SetVoiceByName(this ISpVoice voice, string namePart)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            throw new ArgumentException("Voice name cannot be empty.", nameof(namePart));

        var voicesObj = voice.GetVoices();
        if (voicesObj is ISpeechObjectTokens voices)
        {
            for (int i = 0; i < voices.Count; i++)
            {
                var token = voices.Item(i);
                var desc = token.GetDescription();
                if (desc.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    voice.Voice = token;
                    return true;
                }
            }
        }
        return false; // No match found
    }
}
