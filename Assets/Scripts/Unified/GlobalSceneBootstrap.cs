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
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
