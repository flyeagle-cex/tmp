using System;
using UnityEngine;

[Serializable]
public sealed class CityRegistryEntry
{
    public string id;
    public string nameCN;
    public string nameEN;
    public string keywordsCN;
    public string keywordsEN;
    public string thumbnailResource;
    public string sceneName;
    public string dataResource;
    public bool isAvailable;
    public int sortOrder;
    public bool hasHardSubtitles;

    public string GetName(AppLanguage language)
    {
        return language == AppLanguage.English && !string.IsNullOrWhiteSpace(nameEN) ? nameEN : nameCN;
    }

    public string GetKeywords(AppLanguage language)
    {
        return language == AppLanguage.English && !string.IsNullOrWhiteSpace(keywordsEN) ? keywordsEN : keywordsCN;
    }
}

[Serializable]
public sealed class CityRegistryDocument
{
    public CityRegistryEntry[] cities;
}

public static class CityRegistry
{
    private static CityRegistryEntry[] cachedEntries;

    public static CityRegistryEntry[] GetAll()
    {
        if (cachedEntries != null)
        {
            return cachedEntries;
        }

        TextAsset json = Resources.Load<TextAsset>("CityRegistry");
        if (json == null)
        {
            Debug.LogError("Missing Resources/CityRegistry.json.");
            cachedEntries = Array.Empty<CityRegistryEntry>();
            return cachedEntries;
        }

        CityRegistryDocument document = JsonUtility.FromJson<CityRegistryDocument>(json.text);
        cachedEntries = document != null && document.cities != null ? document.cities : Array.Empty<CityRegistryEntry>();
        Array.Sort(cachedEntries, (left, right) => left.sortOrder.CompareTo(right.sortOrder));
        return cachedEntries;
    }

    public static CityRegistryEntry FindById(string cityId)
    {
        CityRegistryEntry[] entries = GetAll();
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && string.Equals(entries[i].id, cityId, StringComparison.OrdinalIgnoreCase))
            {
                return entries[i];
            }
        }

        return null;
    }

    public static CityRegistryEntry FindByScene(string sceneName)
    {
        CityRegistryEntry[] entries = GetAll();
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && string.Equals(entries[i].sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return entries[i];
            }
        }

        return null;
    }

    public static int CountAvailable()
    {
        int count = 0;
        CityRegistryEntry[] entries = GetAll();
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].isAvailable)
            {
                count++;
            }
        }

        return count;
    }

    public static void ClearCache()
    {
        cachedEntries = null;
    }
}
