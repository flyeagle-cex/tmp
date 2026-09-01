using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AppState : MonoBehaviour
{
    private const string SelectedCityKey = "Jiangnan.SelectedCity";
    private const string CompletedCitiesKey = "Jiangnan.CompletedCities";

    public static AppState Instance { get; private set; }

    public string SelectedCityId { get; private set; }
    public IReadOnlyCollection<string> CompletedCities => completedCities;
    public AppLanguage CurrentLanguage => LanguageManager.EnsureExists().CurrentLanguage;
    public SubtitleMode CurrentSubtitleMode => LanguageManager.EnsureExists().CurrentSubtitleMode;
    public bool AudioEnabled => LanguageManager.EnsureExists().SoundEnabled;

    public event Action JourneyChanged;

    private readonly HashSet<string> completedCities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static AppState EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        AppState existing = FindFirstObjectByType<AppState>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        return new GameObject("AppState").AddComponent<AppState>();
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
        SelectedCityId = PlayerPrefs.GetString(SelectedCityKey, string.Empty);
        LoadCompletedCities();
    }

    public void SelectCity(string cityId)
    {
        SelectedCityId = cityId ?? string.Empty;
        PlayerPrefs.SetString(SelectedCityKey, SelectedCityId);
        PlayerPrefs.Save();
        JourneyChanged?.Invoke();
    }

    public void MarkCityCompleted(string cityId)
    {
        if (string.IsNullOrWhiteSpace(cityId) || !completedCities.Add(cityId))
        {
            return;
        }

        SaveCompletedCities();
        JourneyChanged?.Invoke();
    }

    public bool IsCityCompleted(string cityId)
    {
        return !string.IsNullOrWhiteSpace(cityId) && completedCities.Contains(cityId);
    }

    private void LoadCompletedCities()
    {
        completedCities.Clear();
        string serialized = PlayerPrefs.GetString(CompletedCitiesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        string[] cityIds = serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < cityIds.Length; i++)
        {
            completedCities.Add(cityIds[i]);
        }
    }

    private void SaveCompletedCities()
    {
        string[] cityIds = new string[completedCities.Count];
        completedCities.CopyTo(cityIds);
        Array.Sort(cityIds, StringComparer.OrdinalIgnoreCase);
        PlayerPrefs.SetString(CompletedCitiesKey, string.Join(";", cityIds));
        PlayerPrefs.Save();
    }
}
