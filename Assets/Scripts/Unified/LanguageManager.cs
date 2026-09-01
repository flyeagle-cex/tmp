using System;
using UnityEngine;

public enum AppLanguage
{
    Chinese = 0,
    English = 1
}

public enum SubtitleMode
{
    Chinese = 0,
    English = 1,
    Bilingual = 2,
    Off = 3
}

public enum SubtitleSize
{
    Small = 0,
    Medium = 1,
    Large = 2
}

public sealed class LanguageManager : MonoBehaviour
{
    private const string LanguageKey = "Jiangnan.Language";
    private const string SubtitleModeKey = "Jiangnan.SubtitleMode";
    private const string SubtitleSizeKey = "Jiangnan.SubtitleSize";
    private const string SoundKey = "Jiangnan.Sound";

    public static LanguageManager Instance { get; private set; }

    public AppLanguage CurrentLanguage { get; private set; }
    public SubtitleMode CurrentSubtitleMode { get; private set; }
    public SubtitleSize CurrentSubtitleSize { get; private set; }
    public bool SoundEnabled { get; private set; }

    public event Action LanguageChanged;
    public event Action SubtitleSettingsChanged;
    public event Action SoundChanged;

    public static LanguageManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LanguageManager existing = FindFirstObjectByType<LanguageManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("LanguageManager");
        return host.AddComponent<LanguageManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLanguage = (AppLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, (int)AppLanguage.Chinese), 0, 1);
        CurrentSubtitleMode = (SubtitleMode)Mathf.Clamp(PlayerPrefs.GetInt(SubtitleModeKey, (int)SubtitleMode.Bilingual), 0, 3);
        CurrentSubtitleSize = (SubtitleSize)Mathf.Clamp(PlayerPrefs.GetInt(SubtitleSizeKey, (int)SubtitleSize.Medium), 0, 2);
        SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
    }

    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == AppLanguage.Chinese ? AppLanguage.English : AppLanguage.Chinese);
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public void SetSubtitleMode(SubtitleMode mode)
    {
        if (CurrentSubtitleMode == mode)
        {
            return;
        }

        CurrentSubtitleMode = mode;
        PlayerPrefs.SetInt(SubtitleModeKey, (int)mode);
        PlayerPrefs.Save();
        SubtitleSettingsChanged?.Invoke();
    }

    public void SetSubtitleSize(SubtitleSize size)
    {
        if (CurrentSubtitleSize == size)
        {
            return;
        }

        CurrentSubtitleSize = size;
        PlayerPrefs.SetInt(SubtitleSizeKey, (int)size);
        PlayerPrefs.Save();
        SubtitleSettingsChanged?.Invoke();
    }

    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        PlayerPrefs.SetInt(SoundKey, SoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        SoundChanged?.Invoke();
    }
}
