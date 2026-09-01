using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class RuntimeSmokeRunner : MonoBehaviour
{
    [Serializable]
    private sealed class SceneResult
    {
        public string scene;
        public string city;
        public bool introReady;
        public bool audioReady;
        public bool languageReady;
        public bool subtitlesReady;
        public bool resolutionSafe;
        public bool passed;
    }

    [Serializable]
    private sealed class SmokeReport
    {
        public string unityVersion;
        public string generatedAt;
        public List<SceneResult> scenes = new List<SceneResult>();
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public bool passed;
    }

    private SmokeReport report;
    private string outputPath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallWhenRequested()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool requested = Array.IndexOf(args, "-jiangnanSmoke") >= 0;
        if (!requested || FindFirstObjectByType<RuntimeSmokeRunner>() != null)
        {
            return;
        }

        GameObject host = new GameObject("RuntimeSmokeRunner");
        DontDestroyOnLoad(host);
        host.AddComponent<RuntimeSmokeRunner>();
    }

    private IEnumerator Start()
    {
        outputPath = GetOutputPath();
        report = new SmokeReport
        {
            unityVersion = Application.unityVersion,
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        LanguageManager settings = LanguageManager.EnsureExists();
        settings.SetLanguage(AppLanguage.Chinese);
        settings.SetSubtitleMode(SubtitleMode.Bilingual);
        settings.SetSubtitleSize(SubtitleSize.Medium);
        if (!settings.SoundEnabled)
        {
            settings.ToggleSound();
        }

        string[] cityScenes = { "1", "2", "3", "4", "5" };
        for (int i = 0; i < cityScenes.Length; i++)
        {
            yield return SceneManager.LoadSceneAsync(cityScenes[i], LoadSceneMode.Single);
            yield return null;
            yield return null;

            SceneResult result = new SceneResult
            {
                scene = cityScenes[i],
                passed = true
            };
            report.scenes.Add(result);

            VideoQuizManager manager = FindFirstObjectByType<VideoQuizManager>();
            GameObject root = GameObject.Find("CityInteractionRoot");
            CityInteractionData data = CityDataRepository.Load(cityScenes[i]);
            result.city = data != null ? data.cityId : string.Empty;

            result.introReady = manager != null && root != null && manager.State == CityInteractionState.Intro &&
                                !manager.videoPlayer.isPlaying && manager.questionPanel != null && !manager.questionPanel.activeSelf;
            Require(result, result.introReady, cityScenes[i] + ": intro state is not ready.");

            CanvasScaler scaler = manager != null ? manager.GetComponent<CanvasScaler>() : null;
            bool scalerReady = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                               scaler.referenceResolution == new Vector2(1920f, 1080f) && Mathf.Abs(scaler.matchWidthOrHeight - 0.5f) < 0.001f;
            Require(result, scalerReady, cityScenes[i] + ": runtime CanvasScaler is not normalized.");

            AudioSource source = manager != null && manager.videoPlayer != null ? manager.videoPlayer.GetComponent<AudioSource>() : null;
            result.audioReady = manager != null && manager.videoPlayer != null &&
                                manager.videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource &&
                                manager.videoPlayer.GetTargetAudioSource(0) == source && source != null && !source.mute;
            Require(result, result.audioReady, cityScenes[i] + ": runtime video audio is not connected.");

            RawImage rawImage = manager != null && manager.videoPlayer != null ? manager.videoPlayer.GetComponent<RawImage>() : null;
            AspectRatioFitter fitter = rawImage != null ? rawImage.GetComponent<AspectRatioFitter>() : null;
            Require(result, fitter != null && fitter.aspectMode == AspectRatioFitter.AspectMode.FitInParent,
                cityScenes[i] + ": video aspect fitter is missing.");

            result.resolutionSafe = ValidateActiveUiInsideScreen(root, cityScenes[i]);
            Require(result, result.resolutionSafe, cityScenes[i] + ": active UI extends outside the screen.");

            if (i == 0 && manager != null && root != null)
            {
                Transform startTransform = root.transform.Find("IntroLayer/IntroCard/StartButton");
                Button startButton = startTransform != null ? startTransform.GetComponent<Button>() : null;
                Require(result, startButton != null, "Yangzhou: shared start button is missing.");
                if (startButton != null)
                {
                    startButton.onClick.Invoke();
                    float timeout = 20f;
                    while (manager.State != CityInteractionState.PlayingVideo && timeout > 0f)
                    {
                        timeout -= Time.unscaledDeltaTime;
                        yield return null;
                    }

                    Require(result, manager.State == CityInteractionState.PlayingVideo && manager.videoPlayer.isPlaying,
                        "Yangzhou: video did not enter PlayingVideo state.");

                    if (manager.videoPlayer.isPlaying)
                    {
                        yield return new WaitForSecondsRealtime(0.6f);
                        double before = manager.videoPlayer.time;
                        settings.SetLanguage(AppLanguage.English);
                        yield return null;
                        double after = manager.videoPlayer.time;
                        result.languageReady = settings.CurrentLanguage == AppLanguage.English && after + 0.05d >= before && after - before < 0.5d;
                        Require(result, result.languageReady, "Yangzhou: language switching changed the video timeline.");

                        settings.SetSubtitleMode(SubtitleMode.Chinese);
                        settings.SetSubtitleMode(SubtitleMode.English);
                        settings.SetSubtitleMode(SubtitleMode.Bilingual);
                        settings.SetSubtitleMode(SubtitleMode.Off);
                        settings.SetSubtitleMode(SubtitleMode.Bilingual);
                        result.subtitlesReady = settings.CurrentSubtitleMode == SubtitleMode.Bilingual;
                        Require(result, result.subtitlesReady, "Yangzhou: subtitle mode switching failed.");

                        double muteTime = manager.videoPlayer.time;
                        settings.ToggleSound();
                        yield return null;
                        bool mutedWithoutReset = source != null && source.mute && manager.videoPlayer.time + 0.05d >= muteTime;
                        settings.ToggleSound();
                        Require(result, mutedWithoutReset && source != null && !source.mute,
                            "Yangzhou: sound toggle stopped or reset playback.");
                        manager.videoPlayer.Pause();
                    }
                }
            }
            else
            {
                result.languageReady = settings.CurrentLanguage == AppLanguage.English;
                result.subtitlesReady = settings.CurrentSubtitleMode == SubtitleMode.Bilingual;
                Require(result, result.languageReady, cityScenes[i] + ": global language did not persist across scenes.");
                Require(result, result.subtitlesReady, cityScenes[i] + ": subtitle settings did not persist across scenes.");
            }

        }

        settings.SetLanguage(AppLanguage.Chinese);
        settings.SetSubtitleMode(SubtitleMode.Bilingual);
        settings.SetSubtitleSize(SubtitleSize.Medium);
        if (!settings.SoundEnabled)
        {
            settings.ToggleSound();
        }

        report.passed = report.errors.Count == 0;
        WriteReport();
        Debug.Log(report.passed ? "JIANGNAN_RUNTIME_SMOKE_PASS" : "JIANGNAN_RUNTIME_SMOKE_FAIL");
        yield return null;
        Application.Quit(report.passed ? 0 : 2);
    }

    private bool ValidateActiveUiInsideScreen(GameObject root, string sceneName)
    {
        if (root == null)
        {
            return false;
        }

        Canvas.ForceUpdateCanvases();
        string[] paths =
        {
            "TopBar",
            "IntroLayer/IntroCard",
            "IntroLayer/IntroCard/StartButton"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            Transform item = root.transform.Find(paths[i]);
            RectTransform rect = item as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                report.errors.Add(sceneName + ": missing active UI element " + paths[i] + ".");
                return false;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            for (int corner = 0; corner < corners.Length; corner++)
            {
                if (corners[corner].x < -2f || corners[corner].y < -2f ||
                    corners[corner].x > Screen.width + 2f || corners[corner].y > Screen.height + 2f)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void Require(SceneResult result, bool condition, string message)
    {
        if (condition)
        {
            return;
        }

        result.passed = false;
        report.errors.Add(message);
    }

    private string GetOutputPath()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-jiangnanSmokeOutput")
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }

        return Path.GetFullPath(Path.Combine(Application.persistentDataPath, "jiangnan-runtime-smoke.json"));
    }

    private void WriteReport()
    {
        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
    }
}
