using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class ExhibitionCityView : MonoBehaviour
{
    public event Action StartRequested;
    public event Action<string> AnswerRequested;
    public event Action FeedbackActionRequested;
    public event Action CompleteRequested;
    public event Action ExitRequested;
    public event Action ExitConfirmed;

    public CanvasGroup FadeGroup { get; private set; }
    public bool SettingsVisible => settingsPanel != null && settingsPanel.activeSelf;
    public bool ExitConfirmationVisible => directoryConfirmPanel != null && directoryConfirmPanel.activeSelf;

    private CityInteractionData cityData;
    private LanguageManager settings;
    private RectTransform root;
    private TMP_Text cityText;
    private TMP_Text chapterText;
    private TMP_Text cityDescription;
    private TMP_Text languageCnText;
    private TMP_Text languageEnText;
    private TMP_Text ccText;
    private TMP_Text soundText;
    private TMP_Text cultureHeading;
    private TMP_Text cultureSummary;
    private TMP_Text cultureActionText;
    private TMP_Text subtitleText;
    private TMP_Text introCityText;
    private TMP_Text introTitleText;
    private TMP_Text introActionText;
    private TMP_Text questionHeading;
    private TMP_Text questionBody;
    private TMP_Text feedbackHeading;
    private TMP_Text feedbackBody;
    private TMP_Text feedbackActionText;
    private TMP_Text completeHeading;
    private TMP_Text completeBody;
    private TMP_Text completeActionText;
    private TMP_Text detailHeading;
    private TMP_Text detailBody;
    private TMP_Text settingsHeading;
    private TMP_Text directoryConfirmText;
    private TMP_Text quitConfirmText;

    private GameObject introPanel;
    private GameObject subtitlePanel;
    private GameObject quizPanel;
    private GameObject feedbackPanel;
    private GameObject completePanel;
    private GameObject cultureCuePanel;
    private GameObject cultureDetailPanel;
    private GameObject settingsPanel;
    private GameObject directoryConfirmPanel;
    private GameObject quitConfirmPanel;
    private Button cultureAction;
    private Button[] answerButtons;
    private TMP_Text[] answerTexts;
    private Button[] subtitleModeButtons;
    private TMP_Text[] subtitleModeTexts;
    private Button[] subtitleSizeButtons;
    private TMP_Text[] subtitleSizeTexts;
    private Button languageCnButton;
    private Button languageEnButton;
    private Button ccButton;
    private Button soundButton;

    private QuestionData currentQuestion;
    private string currentChapterZh;
    private string currentChapterEn;
    private string cultureTitleZh;
    private string cultureTitleEn;
    private string cultureBodyZh;
    private string cultureBodyEn;
    private string feedbackBodyZh;
    private string feedbackBodyEn;
    private bool lastFeedbackCorrect;

    public void Initialize(RectTransform canvasRoot, CityInteractionData data)
    {
        cityData = data;
        settings = LanguageManager.EnsureExists();
        Build(canvasRoot);
        settings.LanguageChanged += RefreshLanguage;
        settings.SubtitleSettingsChanged += RefreshSettings;
        settings.SoundChanged += RefreshSound;
        RefreshLanguage();
        ShowIntro();
    }

    private void Build(RectTransform canvasRoot)
    {
        root = RuntimeUiFactory.CreatePanel("CityInteractionRoot", canvasRoot, RuntimeUiFactory.DeepBlue).rectTransform;
        RuntimeUiFactory.Stretch(root);
        root.SetAsLastSibling();

        BuildHeader();
        BuildLeftRail();
        BuildVideoStage();
        BuildCultureRail();
        BuildIntro();
        BuildQuiz();
        BuildFeedback();
        BuildComplete();
        BuildSettings();
        directoryConfirmPanel = BuildConfirmation("DirectoryConfirmation", "返回城市目录？\nRETURN TO CITY DIRECTORY?", () => ExitConfirmed?.Invoke());
        quitConfirmPanel = BuildConfirmation("QuitConfirmation", "确认退出体验？\nEXIT THE EXPERIENCE?", QuitApplication);
        BuildFade();
    }

    private void BuildHeader()
    {
        Image header = RuntimeUiFactory.CreatePanel("TopBar", root, RuntimeUiFactory.Navy);
        RuntimeUiFactory.SetRect(header.rectTransform, new Vector2(0f, 0.9f), Vector2.one, Vector2.zero, Vector2.zero);

        cityText = RuntimeUiFactory.CreateText("CityTitle", header.transform, string.Empty, 35f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(cityText.rectTransform, new Vector2(0.035f, 0.18f), new Vector2(0.24f, 0.88f), Vector2.zero, Vector2.zero);
        cityText.fontStyle = FontStyles.Bold;
        chapterText = RuntimeUiFactory.CreateText("ChapterTitle", header.transform, string.Empty, 23f, RuntimeUiFactory.Gold, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(chapterText.rectTransform, new Vector2(0.25f, 0.18f), new Vector2(0.56f, 0.88f), Vector2.zero, Vector2.zero);

        languageCnButton = HeaderButton("LanguageCN", header.transform, "中文", () => settings.SetLanguage(AppLanguage.Chinese), 0.575f, 0.635f);
        languageCnText = languageCnButton.GetComponentInChildren<TMP_Text>();
        languageEnButton = HeaderButton("LanguageEN", header.transform, "EN", () => settings.SetLanguage(AppLanguage.English), 0.642f, 0.697f);
        languageEnText = languageEnButton.GetComponentInChildren<TMP_Text>();
        ccButton = HeaderButton("Subtitle", header.transform, "CC", ToggleSettings, 0.705f, 0.765f);
        ccText = ccButton.GetComponentInChildren<TMP_Text>();
        soundButton = HeaderButton("Sound", header.transform, "声音", () => settings.ToggleSound(), 0.772f, 0.845f);
        soundText = soundButton.GetComponentInChildren<TMP_Text>();
        HeaderButton("Directory", header.transform, "目录", () => ExitRequested?.Invoke(), 0.852f, 0.915f);
        HeaderButton("Quit", header.transform, "退出", () => quitConfirmPanel.SetActive(true), 0.922f, 0.982f);
    }

    private Button HeaderButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action, float left, float right)
    {
        Button button = RuntimeUiFactory.CreateButton(name, parent, label, action, RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 18f);
        RuntimeUiFactory.SetRect((RectTransform)button.transform, new Vector2(left, 0.22f), new Vector2(right, 0.78f), Vector2.zero, Vector2.zero);
        return button;
    }

    private void BuildLeftRail()
    {
        Image rail = RuntimeUiFactory.CreatePanel("CityInformation", root, RuntimeUiFactory.PanelBlue);
        RuntimeUiFactory.SetRect(rail.rectTransform, new Vector2(0.03f, 0.065f), new Vector2(0.265f, 0.865f), Vector2.zero, Vector2.zero);

        TMP_Text label = RuntimeUiFactory.CreateText("Label", rail.transform, "CITY ARCHIVE  ·  城市档案", 17f, RuntimeUiFactory.Gold);
        RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.965f), Vector2.zero, Vector2.zero);
        TMP_Text bigCity = RuntimeUiFactory.CreateText("BigCity", rail.transform, cityData.cityNameZh, 58f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(bigCity.rectTransform, new Vector2(0.075f, 0.73f), new Vector2(0.92f, 0.89f), Vector2.zero, Vector2.zero);
        bigCity.fontStyle = FontStyles.Bold;
        cityDescription = RuntimeUiFactory.CreateText("Description", rail.transform, string.Empty, 23f, RuntimeUiFactory.Muted);
        RuntimeUiFactory.SetRect(cityDescription.rectTransform, new Vector2(0.08f, 0.57f), new Vector2(0.92f, 0.73f), Vector2.zero, Vector2.zero);

        Image line = RuntimeUiFactory.CreatePanel("Line", rail.transform, RuntimeUiFactory.Jade);
        RuntimeUiFactory.SetRect(line.rectTransform, new Vector2(0.08f, 0.535f), new Vector2(0.92f, 0.54f), Vector2.zero, Vector2.zero);
        TMP_Text journey = RuntimeUiFactory.CreateText("Journey", rail.transform, BuildJourneyText(), 20f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(journey.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.5f), Vector2.zero, Vector2.zero);
        journey.lineSpacing = 10f;
    }

    private string BuildJourneyText()
    {
        if (cityData.segments == null)
        {
            return string.Empty;
        }
        string value = "";
        int count = Mathf.Min(cityData.segments.Length, 8);
        for (int i = 0; i < count; i++)
        {
            VideoSegmentData segment = cityData.segments[i];
            string title = segment != null ? segment.GetChapter(settings != null ? settings.CurrentLanguage : AppLanguage.Chinese) : string.Empty;
            value += (i + 1).ToString("00") + "   " + title + (i == count - 1 ? string.Empty : "\n");
        }
        return value;
    }

    private void BuildVideoStage()
    {
        Image stage = RuntimeUiFactory.CreatePanel("VideoStage", root, new Color32(4, 14, 24, 255));
        RuntimeUiFactory.SetRect(stage.rectTransform, new Vector2(0.285f, 0.205f), new Vector2(0.635f, 0.865f), Vector2.zero, Vector2.zero);

        VideoPlayer player = GetComponentInChildren<VideoPlayer>(true);
        RawImage raw = player != null ? player.GetComponent<RawImage>() : null;
        if (raw != null)
        {
            RectTransform videoRect = raw.rectTransform;
            videoRect.SetParent(stage.transform, false);
            RuntimeUiFactory.Stretch(videoRect, 18f, 18f, 18f, 18f);
            raw.color = Color.white;
            raw.raycastTarget = false;
            videoRect.SetAsLastSibling();
        }

        Image strip = RuntimeUiFactory.CreatePanel("SubtitleLayer", root, new Color32(5, 17, 27, 248));
        RuntimeUiFactory.SetRect(strip.rectTransform, new Vector2(0.285f, 0.065f), new Vector2(0.635f, 0.185f), Vector2.zero, Vector2.zero);
        subtitlePanel = strip.gameObject;
        subtitleText = RuntimeUiFactory.CreateText("SubtitleText", strip.transform, string.Empty, 31f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.Stretch(subtitleText.rectTransform, 24f, 12f, 24f, 12f);
        subtitleText.overflowMode = TextOverflowModes.Overflow;
        subtitleText.enableAutoSizing = true;
        subtitleText.fontSizeMin = 20f;
        subtitleText.fontSizeMax = 35f;
        subtitlePanel.SetActive(false);
    }

    private void BuildCultureRail()
    {
        Image rail = RuntimeUiFactory.CreatePanel("CultureRail", root, RuntimeUiFactory.PanelBlue);
        RuntimeUiFactory.SetRect(rail.rectTransform, new Vector2(0.655f, 0.065f), new Vector2(0.97f, 0.865f), Vector2.zero, Vector2.zero);
        TMP_Text label = RuntimeUiFactory.CreateText("Label", rail.transform, "CULTURAL CLUE  ·  文化线索", 17f, RuntimeUiFactory.Gold);
        RuntimeUiFactory.SetRect(label.rectTransform, new Vector2(0.07f, 0.89f), new Vector2(0.93f, 0.965f), Vector2.zero, Vector2.zero);
        cultureHeading = RuntimeUiFactory.CreateText("Heading", rail.transform, "等待影片中的文化线索", 35f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(cultureHeading.rectTransform, new Vector2(0.07f, 0.68f), new Vector2(0.93f, 0.88f), Vector2.zero, Vector2.zero);
        cultureHeading.fontStyle = FontStyles.Bold;
        cultureSummary = RuntimeUiFactory.CreateText("Summary", rail.transform, "线索会随影片时间轴出现。", 23f, RuntimeUiFactory.Muted);
        RuntimeUiFactory.SetRect(cultureSummary.rectTransform, new Vector2(0.07f, 0.31f), new Vector2(0.93f, 0.66f), Vector2.zero, Vector2.zero);
        cultureAction = RuntimeUiFactory.CreateButton("CultureAction", rail.transform, "查看文化详情", ShowCultureDetail, RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 22f);
        RuntimeUiFactory.SetRect((RectTransform)cultureAction.transform, new Vector2(0.07f, 0.09f), new Vector2(0.56f, 0.19f), Vector2.zero, Vector2.zero);
        cultureActionText = cultureAction.GetComponentInChildren<TMP_Text>();
        cultureAction.interactable = false;
        cultureCuePanel = rail.gameObject;

        Image detail = RuntimeUiFactory.CreatePanel("CultureDetailLayer", root, new Color(0f, 0f, 0f, 0.78f));
        RuntimeUiFactory.Stretch(detail.rectTransform);
        Image card = RuntimeUiFactory.CreatePanel("DetailCard", detail.transform, RuntimeUiFactory.Navy);
        RuntimeUiFactory.SetRect(card.rectTransform, new Vector2(0.25f, 0.19f), new Vector2(0.75f, 0.81f), Vector2.zero, Vector2.zero);
        detailHeading = RuntimeUiFactory.CreateText("Heading", card.transform, string.Empty, 42f, RuntimeUiFactory.Paper);
        RuntimeUiFactory.SetRect(detailHeading.rectTransform, new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);
        detailBody = RuntimeUiFactory.CreateText("Body", card.transform, string.Empty, 28f, RuntimeUiFactory.Muted);
        RuntimeUiFactory.SetRect(detailBody.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);
        Button close = RuntimeUiFactory.CreateButton("Close", card.transform, "关闭  CLOSE", () => detail.gameObject.SetActive(false), RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 22f);
        RuntimeUiFactory.SetRect((RectTransform)close.transform, new Vector2(0.34f, 0.08f), new Vector2(0.66f, 0.19f), Vector2.zero, Vector2.zero);
        cultureDetailPanel = detail.gameObject;
        cultureDetailPanel.SetActive(false);
    }

    private void BuildIntro()
    {
        Image shade = RuntimeUiFactory.CreatePanel("IntroLayer", root, new Color(0.01f, 0.04f, 0.07f, 0.88f));
        RuntimeUiFactory.Stretch(shade.rectTransform);
        Image card = RuntimeUiFactory.CreatePanel("IntroCard", shade.transform, RuntimeUiFactory.Navy);
        RuntimeUiFactory.SetRect(card.rectTransform, new Vector2(0.28f, 0.27f), new Vector2(0.72f, 0.73f), Vector2.zero, Vector2.zero);
        introCityText = RuntimeUiFactory.CreateText("IntroCity", card.transform, string.Empty, 64f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(introCityText.rectTransform, new Vector2(0.08f, 0.57f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero);
        introCityText.fontStyle = FontStyles.Bold;
        introTitleText = RuntimeUiFactory.CreateText("IntroTitle", card.transform, string.Empty, 27f, RuntimeUiFactory.Gold, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(introTitleText.rectTransform, new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);
        Button start = RuntimeUiFactory.CreateButton("StartButton", card.transform, string.Empty, () => StartRequested?.Invoke(), RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 26f);
        RuntimeUiFactory.SetRect((RectTransform)start.transform, new Vector2(0.31f, 0.12f), new Vector2(0.69f, 0.3f), Vector2.zero, Vector2.zero);
        introActionText = start.GetComponentInChildren<TMP_Text>();
        introPanel = shade.gameObject;
    }

    private void BuildQuiz()
    {
        Image shade = ModalShade("QuizLayer", out Image card, new Vector2(0.19f, 0.12f), new Vector2(0.81f, 0.86f));
        questionHeading = RuntimeUiFactory.CreateText("Heading", card.transform, string.Empty, 27f, RuntimeUiFactory.Gold, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(questionHeading.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
        questionBody = RuntimeUiFactory.CreateText("Question", card.transform, string.Empty, 34f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(questionBody.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
        answerButtons = new Button[4];
        answerTexts = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            int optionIndex = i;
            answerButtons[i] = RuntimeUiFactory.CreateButton("Option" + (i + 1), card.transform, string.Empty, () => RequestAnswer(optionIndex), RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 24f);
            float top = 0.57f - (i * 0.115f);
            RuntimeUiFactory.SetRect((RectTransform)answerButtons[i].transform, new Vector2(0.1f, top - 0.095f), new Vector2(0.9f, top), Vector2.zero, Vector2.zero);
            answerTexts[i] = answerButtons[i].GetComponentInChildren<TMP_Text>();
            answerTexts[i].alignment = TextAlignmentOptions.Left;
        }
        quizPanel = shade.gameObject;
        quizPanel.SetActive(false);
    }

    private void BuildFeedback()
    {
        Image shade = ModalShade("FeedbackLayer", out Image card, new Vector2(0.31f, 0.28f), new Vector2(0.69f, 0.72f));
        feedbackHeading = RuntimeUiFactory.CreateText("Heading", card.transform, string.Empty, 40f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(feedbackHeading.rectTransform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
        feedbackBody = RuntimeUiFactory.CreateText("Body", card.transform, string.Empty, 25f, RuntimeUiFactory.Muted, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(feedbackBody.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f), Vector2.zero, Vector2.zero);
        Button action = RuntimeUiFactory.CreateButton("Action", card.transform, string.Empty, () => FeedbackActionRequested?.Invoke(), RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 23f);
        RuntimeUiFactory.SetRect((RectTransform)action.transform, new Vector2(0.28f, 0.1f), new Vector2(0.72f, 0.27f), Vector2.zero, Vector2.zero);
        feedbackActionText = action.GetComponentInChildren<TMP_Text>();
        feedbackPanel = shade.gameObject;
        feedbackPanel.SetActive(false);
    }

    private void BuildComplete()
    {
        Image shade = ModalShade("CompleteLayer", out Image card, new Vector2(0.28f, 0.25f), new Vector2(0.72f, 0.75f));
        completeHeading = RuntimeUiFactory.CreateText("Heading", card.transform, string.Empty, 44f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(completeHeading.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        completeBody = RuntimeUiFactory.CreateText("Body", card.transform, string.Empty, 27f, RuntimeUiFactory.Muted, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(completeBody.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.64f), Vector2.zero, Vector2.zero);
        Button action = RuntimeUiFactory.CreateButton("Action", card.transform, string.Empty, () => CompleteRequested?.Invoke(), RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 24f);
        RuntimeUiFactory.SetRect((RectTransform)action.transform, new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.28f), Vector2.zero, Vector2.zero);
        completeActionText = action.GetComponentInChildren<TMP_Text>();
        completePanel = shade.gameObject;
        completePanel.SetActive(false);
    }

    private void BuildSettings()
    {
        Image shade = ModalShade("SubtitleSettingsLayer", out Image card, new Vector2(0.29f, 0.16f), new Vector2(0.71f, 0.84f));
        settingsHeading = RuntimeUiFactory.CreateText("Heading", card.transform, "字幕设置  ·  SUBTITLES", 34f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(settingsHeading.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
        TMP_Text note = RuntimeUiFactory.CreateText("HardSubtitleNote", card.transform, "原片多数含内嵌双语字幕；CC 字幕显示在影片下方独立区域。\nMost source videos contain burned-in bilingual captions; CC appears below the video.", 18f, RuntimeUiFactory.Muted, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(note.rectTransform, new Vector2(0.07f, 0.66f), new Vector2(0.93f, 0.81f), Vector2.zero, Vector2.zero);

        subtitleModeButtons = new Button[4];
        subtitleModeTexts = new TMP_Text[4];
        string[] labels = { "中文", "ENGLISH", "双语", "关闭" };
        for (int i = 0; i < 4; i++)
        {
            int mode = i;
            subtitleModeButtons[i] = RuntimeUiFactory.CreateButton("Mode" + i, card.transform, labels[i], () => settings.SetSubtitleMode((SubtitleMode)mode), RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 20f);
            float left = 0.07f + (i % 2) * 0.46f;
            float bottom = i < 2 ? 0.52f : 0.41f;
            RuntimeUiFactory.SetRect((RectTransform)subtitleModeButtons[i].transform, new Vector2(left, bottom), new Vector2(left + 0.4f, bottom + 0.085f), Vector2.zero, Vector2.zero);
            subtitleModeTexts[i] = subtitleModeButtons[i].GetComponentInChildren<TMP_Text>();
        }

        subtitleSizeButtons = new Button[3];
        subtitleSizeTexts = new TMP_Text[3];
        string[] sizes = { "小 SMALL", "中 MEDIUM", "大 LARGE" };
        for (int i = 0; i < 3; i++)
        {
            int size = i;
            subtitleSizeButtons[i] = RuntimeUiFactory.CreateButton("Size" + i, card.transform, sizes[i], () => settings.SetSubtitleSize((SubtitleSize)size), RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 18f);
            float left = 0.07f + i * 0.3f;
            RuntimeUiFactory.SetRect((RectTransform)subtitleSizeButtons[i].transform, new Vector2(left, 0.27f), new Vector2(left + 0.26f, 0.355f), Vector2.zero, Vector2.zero);
            subtitleSizeTexts[i] = subtitleSizeButtons[i].GetComponentInChildren<TMP_Text>();
        }
        Button languageDefault = RuntimeUiFactory.CreateButton("LanguageDefault", card.transform, "跟随界面语言  FOLLOW UI LANGUAGE", settings.UseLanguageDefaultSubtitle, RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 18f);
        RuntimeUiFactory.SetRect((RectTransform)languageDefault.transform, new Vector2(0.19f, 0.15f), new Vector2(0.81f, 0.225f), Vector2.zero, Vector2.zero);
        Button close = RuntimeUiFactory.CreateButton("Close", card.transform, "关闭  CLOSE", () => shade.gameObject.SetActive(false), RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 20f);
        RuntimeUiFactory.SetRect((RectTransform)close.transform, new Vector2(0.32f, 0.045f), new Vector2(0.68f, 0.115f), Vector2.zero, Vector2.zero);
        settingsPanel = shade.gameObject;
        settingsPanel.SetActive(false);
    }

    private Image ModalShade(string name, out Image card, Vector2 cardMin, Vector2 cardMax)
    {
        Image shade = RuntimeUiFactory.CreatePanel(name, root, new Color(0f, 0f, 0f, 0.78f));
        RuntimeUiFactory.Stretch(shade.rectTransform);
        card = RuntimeUiFactory.CreatePanel("Card", shade.transform, RuntimeUiFactory.Navy);
        RuntimeUiFactory.SetRect(card.rectTransform, cardMin, cardMax, Vector2.zero, Vector2.zero);
        return shade;
    }

    private GameObject BuildConfirmation(string name, string message, UnityEngine.Events.UnityAction confirmAction)
    {
        Image shade = ModalShade(name, out Image card, new Vector2(0.33f, 0.32f), new Vector2(0.67f, 0.68f));
        TMP_Text body = RuntimeUiFactory.CreateText("Message", card.transform, message, 29f, RuntimeUiFactory.Paper, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(body.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
        if (name.StartsWith("Directory", StringComparison.Ordinal)) directoryConfirmText = body; else quitConfirmText = body;
        Button cancel = RuntimeUiFactory.CreateButton("Cancel", card.transform, "取消  CANCEL", () => shade.gameObject.SetActive(false), RuntimeUiFactory.PanelBlue, RuntimeUiFactory.Paper, 20f);
        RuntimeUiFactory.SetRect((RectTransform)cancel.transform, new Vector2(0.08f, 0.12f), new Vector2(0.46f, 0.35f), Vector2.zero, Vector2.zero);
        Button confirm = RuntimeUiFactory.CreateButton("Confirm", card.transform, "确认  CONFIRM", confirmAction, RuntimeUiFactory.Jade, RuntimeUiFactory.DeepBlue, 20f);
        RuntimeUiFactory.SetRect((RectTransform)confirm.transform, new Vector2(0.54f, 0.12f), new Vector2(0.92f, 0.35f), Vector2.zero, Vector2.zero);
        shade.gameObject.SetActive(false);
        return shade.gameObject;
    }

    private void BuildFade()
    {
        Image fade = RuntimeUiFactory.CreatePanel("TransitionFade", root, RuntimeUiFactory.DeepBlue);
        RuntimeUiFactory.Stretch(fade.rectTransform);
        TMP_Text motif = RuntimeUiFactory.CreateText("WaterMotif", fade.transform, "水纹流转  ·  JOURNEY FLOWS ON", 25f, RuntimeUiFactory.Gold, TextAlignmentOptions.Center);
        RuntimeUiFactory.SetRect(motif.rectTransform, new Vector2(0.31f, 0.45f), new Vector2(0.69f, 0.55f), Vector2.zero, Vector2.zero);
        for (int i = 0; i < 3; i++)
        {
            Image ripple = RuntimeUiFactory.CreatePanel("Ripple" + (i + 1), fade.transform, new Color(0.3f, 0.7f, 0.65f, 0.36f - i * 0.08f));
            float width = 0.16f + i * 0.08f;
            RuntimeUiFactory.SetRect(ripple.rectTransform, new Vector2(0.5f - width, 0.405f - i * 0.025f), new Vector2(0.5f + width, 0.409f - i * 0.025f), Vector2.zero, Vector2.zero);
        }
        fade.rectTransform.SetAsLastSibling();
        FadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
        FadeGroup.alpha = 0f;
        FadeGroup.blocksRaycasts = false;
    }

    public void ShowIntro()
    {
        HideContentLayers();
        introPanel.SetActive(true);
        chapterText.text = string.Empty;
    }

    public void ShowPlaying(VideoSegmentData segment)
    {
        HideContentLayers();
        currentChapterZh = segment != null ? segment.chapterZh : string.Empty;
        currentChapterEn = segment != null ? segment.chapterEn : string.Empty;
        chapterText.text = segment != null ? segment.GetChapter(settings.CurrentLanguage) : string.Empty;
        HideCultureCue();
    }

    public void ShowQuestion(QuestionData question)
    {
        HideContentLayers();
        currentQuestion = question;
        quizPanel.SetActive(true);
        RefreshQuestion();
    }

    public void ShowFeedback(bool correct, string bodyZh, string bodyEn)
    {
        HideContentLayers();
        lastFeedbackCorrect = correct;
        feedbackBodyZh = bodyZh ?? string.Empty;
        feedbackBodyEn = bodyEn ?? string.Empty;
        feedbackPanel.SetActive(true);
        RefreshFeedback();
    }

    public void ShowComplete(string cityZh, string cityEn)
    {
        HideContentLayers();
        completePanel.SetActive(true);
        RefreshComplete();
    }

    public void ShowCultureCue(string titleZh, string titleEn, string bodyZh, string bodyEn)
    {
        cultureTitleZh = titleZh ?? string.Empty;
        cultureTitleEn = titleEn ?? string.Empty;
        cultureBodyZh = bodyZh ?? string.Empty;
        cultureBodyEn = bodyEn ?? string.Empty;
        cultureAction.interactable = true;
        RefreshCulture();
    }

    public void HideCultureCue()
    {
        cultureAction.interactable = false;
        cultureHeading.text = settings.CurrentLanguage == AppLanguage.English ? "Watch for a cultural clue" : "等待影片中的文化线索";
        cultureSummary.text = settings.CurrentLanguage == AppLanguage.English ? "Clues appear as the film timeline advances." : "线索会随影片时间轴出现。";
    }

    private void ShowCultureDetail()
    {
        if (!cultureAction.interactable) return;
        RefreshCulture();
        cultureDetailPanel.SetActive(true);
        cultureDetailPanel.transform.SetAsLastSibling();
    }

    public void ShowSubtitle(string chinese, string english, SubtitleMode mode, SubtitleSize size)
    {
        if (mode == SubtitleMode.Off)
        {
            HideSubtitle();
            return;
        }
        float zh = size == SubtitleSize.Small ? 25f : size == SubtitleSize.Large ? 35f : 30f;
        float en = size == SubtitleSize.Small ? 20f : size == SubtitleSize.Large ? 29f : 24f;
        if (mode == SubtitleMode.Chinese) subtitleText.text = "<size=" + zh + ">" + (chinese ?? string.Empty) + "</size>";
        else if (mode == SubtitleMode.English) subtitleText.text = "<size=" + en + ">" + (english ?? string.Empty) + "</size>";
        else subtitleText.text = "<size=" + zh + ">" + (chinese ?? string.Empty) + "</size>\n<size=" + en + ">" + (english ?? string.Empty) + "</size>";
        subtitlePanel.SetActive(!string.IsNullOrWhiteSpace(subtitleText.text));
    }

    public void HideSubtitle()
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    public void ShowExitConfirmation()
    {
        CloseTopModal();
        directoryConfirmPanel.SetActive(true);
    }

    public void HideExitConfirmation() => directoryConfirmPanel.SetActive(false);

    public void ToggleSettings()
    {
        directoryConfirmPanel.SetActive(false);
        quitConfirmPanel.SetActive(false);
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        settingsPanel.transform.SetAsLastSibling();
        RefreshSettings();
    }

    public void CloseTopModal()
    {
        if (settingsPanel.activeSelf) settingsPanel.SetActive(false);
        else if (cultureDetailPanel.activeSelf) cultureDetailPanel.SetActive(false);
        else if (directoryConfirmPanel.activeSelf) directoryConfirmPanel.SetActive(false);
        else if (quitConfirmPanel.activeSelf) quitConfirmPanel.SetActive(false);
    }

    public void SetControlsInteractable(bool interactable)
    {
        languageCnButton.interactable = interactable;
        languageEnButton.interactable = interactable;
        ccButton.interactable = interactable;
        soundButton.interactable = interactable;
    }

    public void SetVideoAspect(RawImage rawImage, float aspect)
    {
        if (rawImage == null || aspect <= 0f) return;
        AspectRatioFitter fitter = rawImage.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = rawImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = aspect;
    }

    public void RefreshLanguage()
    {
        bool english = settings.CurrentLanguage == AppLanguage.English;
        cityText.text = cityData.GetCityName(settings.CurrentLanguage);
        cityDescription.text = cityData.GetTitle(settings.CurrentLanguage);
        introCityText.text = cityData.GetCityName(settings.CurrentLanguage);
        introTitleText.text = cityData.GetTitle(settings.CurrentLanguage);
        introActionText.text = english ? "START EXPLORING" : "开始探索";
        chapterText.text = english ? currentChapterEn : currentChapterZh;
        languageCnText.text = settings.CurrentLanguage == AppLanguage.Chinese ? "● 中文" : "中文";
        languageEnText.text = settings.CurrentLanguage == AppLanguage.English ? "● EN" : "EN";
        ccText.text = "CC";
        RefreshSound();
        RefreshQuestion();
        RefreshFeedback();
        RefreshComplete();
        RefreshCulture();
        RefreshSettings();
    }

    private void RefreshQuestion()
    {
        if (currentQuestion == null || answerButtons == null) return;
        bool english = settings.CurrentLanguage == AppLanguage.English;
        questionHeading.text = english ? "CULTURAL QUIZ" : "文化小问答";
        questionBody.text = currentQuestion.GetQuestion(settings.CurrentLanguage);
        OptionData[] options = currentQuestion.options ?? Array.Empty<OptionData>();
        for (int i = 0; i < answerButtons.Length; i++)
        {
            bool active = i < options.Length && options[i] != null && !string.IsNullOrEmpty(options[i].optionId);
            answerButtons[i].gameObject.SetActive(active);
            if (active)
            {
                answerTexts[i].text = options[i].GetText(settings.CurrentLanguage);
                answerButtons[i].interactable = true;
            }
        }
    }

    private void RefreshFeedback()
    {
        if (feedbackHeading == null) return;
        bool english = settings.CurrentLanguage == AppLanguage.English;
        feedbackHeading.text = lastFeedbackCorrect ? (english ? "CORRECT" : "回答正确") : (english ? "TRY AGAIN" : "再想一想");
        feedbackBody.text = english ? feedbackBodyEn : feedbackBodyZh;
        feedbackActionText.text = lastFeedbackCorrect ? (english ? "CONTINUE" : "继续") : (english ? "TRY AGAIN" : "再试一次");
    }

    private void RefreshComplete()
    {
        if (completeHeading == null) return;
        bool english = settings.CurrentLanguage == AppLanguage.English;
        completeHeading.text = english ? "CITY EXPLORATION COMPLETE" : "城市探索完成";
        completeBody.text = english ? "This city's cultural route has been added to your journey record." : "本城文化路线已记录，返回目录可继续选择其他城市。";
        completeActionText.text = english ? "BACK TO DIRECTORY" : "返回城市目录";
    }

    private void RefreshCulture()
    {
        if (cultureHeading == null || !cultureAction.interactable) return;
        bool english = settings.CurrentLanguage == AppLanguage.English;
        cultureHeading.text = english && !string.IsNullOrWhiteSpace(cultureTitleEn) ? cultureTitleEn : cultureTitleZh;
        cultureSummary.text = english && !string.IsNullOrWhiteSpace(cultureBodyEn) ? cultureBodyEn : cultureBodyZh;
        cultureActionText.text = english ? "VIEW CULTURAL DETAIL" : "查看文化详情";
        detailHeading.text = cultureHeading.text;
        detailBody.text = cultureSummary.text;
    }

    private void RefreshSound()
    {
        if (soundText != null) soundText.text = settings.SoundEnabled ? "声音 ON" : "声音 OFF";
    }

    private void RefreshSettings()
    {
        if (subtitleModeTexts == null) return;
        for (int i = 0; i < subtitleModeTexts.Length; i++)
        {
            subtitleModeTexts[i].text = ((int)settings.CurrentSubtitleMode == i ? "● " : "○ ") + subtitleModeTexts[i].text.TrimStart('●', '○', ' ');
        }
        for (int i = 0; i < subtitleSizeTexts.Length; i++)
        {
            subtitleSizeTexts[i].text = ((int)settings.CurrentSubtitleSize == i ? "● " : "○ ") + subtitleSizeTexts[i].text.TrimStart('●', '○', ' ');
        }
    }

    private void RequestAnswer(int index)
    {
        if (currentQuestion == null || currentQuestion.options == null || index < 0 || index >= currentQuestion.options.Length) return;
        OptionData option = currentQuestion.options[index];
        if (option == null || string.IsNullOrEmpty(option.optionId)) return;
        for (int i = 0; i < answerButtons.Length; i++) answerButtons[i].interactable = false;
        AnswerRequested?.Invoke(option.optionId);
    }

    private void HideContentLayers()
    {
        introPanel.SetActive(false);
        quizPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        completePanel.SetActive(false);
        settingsPanel.SetActive(false);
        directoryConfirmPanel.SetActive(false);
        quitConfirmPanel.SetActive(false);
        cultureDetailPanel.SetActive(false);
        HideSubtitle();
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
        if (settings == null) return;
        settings.LanguageChanged -= RefreshLanguage;
        settings.SubtitleSettingsChanged -= RefreshSettings;
        settings.SoundChanged -= RefreshSound;
    }
}
