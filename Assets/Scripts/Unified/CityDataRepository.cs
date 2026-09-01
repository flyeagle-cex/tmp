using System;
using UnityEngine;

public static class CityDataRepository
{
    public static CityInteractionData Load(string sceneName)
    {
        TextAsset json = Resources.Load<TextAsset>("CityData/" + sceneName);
        if (json == null)
        {
            Debug.LogError("Missing city interaction data: Resources/CityData/" + sceneName + ".json");
            return null;
        }

        try
        {
            CityInteractionData data = JsonUtility.FromJson<CityInteractionData>(json.text);
            if (data == null || data.segments == null)
            {
                Debug.LogError("Invalid city interaction data for scene " + sceneName);
                return null;
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not parse city interaction data for scene " + sceneName + ": " + exception.Message);
            return null;
        }
    }

    public static string GetCityName(string sceneName, AppLanguage language)
    {
        CityInteractionData data = Load(sceneName);
        return data != null ? data.GetCityName(language) : sceneName;
    }
}
