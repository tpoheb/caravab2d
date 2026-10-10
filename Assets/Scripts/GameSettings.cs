using System;
using UnityEngine;

[Serializable] public sealed class GameSettingsData
{
    public float sound = 1f, music = 0.7f;
    public bool fullscreen = true;
    public int width, height, quality = 2;
}

/// <summary>Параметры приложения сохраняются отдельно от партии.</summary>
public static class GameSettings
{
    private const string Key = "Caravan.Settings.v1";
    public static string OverrideKey { get; set; }
    private static string CurrentKey => OverrideKey ?? Key;
    public static GameSettingsData Current { get; private set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Current = null; Changed = null; }

    public static void Load()
    {
        if (Current != null) return;
        try { Current = JsonUtility.FromJson<GameSettingsData>(PlayerPrefs.GetString(CurrentKey, "{}")); }
        catch (ArgumentException) { Current = null; }
        Current ??= new GameSettingsData();
        if (Current.width < 640 || Current.height < 480)
        { Current.width = Screen.width; Current.height = Screen.height; }
        Normalize(Current);
        Apply(Current);
    }
    public static void Apply(GameSettingsData data)
    {
        Normalize(data);
        Current = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(data));
        AudioListener.volume = Current.sound;
        QualitySettings.SetQualityLevel(Current.quality, true);
        if (!Application.isBatchMode)
            Screen.SetResolution(Current.width, Current.height,
                Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        PlayerPrefs.SetString(CurrentKey, JsonUtility.ToJson(Current)); PlayerPrefs.Save();
        Changed?.Invoke();
    }
    public static GameSettingsData Copy() => JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(Current));
    private static void Normalize(GameSettingsData data)
    {
        data.sound = float.IsNaN(data.sound) ? 1f : Mathf.Clamp01(data.sound);
        data.music = float.IsNaN(data.music) ? 0.7f : Mathf.Clamp01(data.music);
        data.quality = Mathf.Clamp(data.quality, 0, QualitySettings.names.Length - 1);
        data.width = Mathf.Max(640, data.width); data.height = Mathf.Max(480, data.height);
    }
}
