using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class GlobalSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        LanguageManager.EnsureExists();
        AppState.EnsureExists();
        NavigationManager.EnsureExists();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RenderSettings.skybox = null;
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].clearFlags = CameraClearFlags.SolidColor;
            cameras[i].backgroundColor = new Color32(7, 23, 38, 255);
        }

        LanguageManager.EnsureExists().ApplyGlobalSoundSetting();

        CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
        for (int i = 0; i < scalers.Length; i++)
        {
            scalers[i].uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scalers[i].referenceResolution = new Vector2(1920f, 1080f);
            scalers[i].screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scalers[i].matchWidthOrHeight = 0.5f;
        }

        if (scene.name != "Start")
        {
            return;
        }

        HomeDirectoryController home = Object.FindFirstObjectByType<HomeDirectoryController>();
        if (home == null)
        {
            new GameObject("HomeDirectoryController").AddComponent<HomeDirectoryController>();
        }

        VideoPlayer[] players = Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            VideoPlayer player = players[i];
            AudioController controller = players[i].GetComponent<AudioController>();
            if (controller == null)
            {
                controller = players[i].gameObject.AddComponent<AudioController>();
            }

            player.Stop();
            player.playOnAwake = false;
            player.clip = null;
            player.source = VideoSource.Url;
            player.url = MediaPathResolver.GetUrl("intro.mp4");
            controller.Initialize(player, true);
            player.prepareCompleted -= OnStartVideoPrepared;
            player.prepareCompleted += OnStartVideoPrepared;
            player.Prepare();
        }
    }

    private static void OnStartVideoPrepared(VideoPlayer player)
    {
        player.prepareCompleted -= OnStartVideoPrepared;
        RawImage image = player.GetComponent<RawImage>();
        AspectRatioFitter fitter = image != null ? image.GetComponent<AspectRatioFitter>() : null;
        if (image != null && fitter == null)
        {
            fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        }
        if (fitter != null && player.height > 0)
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)player.width / player.height;
        }
        player.Play();
    }
}
