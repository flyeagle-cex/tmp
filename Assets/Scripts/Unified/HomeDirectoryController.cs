using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class HomeDirectoryController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject introLayer;
    private GameObject directoryLayer;
    private GameObject confirmLayer;
    private VideoPlayer introPlayer;
    private TMP_Text languageLabel;
    private TMP_Text soundLabel;

    private void Awake()
    {
        DisableLegacyStartControls();
        introPlayer = FindFirstObjectByType<VideoPlayer>();
        BuildInterface();
        BindSettings();

        if (introPlayer == null)
        {
            ShowDirectory();
            return;
        }

        introPlayer.isLooping = false;
        introPlayer.loopPointReached -= OnIntroFinished;
        introPlayer.loopPointReached += OnIntroFinished;
        introPlayer.errorReceived -= OnIntroError;
        introPlayer.errorReceived += OnIntroError;
        ShowIntro();
    }

    private void DisableLegacyStartControls()
    {
        Canvas[] legacyCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < legacyCanvases.Length; i++)
        {
            Graphic[] graphics = legacyCanvases[i].GetComponentsInChildren<Graphic>(true);
            for (int j = 0; j < graphics.Length; j++)
            {
                if (!(graphics[j] is RawImage))
                {
                    graphics[j].enabled = false;
                }
            }

            Button[] buttons = legacyCanvases[i].GetComponentsInChildren<Button>(true);
            for (int j = 0; j < buttons.Length; j++)
            {
                buttons[j].interactable = false;
            }
        }
    }

    private void BuildInterface()
    {
        canvas = RuntimeUiFactory.CreateCanvas("JiangnanHomeCanvas", 900);
        canvas.transform.SetParent(transform, false);

        introLayer = RuntimeUiFactory.CreateRect("IntroLayer", canvas.transform).gameObject;
        RuntimeUiFactory.Stretch((RectTransform)introLayer.transform);
        BuildIntroLayer(introLayer.transform);

        directoryLayer = RuntimeUiFactory.CreatePanel("DirectoryLayer", canvas.transform, RuntimeUiFactory.DeepBlue).gameObject;
        RuntimeUiFactory.Stretch((RectTransform)directoryLayer.transform);
        BuildDirectoryLayer(directoryLayer.transform);

        confirmLayer = BuildConfirmation(canvas.transform);
        confirmLayer.SetActive(false);
    }

    private void BuildIntroLayer(Transform parent)
    {
        Image topShade = RuntimeUiFactory.CreatePanel("TopShade", parent, new Color(0.02f, 0.08f, 0.12f, 0.84f));
        RuntimeUiFactory.SetRect(topShade.rectTransform, new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text title = RuntimeUiFactory.CreateText("Title", topShade.transform, "灵舟苏韵  ·  LINGZHOU JIANGNAN", 38f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.055f, 0f), new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero);
        title.fontStyle = FontStyles.Bold;

        Button enter = RuntimeUiFactory.CreateButton("EnterDirectory", parent, "进入城市目录  ENTER", ShowDirectory, RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 25f);
        RuntimeUiFactory.SetRect((RectTransform)enter.transform, new Vector2(0.775f, 0.055f), new Vector2(0.955f, 0.13f), Vector2.zero, Vector2.zero);

        TMP_Text hint = RuntimeUiFactory.CreateText("IntroHint", parent, "江南城市文化互动展  ·  INTRO FILM", 21f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(hint.rectTransform, new Vector2(0.31f, 0.04f), new Vector2(0.69f, 0.095f), Vector2.zero, Vector2.zero);
    }

    private void BuildDirectoryLayer(Transform parent)
    {
        Image header = RuntimeUiFactory.CreatePanel("Header", parent, RuntimeUiFactory.Navy);
        RuntimeUiFactory.SetRect(header.rectTransform, new Vector2(0f, 0.84f), Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text title = RuntimeUiFactory.CreateText("Brand", header.transform, "灵舟苏韵", 48f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.045f, 0.24f), new Vector2(0.35f, 0.9f), Vector2.zero, Vector2.zero);
        title.fontStyle = FontStyles.Bold;
        TMP_Text subtitle = RuntimeUiFactory.CreateText("BrandEnglish", header.transform, "A JOURNEY THROUGH JIANGSU'S LIVING CULTURE", 18f, RuntimeUiFactory.Gold);
        RuntimeUiFactory.SetRect(subtitle.rectTransform, new Vector2(0.047f, 0.07f), new Vector2(0.53f, 0.36f), Vector2.zero, Vector2.zero);

        Button language = RuntimeUiFactory.CreateButton("Language", header.transform, string.Empty, ToggleLanguage, RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 20f);
        RuntimeUiFactory.SetRect((RectTransform)language.transform, new Vector2(0.69f, 0.28f), new Vector2(0.79f, 0.72f), Vector2.zero, Vector2.zero);
        languageLabel = language.GetComponentInChildren<TMP_Text>();

        Button sound = RuntimeUiFactory.CreateButton("Sound", header.transform, string.Empty, ToggleSound, RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 20f);
        RuntimeUiFactory.SetRect((RectTransform)sound.transform, new Vector2(0.805f, 0.28f), new Vector2(0.895f, 0.72f), Vector2.zero, Vector2.zero);
        soundLabel = sound.GetComponentInChildren<TMP_Text>();

        Button exit = RuntimeUiFactory.CreateButton("Exit", header.transform, "退出  EXIT", () => confirmLayer.SetActive(true), RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 20f);
        RuntimeUiFactory.SetRect((RectTransform)exit.transform, new Vector2(0.91f, 0.28f), new Vector2(0.975f, 0.72f), Vector2.zero, Vector2.zero);

        TMP_Text section = RuntimeUiFactory.CreateText("SectionTitle", parent, "选择一座城市  ·  SELECT A CITY", 29f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(section.rectTransform, new Vector2(0.045f, 0.765f), new Vector2(0.7f, 0.83f), Vector2.zero, Vector2.zero);

        ScrollRect scroll = CreateCityScroll(parent);
        RuntimeUiFactory.SetRect((RectTransform)scroll.transform, new Vector2(0.035f, 0.075f), new Vector2(0.965f, 0.75f), Vector2.zero, Vector2.zero);
    }

    private ScrollRect CreateCityScroll(Transform parent)
    {
        Image viewportImage = RuntimeUiFactory.CreatePanel("CityScroll", parent, new Color(0f, 0f, 0f, 0f));
        Mask mask = viewportImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        ScrollRect scroll = viewportImage.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 55f;

        RectTransform content = RuntimeUiFactory.CreateRect("Content", viewportImage.transform);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewportImage.rectTransform;
        scroll.content = content;

        CityRegistryEntry[] cities = CityRegistry.GetAll();
        for (int i = 0; i < cities.Length; i++)
        {
            BuildCityCard(content, cities[i]);
        }
        return scroll;
    }

    private void BuildCityCard(Transform parent, CityRegistryEntry entry)
    {
        bool available = entry != null && entry.isAvailable;
        Image card = RuntimeUiFactory.CreatePanel("City_" + entry.id, parent, available ? RuntimeUiFactory.PanelBlue : new Color32(28, 43, 53, 255));
        LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 286f;
        element.minWidth = 286f;

        RawImage thumbnail = RuntimeUiFactory.CreateRect("Thumbnail", card.transform).gameObject.AddComponent<RawImage>();
        RuntimeUiFactory.SetRect(thumbnail.rectTransform, new Vector2(0f, 0.38f), Vector2.one, Vector2.zero, Vector2.zero);
        thumbnail.color = available ? Color.white : new Color32(76, 83, 86, 255);
        if (available && !string.IsNullOrWhiteSpace(entry.thumbnailResource))
        {
            thumbnail.texture = Resources.Load<Texture2D>(entry.thumbnailResource);
            thumbnail.uvRect = new Rect(0f, 0.18f, 1f, 0.64f);
        }
        if (available && thumbnail.texture == null)
        {
            CityInteractionData data = CityDataRepository.Load(entry.sceneName);
            if (data != null && data.segments != null && data.segments.Length > 0 && data.segments[0] != null)
            {
                thumbnail.gameObject.AddComponent<RuntimeVideoThumbnail>().Initialize(thumbnail, data.segments[0].mediaFile);
            }
        }

        Image accent = RuntimeUiFactory.CreatePanel("Accent", card.transform, available ? RuntimeUiFactory.Jade : RuntimeUiFactory.Muted);
        RuntimeUiFactory.SetRect(accent.rectTransform, new Vector2(0f, 0.355f), new Vector2(1f, 0.375f), Vector2.zero, Vector2.zero);

        TMP_Text cityName = RuntimeUiFactory.CreateText("CityName", card.transform, entry.GetName(LanguageManager.EnsureExists().CurrentLanguage), 36f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(cityName.rectTransform, new Vector2(0.075f, 0.23f), new Vector2(0.925f, 0.345f), Vector2.zero, Vector2.zero);
        cityName.fontStyle = FontStyles.Bold;

        TMP_Text keyword = RuntimeUiFactory.CreateText("Keywords", card.transform, entry.GetKeywords(LanguageManager.EnsureExists().CurrentLanguage), 18f, available ? RuntimeUiFactory.Muted : new Color32(120, 132, 138, 255));
        RuntimeUiFactory.SetRect(keyword.rectTransform, new Vector2(0.075f, 0.115f), new Vector2(0.925f, 0.23f), Vector2.zero, Vector2.zero);

        string action = available ? "进入  ENTER" : "即将开放  COMING SOON";
        Button button = RuntimeUiFactory.CreateButton("Action", card.transform, action, available ? () => NavigationManager.EnsureExists().OpenCity(entry) : null, available ? RuntimeUiFactory.Jade : new Color32(55, 67, 73, 255), available ? RuntimeUiFactory.DeepBlue : RuntimeUiFactory.Muted, 17f);
        RuntimeUiFactory.SetRect((RectTransform)button.transform, new Vector2(0.075f, 0.025f), new Vector2(0.925f, 0.105f), Vector2.zero, Vector2.zero);
        button.interactable = available;

        if (available && AppState.EnsureExists().IsCityCompleted(entry.id))
        {
            TMP_Text completed = RuntimeUiFactory.CreateText("Completed", card.transform, "✓ 已完成  COMPLETED", 16f, RuntimeUiFactory.Gold, TextAlignmentOptions.Center);
            RuntimeUiFactory.SetRect(completed.rectTransform, new Vector2(0.43f, 0.91f), new Vector2(0.96f, 0.975f), Vector2.zero, Vector2.zero);
        }
    }

    private GameObject BuildConfirmation(Transform parent)
    {
        Image shade = RuntimeUiFactory.CreatePanel("ExitConfirmation", parent, new Color(0f, 0f, 0f, 0.74f));
        RuntimeUiFactory.Stretch(shade.rectTransform);
        Image card = RuntimeUiFactory.CreatePanel("Card", shade.transform, RuntimeUiFactory.Navy);
        RuntimeUiFactory.SetRect(card.rectTransform, new Vector2(0.35f, 0.34f), new Vector2(0.65f, 0.66f), Vector2.zero, Vector2.zero);
        TMP_Text question = RuntimeUiFactory.CreateText("Question", card.transform, "确认退出体验？\nEXIT THE EXPERIENCE?", 28f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(question.rectTransform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
        Button cancel = RuntimeUiFactory.CreateButton("Cancel", card.transform, "取消  CANCEL", () => shade.gameObject.SetActive(false), RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 20f);
        RuntimeUiFactory.SetRect((RectTransform)cancel.transform, new Vector2(0.08f, 0.12f), new Vector2(0.46f, 0.34f), Vector2.zero, Vector2.zero);
        Button confirm = RuntimeUiFactory.CreateButton("Confirm", card.transform, "确认  EXIT", QuitApplication, RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 20f);
        RuntimeUiFactory.SetRect((RectTransform)confirm.transform, new Vector2(0.54f, 0.12f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero);
        return shade.gameObject;
    }

    private void BindSettings()
    {
        LanguageManager settings = LanguageManager.EnsureExists();
        settings.LanguageChanged -= RebuildDirectory;
        settings.LanguageChanged += RebuildDirectory;
        settings.SoundChanged -= RefreshSettingsLabels;
        settings.SoundChanged += RefreshSettingsLabels;
        RefreshSettingsLabels();
    }

    private void ToggleLanguage()
    {
        LanguageManager.EnsureExists().ToggleLanguage();
    }

    private void ToggleSound()
    {
        LanguageManager.EnsureExists().ToggleSound();
    }

    private void RefreshSettingsLabels()
    {
        LanguageManager settings = LanguageManager.EnsureExists();
        if (languageLabel != null)
        {
            languageLabel.text = settings.CurrentLanguage == AppLanguage.Chinese ? "中文  CN" : "EN  English";
        }
        if (soundLabel != null)
        {
            soundLabel.text = settings.SoundEnabled ? "声音  ON" : "声音  OFF";
        }
    }

    private void RebuildDirectory()
    {
        bool visible = directoryLayer != null && directoryLayer.activeSelf;
        if (directoryLayer != null)
        {
            Destroy(directoryLayer);
        }
        directoryLayer = RuntimeUiFactory.CreatePanel("DirectoryLayer", canvas.transform, RuntimeUiFactory.DeepBlue).gameObject;
        RuntimeUiFactory.Stretch((RectTransform)directoryLayer.transform);
        BuildDirectoryLayer(directoryLayer.transform);
        directoryLayer.SetActive(visible);
        confirmLayer.transform.SetAsLastSibling();
        RefreshSettingsLabels();
    }

    private void ShowIntro()
    {
        introLayer.SetActive(true);
        directoryLayer.SetActive(false);
    }

    private void ShowDirectory()
    {
        if (introPlayer != null)
        {
            introPlayer.Pause();
        }
        introLayer.SetActive(false);
        directoryLayer.SetActive(true);
    }

    private void OnIntroFinished(VideoPlayer player)
    {
        ShowDirectory();
    }

    private void OnIntroError(VideoPlayer player, string message)
    {
        Debug.LogWarning("Intro video could not play: " + message);
        ShowDirectory();
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (introPlayer != null)
        {
            introPlayer.loopPointReached -= OnIntroFinished;
            introPlayer.errorReceived -= OnIntroError;
        }
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged -= RebuildDirectory;
            LanguageManager.Instance.SoundChanged -= RefreshSettingsLabels;
        }
    }
}
