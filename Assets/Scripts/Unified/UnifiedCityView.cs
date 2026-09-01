using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnifiedCityView : MonoBehaviour
{
    private static readonly Color32 Ink = new Color32(28, 34, 40, 255);
    private static readonly Color32 Paper = new Color32(247, 243, 232, 245);
    private static readonly Color32 Jade = new Color32(42, 112, 101, 255);
    private static readonly Color32 JadeHover = new Color32(55, 137, 123, 255);
    private static readonly Color32 Gold = new Color32(201, 161, 82, 255);
    private static readonly Color32 Dim = new Color32(10, 16, 20, 190);

    public event Action StartRequested;
    public event Action<string> AnswerRequested;
    public event Action FeedbackActionRequested;
    public event Action CompleteRequested;
    public event Action ExitRequested;
    public event Action ExitConfirmed;

    public CanvasGroup FadeGroup { get; private set; }
    public bool SettingsVisible => settingsPanel != null && settingsPanel.activeSelf;
    public bool ExitConfirmationVisible => exitPanel != null && exitPanel.activeSelf;

    private CityInteractionData cityData;
    private LanguageManager settings;
    private Font uiFont;

    private Text cityTitleText;
    private Text chapterText;
    private Text languageButtonText;
    private Text subtitleButtonText;
    private Text soundButtonText;
    private Text exitButtonText;
    private Text introCityText;
    private Text introTitleText;
    private Text startButtonText;
    private Text questionHeadingText;
    private Text questionText;
    private Text feedbackHeadingText;
    private Text feedbackBodyText;
    private Text feedbackButtonText;
    private Text completeHeadingText;
    private Text completeBodyText;
    private Text completeButtonText;
    private Text subtitleText;
    private Text settingsHeadingText;
    private Text modeHeadingText;
    private Text sizeHeadingText;
    private Text closeSettingsText;
    private Text exitHeadingText;
    private Text exitBodyText;
    private Text cancelExitText;
    private Text confirmExitText;

    private GameObject introPanel;
    private GameObject quizPanel;
    private GameObject feedbackPanel;
    private GameObject completePanel;
    private GameObject subtitlePanel;
    private GameObject settingsPanel;
    private GameObject exitPanel;

    private Button[] answerButtons;
    private Text[] answerTexts;
    private Button feedbackButton;
    private Button completeButton;
    private Button languageButton;
    private Button subtitleButton;
    private Button soundButton;
    private Button exitButton;
    private Button[] modeButtons;
    private Text[] modeButtonTexts;
    private Button[] sizeButtons;
    private Text[] sizeButtonTexts;

    private QuestionData currentQuestion;
    private string currentChapterZh;
    private string currentChapterEn;
    private string feedbackBodyZh;
    private string feedbackBodyEn;
    private string nextCityZh;
    private string nextCityEn;
    private bool lastFeedbackCorrect;

    public void Initialize(RectTransform canvasRoot, CityInteractionData data)
    {
        cityData = data;
        settings = LanguageManager.EnsureExists();
        uiFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        RectTransform root = CreateRect("CityInteractionRoot", canvasRoot);
        Stretch(root);
        root.SetAsLastSibling();

        BuildIntro(root);
        BuildSubtitle(root);
        BuildQuiz(root);
        BuildFeedback(root);
        BuildComplete(root);
        BuildTopBar(root);
        BuildSubtitleSettings(root);
        BuildExitConfirmation(root);
        BuildFade(root);

        settings.LanguageChanged += RefreshLanguage;
        settings.SubtitleSettingsChanged += RefreshSettingsLabels;
        settings.SoundChanged += RefreshSoundLabel;

        RefreshLanguage();
        ShowIntro();
    }

    private void BuildTopBar(RectTransform root)
    {
        RectTransform bar = CreatePanel("TopBar", root, new Color32(14, 25, 29, 225));
        SetAnchors(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        bar.sizeDelta = new Vector2(0f, 92f);
        bar.anchoredPosition = Vector2.zero;

        cityTitleText = CreateText("CityTitle", bar, 34, TextAnchor.MiddleLeft, Color.white);
        SetAnchors(cityTitleText.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 1f), new Vector2(0f, 0.5f));
        cityTitleText.rectTransform.offsetMin = new Vector2(40f, 0f);
        cityTitleText.rectTransform.offsetMax = Vector2.zero;
        cityTitleText.fontStyle = FontStyle.Bold;

        chapterText = CreateText("ChapterTitle", bar, 26, TextAnchor.MiddleCenter, new Color32(232, 218, 181, 255));
        SetAnchors(chapterText.rectTransform, new Vector2(0.34f, 0f), new Vector2(0.63f, 1f), new Vector2(0.5f, 0.5f));
        chapterText.rectTransform.offsetMin = Vector2.zero;
        chapterText.rectTransform.offsetMax = Vector2.zero;

        languageButton = CreateButton("LanguageButton", bar, new Vector2(152f, 54f), out languageButtonText);
        AnchorTopRight(languageButton.GetComponent<RectTransform>(), -430f, -19f);
        languageButton.onClick.AddListener(() => settings.ToggleLanguage());

        subtitleButton = CreateButton("SubtitleButton", bar, new Vector2(118f, 54f), out subtitleButtonText);
        AnchorTopRight(subtitleButton.GetComponent<RectTransform>(), -300f, -19f);
        subtitleButton.onClick.AddListener(ToggleSettings);

        soundButton = CreateButton("SoundButton", bar, new Vector2(136f, 54f), out soundButtonText);
        AnchorTopRight(soundButton.GetComponent<RectTransform>(), -152f, -19f);
        soundButton.onClick.AddListener(() => settings.ToggleSound());

        exitButton = CreateButton("ExitButton", bar, new Vector2(92f, 54f), out exitButtonText);
        AnchorTopRight(exitButton.GetComponent<RectTransform>(), -48f, -19f);
        exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
    }

    private void BuildIntro(RectTransform root)
    {
        RectTransform overlay = CreatePanel("IntroLayer", root, new Color32(8, 16, 20, 150));
        Stretch(overlay);
        introPanel = overlay.gameObject;

        RectTransform card = CreatePanel("IntroCard", overlay, Paper);
        SetCentered(card, new Vector2(880f, 470f), new Vector2(0f, -20f));

        RectTransform accent = CreatePanel("Accent", card, Gold);
        SetAnchors(accent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        accent.sizeDelta = new Vector2(0f, 8f);

        introCityText = CreateText("IntroCity", card, 72, TextAnchor.MiddleCenter, Ink);
        SetCentered(introCityText.rectTransform, new Vector2(760f, 120f), new Vector2(0f, 90f));
        introCityText.fontStyle = FontStyle.Bold;

        introTitleText = CreateText("IntroTitle", card, 30, TextAnchor.MiddleCenter, Jade);
        SetCentered(introTitleText.rectTransform, new Vector2(740f, 100f), new Vector2(0f, 5f));

        Button startButton = CreateButton("StartButton", card, new Vector2(330f, 86f), out startButtonText);
        SetCentered(startButton.GetComponent<RectTransform>(), new Vector2(330f, 86f), new Vector2(0f, -115f));
        startButtonText.fontSize = 32;
        startButton.onClick.AddListener(() => StartRequested?.Invoke());
    }

    private void BuildSubtitle(RectTransform root)
    {
        RectTransform panel = CreatePanel("SubtitleLayer", root, new Color32(7, 11, 14, 205));
        SetAnchors(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        panel.sizeDelta = new Vector2(1260f, 172f);
        panel.anchoredPosition = new Vector2(0f, 76f);
        subtitlePanel = panel.gameObject;

        subtitleText = CreateText("SubtitleText", panel, 40, TextAnchor.MiddleCenter, Color.white);
        Stretch(subtitleText.rectTransform, 38f, 18f);
        subtitleText.supportRichText = true;
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Truncate;
        Outline outline = subtitleText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        subtitlePanel.SetActive(false);
    }

    private void BuildQuiz(RectTransform root)
    {
        RectTransform panel = CreatePanel("QuizLayer", root, Paper);
        SetCentered(panel, new Vector2(1180f, 790f), new Vector2(0f, -5f));
        quizPanel = panel.gameObject;

        RectTransform accent = CreatePanel("Accent", panel, Gold);
        SetAnchors(accent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        accent.sizeDelta = new Vector2(0f, 8f);

        questionHeadingText = CreateText("QuizHeading", panel, 30, TextAnchor.MiddleCenter, Jade);
        SetCentered(questionHeadingText.rectTransform, new Vector2(1000f, 58f), new Vector2(0f, 332f));
        questionHeadingText.fontStyle = FontStyle.Bold;

        questionText = CreateText("QuestionText", panel, 39, TextAnchor.MiddleCenter, Ink);
        SetCentered(questionText.rectTransform, new Vector2(1020f, 180f), new Vector2(0f, 220f));
        questionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionText.verticalOverflow = VerticalWrapMode.Truncate;
        questionText.resizeTextForBestFit = true;
        questionText.resizeTextMinSize = 25;
        questionText.resizeTextMaxSize = 39;

        answerButtons = new Button[4];
        answerTexts = new Text[4];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int capturedIndex = i;
            answerButtons[i] = CreateButton("Option" + (i + 1), panel, new Vector2(980f, 88f), out answerTexts[i]);
            SetCentered(answerButtons[i].GetComponent<RectTransform>(), new Vector2(980f, 88f), new Vector2(0f, 82f - (i * 105f)));
            answerTexts[i].fontSize = 28;
            answerTexts[i].alignment = TextAnchor.MiddleLeft;
            answerTexts[i].rectTransform.offsetMin = new Vector2(34f, 6f);
            answerTexts[i].rectTransform.offsetMax = new Vector2(-24f, -6f);
            answerButtons[i].onClick.AddListener(() => RequestAnswer(capturedIndex));
        }

        quizPanel.SetActive(false);
    }

    private void BuildFeedback(RectTransform root)
    {
        RectTransform panel = CreatePanel("FeedbackLayer", root, Paper);
        SetCentered(panel, new Vector2(820f, 430f), Vector2.zero);
        feedbackPanel = panel.gameObject;

        feedbackHeadingText = CreateText("FeedbackHeading", panel, 44, TextAnchor.MiddleCenter, Jade);
        SetCentered(feedbackHeadingText.rectTransform, new Vector2(720f, 80f), new Vector2(0f, 125f));
        feedbackHeadingText.fontStyle = FontStyle.Bold;

        feedbackBodyText = CreateText("FeedbackBody", panel, 30, TextAnchor.MiddleCenter, Ink);
        SetCentered(feedbackBodyText.rectTransform, new Vector2(700f, 150f), new Vector2(0f, 25f));
        feedbackBodyText.resizeTextForBestFit = true;
        feedbackBodyText.resizeTextMinSize = 22;
        feedbackBodyText.resizeTextMaxSize = 30;

        feedbackButton = CreateButton("FeedbackAction", panel, new Vector2(290f, 74f), out feedbackButtonText);
        SetCentered(feedbackButton.GetComponent<RectTransform>(), new Vector2(290f, 74f), new Vector2(0f, -135f));
        feedbackButton.onClick.AddListener(() => FeedbackActionRequested?.Invoke());
        feedbackPanel.SetActive(false);
    }

    private void BuildComplete(RectTransform root)
    {
        RectTransform panel = CreatePanel("CompleteLayer", root, Paper);
        SetCentered(panel, new Vector2(900f, 450f), Vector2.zero);
        completePanel = panel.gameObject;

        completeHeadingText = CreateText("CompleteHeading", panel, 46, TextAnchor.MiddleCenter, Jade);
        SetCentered(completeHeadingText.rectTransform, new Vector2(760f, 82f), new Vector2(0f, 130f));
        completeHeadingText.fontStyle = FontStyle.Bold;

        completeBodyText = CreateText("CompleteBody", panel, 31, TextAnchor.MiddleCenter, Ink);
        SetCentered(completeBodyText.rectTransform, new Vector2(760f, 135f), new Vector2(0f, 25f));

        completeButton = CreateButton("CompleteAction", panel, new Vector2(370f, 78f), out completeButtonText);
        SetCentered(completeButton.GetComponent<RectTransform>(), new Vector2(370f, 78f), new Vector2(0f, -135f));
        completeButton.onClick.AddListener(() => CompleteRequested?.Invoke());
        completePanel.SetActive(false);
    }

    private void BuildSubtitleSettings(RectTransform root)
    {
        RectTransform panel = CreatePanel("SubtitleSettingsLayer", root, Paper);
        SetCentered(panel, new Vector2(720f, 620f), new Vector2(380f, 20f));
        settingsPanel = panel.gameObject;

        settingsHeadingText = CreateText("SettingsHeading", panel, 38, TextAnchor.MiddleCenter, Jade);
        SetCentered(settingsHeadingText.rectTransform, new Vector2(620f, 65f), new Vector2(0f, 250f));
        settingsHeadingText.fontStyle = FontStyle.Bold;

        modeHeadingText = CreateText("ModeHeading", panel, 25, TextAnchor.MiddleLeft, Ink);
        SetCentered(modeHeadingText.rectTransform, new Vector2(590f, 45f), new Vector2(0f, 185f));

        modeButtons = new Button[4];
        modeButtonTexts = new Text[4];
        for (int i = 0; i < modeButtons.Length; i++)
        {
            int capturedMode = i;
            modeButtons[i] = CreateButton("SubtitleMode" + i, panel, new Vector2(280f, 64f), out modeButtonTexts[i]);
            float x = (i % 2 == 0) ? -155f : 155f;
            float y = (i < 2) ? 118f : 42f;
            SetCentered(modeButtons[i].GetComponent<RectTransform>(), new Vector2(280f, 64f), new Vector2(x, y));
            modeButtons[i].onClick.AddListener(() => settings.SetSubtitleMode((SubtitleMode)capturedMode));
        }

        sizeHeadingText = CreateText("SizeHeading", panel, 25, TextAnchor.MiddleLeft, Ink);
        SetCentered(sizeHeadingText.rectTransform, new Vector2(590f, 45f), new Vector2(0f, -32f));

        sizeButtons = new Button[3];
        sizeButtonTexts = new Text[3];
        for (int i = 0; i < sizeButtons.Length; i++)
        {
            int capturedSize = i;
            sizeButtons[i] = CreateButton("SubtitleSize" + i, panel, new Vector2(185f, 62f), out sizeButtonTexts[i]);
            SetCentered(sizeButtons[i].GetComponent<RectTransform>(), new Vector2(185f, 62f), new Vector2(-205f + (i * 205f), -102f));
            sizeButtons[i].onClick.AddListener(() => settings.SetSubtitleSize((SubtitleSize)capturedSize));
        }

        Button closeButton = CreateButton("CloseSettings", panel, new Vector2(260f, 68f), out closeSettingsText);
        SetCentered(closeButton.GetComponent<RectTransform>(), new Vector2(260f, 68f), new Vector2(0f, -220f));
        closeButton.onClick.AddListener(() => settingsPanel.SetActive(false));
        settingsPanel.SetActive(false);
    }

    private void BuildExitConfirmation(RectTransform root)
    {
        RectTransform panel = CreatePanel("ExitConfirmationLayer", root, Paper);
        SetCentered(panel, new Vector2(760f, 400f), Vector2.zero);
        exitPanel = panel.gameObject;

        exitHeadingText = CreateText("ExitHeading", panel, 40, TextAnchor.MiddleCenter, Jade);
        SetCentered(exitHeadingText.rectTransform, new Vector2(650f, 65f), new Vector2(0f, 118f));
        exitHeadingText.fontStyle = FontStyle.Bold;

        exitBodyText = CreateText("ExitBody", panel, 30, TextAnchor.MiddleCenter, Ink);
        SetCentered(exitBodyText.rectTransform, new Vector2(650f, 100f), new Vector2(0f, 35f));

        Button cancel = CreateButton("CancelExit", panel, new Vector2(250f, 72f), out cancelExitText);
        SetCentered(cancel.GetComponent<RectTransform>(), new Vector2(250f, 72f), new Vector2(-145f, -110f));
        cancel.onClick.AddListener(HideExitConfirmation);

        Button confirm = CreateButton("ConfirmExit", panel, new Vector2(250f, 72f), out confirmExitText);
        SetCentered(confirm.GetComponent<RectTransform>(), new Vector2(250f, 72f), new Vector2(145f, -110f));
        confirm.onClick.AddListener(() => ExitConfirmed?.Invoke());
        exitPanel.SetActive(false);
    }

    private void BuildFade(RectTransform root)
    {
        RectTransform fade = CreatePanel("TransitionFade", root, Color.black);
        Stretch(fade);
        fade.SetAsLastSibling();
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
        RefreshChapter();
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
        nextCityZh = cityZh;
        nextCityEn = cityEn;
        completePanel.SetActive(true);
        RefreshComplete();
    }

    public void ShowSubtitle(string chinese, string english, SubtitleMode mode, SubtitleSize size)
    {
        if (mode == SubtitleMode.Off)
        {
            HideSubtitle();
            return;
        }

        int zhSize = size == SubtitleSize.Small ? 34 : size == SubtitleSize.Large ? 50 : 42;
        int enSize = size == SubtitleSize.Small ? 28 : size == SubtitleSize.Large ? 40 : 34;

        if (mode == SubtitleMode.Chinese)
        {
            subtitleText.text = "<size=" + zhSize + ">" + (chinese ?? string.Empty) + "</size>";
        }
        else if (mode == SubtitleMode.English)
        {
            subtitleText.text = "<size=" + enSize + ">" + (english ?? string.Empty) + "</size>";
        }
        else
        {
            subtitleText.text = "<size=" + zhSize + ">" + (chinese ?? string.Empty) + "</size>\n<size=" + enSize + ">" + (english ?? string.Empty) + "</size>";
        }

        subtitlePanel.SetActive(!string.IsNullOrEmpty(subtitleText.text));
    }

    public void HideSubtitle()
    {
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }

    public void ShowExitConfirmation()
    {
        if (settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
        }
        exitPanel.SetActive(true);
        RefreshExit();
    }

    public void HideExitConfirmation()
    {
        exitPanel.SetActive(false);
    }

    public void ToggleSettings()
    {
        exitPanel.SetActive(false);
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        RefreshSettingsLabels();
    }

    public void CloseTopModal()
    {
        if (settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
        }
        else if (exitPanel.activeSelf)
        {
            exitPanel.SetActive(false);
        }
    }

    public void SetControlsInteractable(bool interactable)
    {
        languageButton.interactable = interactable;
        subtitleButton.interactable = interactable;
        soundButton.interactable = interactable;
        exitButton.interactable = interactable;
    }

    public void SetVideoAspect(RawImage rawImage, float aspect)
    {
        if (rawImage == null || aspect <= 0f)
        {
            return;
        }

        AspectRatioFitter fitter = rawImage.GetComponent<AspectRatioFitter>();
        if (fitter == null)
        {
            fitter = rawImage.gameObject.AddComponent<AspectRatioFitter>();
        }

        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = aspect;
    }

    public void RefreshLanguage()
    {
        AppLanguage language = settings.CurrentLanguage;
        bool english = language == AppLanguage.English;

        cityTitleText.text = cityData.GetCityName(language);
        introCityText.text = cityData.GetCityName(language);
        introTitleText.text = cityData.GetTitle(language);
        startButtonText.text = english ? "Start Exploring" : "开始探索";
        languageButtonText.text = english ? "中文 | [EN]" : "[中文] | EN";
        subtitleButtonText.text = english ? "CC" : "字幕 CC";
        exitButtonText.text = english ? "Exit" : "退出";
        questionHeadingText.text = english ? "Cultural Quiz" : "文化小问答";

        RefreshChapter();
        RefreshQuestion();
        RefreshFeedback();
        RefreshComplete();
        RefreshSoundLabel();
        RefreshSettingsLabels();
        RefreshExit();
    }

    private void RefreshChapter()
    {
        chapterText.text = settings.CurrentLanguage == AppLanguage.English ? (currentChapterEn ?? string.Empty) : (currentChapterZh ?? string.Empty);
    }

    private void RefreshQuestion()
    {
        if (currentQuestion == null || answerButtons == null)
        {
            return;
        }

        questionText.text = currentQuestion.GetQuestion(settings.CurrentLanguage);
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
        if (feedbackHeadingText == null)
        {
            return;
        }

        bool english = settings.CurrentLanguage == AppLanguage.English;
        feedbackHeadingText.text = lastFeedbackCorrect ? (english ? "Correct!" : "回答正确！") : (english ? "Try Again" : "再想一想");
        feedbackBodyText.text = english ? (feedbackBodyEn ?? string.Empty) : (feedbackBodyZh ?? string.Empty);
        feedbackButtonText.text = lastFeedbackCorrect ? (english ? "Continue" : "继续") : (english ? "Try Again" : "再试一次");
    }

    private void RefreshComplete()
    {
        if (completeHeadingText == null)
        {
            return;
        }

        bool english = settings.CurrentLanguage == AppLanguage.English;
        completeHeadingText.text = english ? "City Exploration Complete" : "城市探索完成";
        if (!string.IsNullOrEmpty(nextCityZh) || !string.IsNullOrEmpty(nextCityEn))
        {
            completeBodyText.text = english ? "Ready for the next stop: " + nextCityEn : "准备前往下一站·" + nextCityZh;
            completeButtonText.text = english ? "Continue" : "继续";
        }
        else
        {
            completeBodyText.text = english ? "You have completed the Jiangnan cultural journey." : "你已完成江南城市文化之旅。";
            completeButtonText.text = english ? "Back to Home" : "返回首页";
        }
    }

    private void RefreshSoundLabel()
    {
        if (soundButtonText == null)
        {
            return;
        }

        bool english = settings.CurrentLanguage == AppLanguage.English;
        soundButtonText.text = settings.SoundEnabled ? (english ? "Sound On" : "声音 开") : (english ? "Sound Off" : "声音 关");
    }

    private void RefreshSettingsLabels()
    {
        if (settingsHeadingText == null)
        {
            return;
        }

        bool english = settings.CurrentLanguage == AppLanguage.English;
        settingsHeadingText.text = english ? "Subtitle Settings" : "字幕设置";
        modeHeadingText.text = english ? "Subtitle mode" : "字幕模式";
        sizeHeadingText.text = english ? "Subtitle size" : "字幕大小";
        closeSettingsText.text = english ? "Close" : "关闭";

        string[] modeZh = { "中文", "English", "中英双语", "关闭字幕" };
        string[] modeEn = { "Chinese", "English", "Bilingual", "Off" };
        for (int i = 0; i < modeButtons.Length; i++)
        {
            bool selected = (int)settings.CurrentSubtitleMode == i;
            modeButtonTexts[i].text = (selected ? "● " : "○ ") + (english ? modeEn[i] : modeZh[i]);
        }

        string[] sizeZh = { "小", "中", "大" };
        string[] sizeEn = { "Small", "Medium", "Large" };
        for (int i = 0; i < sizeButtons.Length; i++)
        {
            bool selected = (int)settings.CurrentSubtitleSize == i;
            sizeButtonTexts[i].text = (selected ? "● " : "○ ") + (english ? sizeEn[i] : sizeZh[i]);
        }
    }

    private void RefreshExit()
    {
        if (exitHeadingText == null)
        {
            return;
        }

        bool english = settings.CurrentLanguage == AppLanguage.English;
        exitHeadingText.text = english ? "Exit" : "退出";
        exitBodyText.text = english ? "Exit the current city experience?" : "确定退出当前城市探索吗？";
        cancelExitText.text = english ? "Cancel" : "取消";
        confirmExitText.text = english ? "Exit" : "退出";
    }

    private void RequestAnswer(int index)
    {
        if (currentQuestion == null || currentQuestion.options == null || index < 0 || index >= currentQuestion.options.Length)
        {
            return;
        }

        OptionData option = currentQuestion.options[index];
        if (option != null && !string.IsNullOrEmpty(option.optionId))
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].interactable = false;
            }
            AnswerRequested?.Invoke(option.optionId);
        }
    }

    private void HideContentLayers()
    {
        introPanel.SetActive(false);
        quizPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        completePanel.SetActive(false);
        settingsPanel.SetActive(false);
        exitPanel.SetActive(false);
        HideSubtitle();
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 1.05f;
        return text;
    }

    private Button CreateButton(string name, Transform parent, Vector2 size, out Text label)
    {
        RectTransform rect = CreatePanel(name, parent, Jade);
        rect.sizeDelta = size;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = JadeHover;
        colors.pressedColor = new Color32(31, 88, 80, 255);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color32(100, 110, 110, 150);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.15f;
        button.colors = colors;

        label = CreateText("Label", rect, 25, TextAnchor.MiddleCenter, Color.white);
        Stretch(label.rectTransform, 14f, 6f);
        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
    }

    private static void AnchorTopRight(RectTransform rect, float x, float y)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private void OnDestroy()
    {
        if (settings == null)
        {
            return;
        }

        settings.LanguageChanged -= RefreshLanguage;
        settings.SubtitleSettingsChanged -= RefreshSettingsLabels;
        settings.SoundChanged -= RefreshSoundLabel;
    }
}
