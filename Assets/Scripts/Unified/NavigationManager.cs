using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public sealed class NavigationManager : MonoBehaviour
{
    public const string DirectorySceneName = "Start";

    public static NavigationManager Instance { get; private set; }

    public static NavigationManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        NavigationManager existing = FindFirstObjectByType<NavigationManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        return new GameObject("NavigationManager").AddComponent<NavigationManager>();
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

    public void OpenCity(CityRegistryEntry entry)
    {
        if (entry == null || !entry.isAvailable || string.IsNullOrWhiteSpace(entry.sceneName))
        {
            return;
        }

        AppState.EnsureExists().SelectCity(entry.id);
        LoadScene(entry.sceneName);
    }

    public void ReturnToDirectory()
    {
        LoadScene(DirectorySceneName);
    }

    public void MarkCurrentCityCompleted()
    {
        CityRegistryEntry entry = CityRegistry.FindByScene(SceneManager.GetActiveScene().name);
        if (entry != null)
        {
            AppState.EnsureExists().MarkCityCompleted(entry.id);
        }
    }

    private static void LoadScene(string sceneName)
    {
        StopAndReleaseSceneMedia();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private static void StopAndReleaseSceneMedia()
    {
        VideoPlayer[] players = FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            VideoPlayer player = players[i];
            if (player == null)
            {
                continue;
            }

            player.Stop();
            player.clip = null;
            player.url = string.Empty;
        }

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
            {
                sources[i].Stop();
            }
        }
    }
}
