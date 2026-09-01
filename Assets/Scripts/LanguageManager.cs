using System;
using UnityEngine;

public enum AppLanguage
{
    Chinese,
    English
}

public sealed class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }
    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Chinese;
    public bool IsEnglish => CurrentLanguage == AppLanguage.English;
    public event Action LanguageChanged;

    private static bool openDirectoryOnStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        EnsureExists();
    }

    public static LanguageManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LanguageManager existing = FindObjectOfType<LanguageManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("LanguageManager");
        Instance = host.AddComponent<LanguageManager>();
        DontDestroyOnLoad(host);
        return Instance;
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
    }

    public void ToggleLanguage()
    {
        SetLanguage(IsEnglish ? AppLanguage.Chinese : AppLanguage.English);
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }

    public static void RequestDirectoryOnStart()
    {
        openDirectoryOnStart = true;
    }

    public static bool ConsumeDirectoryRequest()
    {
        bool requested = openDirectoryOnStart;
        openDirectoryOnStart = false;
        return requested;
    }
}
