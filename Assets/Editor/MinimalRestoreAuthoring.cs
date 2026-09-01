using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MinimalRestoreAuthoring
{
    [Serializable] private sealed class CityDocument { public Segment[] segments; }
    [Serializable] private sealed class Segment { public Question question; }
    [Serializable] private sealed class Question
    {
        public string questionEn;
        public Option[] options;
        public string successEn;
    }
    [Serializable] private sealed class Option { public string textEn; }

    private static readonly string[] CityNamesZh = { "扬州", "淮安", "无锡", "苏州", "南京" };
    private static readonly string[] CityNamesEn = { "Yangzhou", "Huai'an", "Wuxi", "Suzhou", "Nanjing" };
    private const string EnglishDataRoot = "F:/新文化大赛/JiangnanProject/Assets/Resources/CityData";

    public static void Apply()
    {
        int localizedQuestions = 0;
        for (int index = 0; index <= 5; index++)
        {
            string sceneName = index == 0 ? "Start" : index.ToString();
            string path = "Assets/Scenes/" + sceneName + ".unity";
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                throw new InvalidOperationException(path + " has no Canvas.");
            }

            if (sceneName == "Start")
            {
                AuthorStart(canvas);
            }
            else
            {
                AuthorCity(canvas);
                localizedQuestions += InjectEnglishQuizData(sceneName);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (localizedQuestions != 24)
        {
            throw new InvalidDataException("Expected 24 localized questions, wrote " + localizedQuestions + ".");
        }

        string qa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "QA");
        Directory.CreateDirectory(qa);
        File.WriteAllText(Path.Combine(qa, "authoring-result.txt"),
            "MINIMAL_RESTORE_AUTHORING_PASS\nlocalizedQuestions=" + localizedQuestions + "\n");
        AssetDatabase.SaveAssets();
        Debug.Log("MINIMAL_RESTORE_AUTHORING_PASS localizedQuestions=" + localizedQuestions);
    }

    public static void Validate()
    {
        List<string> errors = new List<string>();
        int englishQuestions = 0;
        for (int index = 0; index <= 5; index++)
        {
            string sceneName = index == 0 ? "Start" : index.ToString();
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length != 1)
            {
                errors.Add(sceneName + ": expected one Canvas, found " + canvases.Length + ".");
            }
            else
            {
                CanvasScaler scaler = canvases[0].GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                    scaler.referenceResolution != new Vector2(1080f, 1920f))
                {
                    errors.Add(sceneName + ": Canvas is not configured for 1080x1920 portrait.");
                }
            }

            CityDirectoryController controller = UnityEngine.Object.FindFirstObjectByType<CityDirectoryController>();
            if (controller == null)
            {
                errors.Add(sceneName + ": CityDirectoryController missing.");
            }
            else if (sceneName == "Start")
            {
                if (controller.cityDirectoryPanel == null || controller.cityDirectoryPanel.transform.parent != canvases[0].transform)
                {
                    errors.Add("Start: directory panel is not on the original Canvas.");
                }
                if (controller.cityButtons == null || controller.cityButtons.Length != 5 || controller.cityButtons.Any(button => button == null))
                {
                    errors.Add("Start: directory does not contain exactly five city buttons.");
                }
            }

            if (sceneName != "Start")
            {
                VideoQuizManager manager = UnityEngine.Object.FindFirstObjectByType<VideoQuizManager>();
                if (manager == null)
                {
                    errors.Add(sceneName + ": VideoQuizManager missing.");
                }
                else
                {
                    for (int i = 0; i < manager.quizList.Count; i++)
                    {
                        VideoQuizManager.QuizData quiz = manager.quizList[i];
                        if (string.IsNullOrWhiteSpace(quiz.question)) continue;
                        if (string.IsNullOrWhiteSpace(quiz.questionEnglish) || quiz.answersEnglish == null || quiz.answersEnglish.Length == 0)
                        {
                            errors.Add(sceneName + ": quiz " + i + " lacks English text.");
                        }
                        else
                        {
                            englishQuestions++;
                        }
                    }
                }

                VideoSpeedController speedController = UnityEngine.Object.FindFirstObjectByType<VideoSpeedController>();
                if (speedController == null || speedController.speedButton == null || speedController.speedLabel == null ||
                    speedController.videoPlayer == null || speedController.videoPlayer != manager.videoPlayer)
                {
                    errors.Add(sceneName + ": playback-speed control is missing or invalid.");
                }
            }
        }

        if (englishQuestions != 24)
        {
            errors.Add("Expected 24 English questions, found " + englishQuestions + ".");
        }

        string scripts = Path.Combine(Application.dataPath, "Scripts");
        string[] forbidden = { "ExhibitionCityView", "CultureCueManager", "RuntimeVideoThumbnail", "SubtitleManager", "SubtitleLayer" };
        string[] files = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string text = File.ReadAllText(files[i]);
            for (int j = 0; j < forbidden.Length; j++)
            {
                if (text.Contains(forbidden[j]))
                {
                    errors.Add("Forbidden rebuilt system found: " + forbidden[j] + ".");
                }
            }
        }

        string qa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "QA");
        Directory.CreateDirectory(qa);
        File.WriteAllLines(Path.Combine(qa, "validation.txt"),
            new[] { errors.Count == 0 ? "MINIMAL_RESTORE_VALIDATION_PASS" : "MINIMAL_RESTORE_VALIDATION_FAIL",
                    "englishQuestions=" + englishQuestions,
                    "errors=" + errors.Count }.Concat(errors));
        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join("\n", errors));
        }
        Debug.Log("MINIMAL_RESTORE_VALIDATION_PASS englishQuestions=" + englishQuestions);
    }

    public static void BuildWindows()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(root, "Builds", "Windows", "JiangnanProject.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = false;
        string[] scenes = EditorBuildSettings.scenes.Where(item => item.enabled).Select(item => item.path).ToArray();
        BuildReport build = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        string result = "Windows build: " + build.summary.result + ", errors=" + build.summary.totalErrors +
                        ", warnings=" + build.summary.totalWarnings + ", bytes=" + build.summary.totalSize;
        Directory.CreateDirectory(Path.Combine(root, "QA"));
        File.WriteAllText(Path.Combine(root, "QA", "build-windows.txt"), result);
        Debug.Log("MINIMAL_RESTORE_BUILD " + result);
        if (build.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(result);
        }
    }

    private static void AuthorStart(Canvas canvas)
    {
        ConfigurePortraitCanvas(canvas);
        DestroyChild(canvas.transform, "MinimalLanguageButton");
        DestroyChild(canvas.transform, "MinimalCityDirectoryButton");
        DestroyChild(canvas.transform, "CityDirectoryPanel");

        CityDirectoryController oldController = canvas.GetComponent<CityDirectoryController>();
        if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);
        CityDirectoryController controller = canvas.gameObject.AddComponent<CityDirectoryController>();

        controller.languageButton = CreateButton("MinimalLanguageButton", canvas.transform,
            new Vector2(1f, 1f), new Vector2(-120f, -42f), new Vector2(190f, 58f),
            "中文 | EN", "中文 | EN", 24, out _);
        controller.cityDirectoryButton = CreateButton("MinimalCityDirectoryButton", canvas.transform,
            new Vector2(1f, 0f), new Vector2(-215f, 88f), new Vector2(330f, 112f),
            "城市目录\nCITY DIRECTORY", "CITY DIRECTORY\n城市目录", 28, out _);

        GameObject panelRoot = CreateRect("CityDirectoryPanel", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image blocker = panelRoot.AddComponent<Image>();
        blocker.color = new Color(0.04f, 0.2f, 0.3f, 0.28f);
        blocker.raycastTarget = true;
        controller.cityDirectoryPanel = panelRoot;

        GameObject popup = CreateRect("OriginalStylePopup", panelRoot.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 920f));
        Image popupImage = popup.AddComponent<Image>();
        popupImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/水灵/弹窗.png");
        popupImage.preserveAspect = true;
        popupImage.raycastTarget = true;

        CreateLabel("Title", popup.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 235f), new Vector2(570f, 72f),
            "选择城市", "SELECT A CITY", 36, TextAnchor.MiddleCenter, out _);

        controller.cityButtons = new Button[5];
        float[] y = { 92f, 14f, -64f, -142f, -220f };
        for (int i = 0; i < controller.cityButtons.Length; i++)
        {
            controller.cityButtons[i] = CreateButton("City_" + (i + 1), popup.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0f, y[i]), new Vector2(560f, 70f),
                CityNamesZh[i] + "  " + CityNamesEn[i], CityNamesEn[i] + "  " + CityNamesZh[i], 28, out _);
        }

        controller.closeButton = CreateButton("Close", popup.transform,
            new Vector2(0.5f, 0.5f), new Vector2(382f, 190f), new Vector2(86f, 86f),
            "×", "×", 46, out Image closeImage);
        closeImage.color = new Color(1f, 1f, 1f, 0.02f);

        Button[] originalButtons = canvas.GetComponentsInChildren<Button>(true)
            .Where(button => button != controller.languageButton && button != controller.cityDirectoryButton &&
                             button != controller.closeButton && !controller.cityButtons.Contains(button)).ToArray();
        if (originalButtons.Length >= 2)
        {
            AddEnglishOverlay(originalButtons[0], "START JOURNEY");
            AddEnglishOverlay(originalButtons[1], "EXIT JOURNEY");
        }

        panelRoot.SetActive(false);
        panelRoot.transform.SetAsLastSibling();
    }

    private static void AuthorCity(Canvas canvas)
    {
        ConfigurePortraitCanvas(canvas);
        DestroyChild(canvas.transform, "MinimalLanguageButton");
        DestroyChild(canvas.transform, "MinimalReturnDirectoryButton");
        DestroyChild(canvas.transform, "MinimalSpeedButton");
        CityDirectoryController oldController = canvas.GetComponent<CityDirectoryController>();
        if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);
        VideoSpeedController oldSpeedController = canvas.GetComponent<VideoSpeedController>();
        if (oldSpeedController != null) UnityEngine.Object.DestroyImmediate(oldSpeedController);
        CityDirectoryController controller = canvas.gameObject.AddComponent<CityDirectoryController>();
        controller.returnDirectoryButton = CreateButton("MinimalReturnDirectoryButton", canvas.transform,
            new Vector2(0f, 1f), new Vector2(155f, -40f), new Vector2(280f, 58f),
            "返回城市目录", "CITY DIRECTORY", 22, out _);
        controller.languageButton = CreateButton("MinimalLanguageButton", canvas.transform,
            new Vector2(1f, 1f), new Vector2(-115f, -40f), new Vector2(190f, 58f),
            "中文 | EN", "中文 | EN", 22, out _);
        Button speedButton = CreateButton("MinimalSpeedButton", canvas.transform,
            new Vector2(1f, 1f), new Vector2(-115f, -108f), new Vector2(190f, 58f),
            "语速 1×", "SPEED 1×", 22, out _);
        Text speedLabel = speedButton.GetComponentInChildren<Text>(true);
        BilingualText bilingualSpeedLabel = speedLabel != null ? speedLabel.GetComponent<BilingualText>() : null;
        if (bilingualSpeedLabel != null) UnityEngine.Object.DestroyImmediate(bilingualSpeedLabel);
        VideoQuizManager manager = UnityEngine.Object.FindFirstObjectByType<VideoQuizManager>();
        VideoSpeedController speedController = canvas.gameObject.AddComponent<VideoSpeedController>();
        speedController.videoPlayer = manager != null ? manager.videoPlayer : null;
        speedController.speedButton = speedButton;
        speedController.speedLabel = speedLabel;
        controller.returnDirectoryButton.transform.SetAsLastSibling();
        controller.languageButton.transform.SetAsLastSibling();
        speedButton.transform.SetAsLastSibling();
    }

    private static int InjectEnglishQuizData(string sceneName)
    {
        string path = Path.Combine(EnglishDataRoot, sceneName + ".json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("English city data missing", path);
        }
        CityDocument document = JsonUtility.FromJson<CityDocument>(File.ReadAllText(path));
        VideoQuizManager manager = UnityEngine.Object.FindFirstObjectByType<VideoQuizManager>();
        if (manager == null || document == null || document.segments == null)
        {
            throw new InvalidDataException(sceneName + ": could not load quiz localization data.");
        }

        int count = 0;
        int limit = Mathf.Min(manager.quizList.Count, document.segments.Length);
        for (int i = 0; i < limit; i++)
        {
            Question source = document.segments[i].question;
            if (source == null || string.IsNullOrWhiteSpace(manager.quizList[i].question))
            {
                continue;
            }
            VideoQuizManager.QuizData target = manager.quizList[i];
            target.questionEnglish = source.questionEn;
            target.answersEnglish = source.options != null
                ? source.options.Select(option => option.textEn).ToArray()
                : Array.Empty<string>();
            target.successMessageEnglish = source.successEn;
            count++;
        }
        EditorUtility.SetDirty(manager);
        return count;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
        string chinese, string english, int fontSize, out Image image)
    {
        GameObject host = CreateRect(name, parent, anchor, anchor, position, size);
        image = host.AddComponent<Image>();
        image.color = new Color(0.82f, 0.94f, 0.99f, 0.96f);
        image.raycastTarget = true;
        Outline outline = host.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.48f, 0.76f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);
        Button button = host.AddComponent<Button>();
        button.targetGraphic = image;
        CreateLabel("Label", host.transform, Vector2.one * 0.5f, Vector2.zero, size - new Vector2(18f, 10f),
            chinese, english, fontSize, TextAnchor.MiddleCenter, out _);
        return button;
    }

    private static GameObject CreateLabel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
        string chinese, string english, int fontSize, TextAnchor alignment, out Text text)
    {
        GameObject host = CreateRect(name, parent, anchor, anchor, position, size);
        text = host.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = new Color(0.08f, 0.31f, 0.62f, 1f);
        text.alignment = alignment;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 13;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;
        BilingualText bilingual = host.AddComponent<BilingualText>();
        bilingual.targetText = text;
        bilingual.chineseText = chinese;
        bilingual.englishText = english;
        return host;
    }

    private static void AddEnglishOverlay(Button button, string english)
    {
        Transform old = button.transform.Find("EnglishOverlay");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        GameObject overlay = CreateRect("EnglishOverlay", button.transform,
            new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.27f), Vector2.zero, Vector2.zero);
        Image background = overlay.AddComponent<Image>();
        background.color = new Color(0.86f, 0.96f, 1f, 0.97f);
        background.raycastTarget = false;
        CreateLabel("Label", overlay.transform, Vector2.zero, Vector2.zero, Vector2.zero, string.Empty, english, 31,
            TextAnchor.MiddleCenter, out Text text);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        BilingualText bilingual = text.GetComponent<BilingualText>();
        bilingual.hideWhenEmpty = true;
        bilingual.englishOverlayBackground = background;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject host = new GameObject(name, typeof(RectTransform));
        host.layer = 5;
        RectTransform rect = host.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = Vector2.one * 0.5f;
        if (anchorMin == anchorMax)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        else
        {
            rect.offsetMin = position;
            rect.offsetMax = size;
        }
        return host;
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void ConfigurePortraitCanvas(Canvas canvas)
    {
        canvas.transform.localScale = Vector3.one;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}
