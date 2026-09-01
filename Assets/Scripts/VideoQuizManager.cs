using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public enum CityInteractionState
{
    Intro,
    PlayingVideo,
    ShowingQuestion,
    ShowingFeedback,
    Transitioning,
    Completed
}

public class VideoQuizManager : MonoBehaviour
{
    [Header("=== Legacy scene references ===")]
    public VideoPlayer videoPlayer;
    public GameObject questionPanel;
    public Text questionText;
    public Button[] answerButtons;

    [Header("=== Legacy feedback UI ===")]
    public GameObject correctPopup;
    public Text correctPopupText;
    public GameObject successPopup;
    public Text successPopupText;
    public GameObject errorPopup;
    public Text errorPopupText;

    [Header("=== Legacy completion UI ===")]
    public GameObject completePopup;
    public Text completePopupText;
    public Button completeButton;

    [Header("=== Existing city content (preserved) ===")]
    public List<QuizData> quizList = new List<QuizData>();

    [System.Serializable]
    public class QuizData
    {
        public VideoClip videoClip;
        [TextArea(2, 3)] public string question;
        public string[] answers = new string[4];
        public int correctIndex;
        [TextArea(2, 3)] public string successMessage = "太棒了！继续加油哦！";
    }

    public CityInteractionState State { get; private set; }

    private const float FadeOutDuration = 0.25f;
    private const float FadeInDuration = 0.32f;
    private const float PrepareTimeout = 15f;

    private int currentIndex;
    private bool lastAnswerCorrect;
    private CityInteractionData cityData;
    private UnifiedCityView view;
    private SubtitleManager subtitleManager;
    private TransitionController transitionController;
    private RawImage videoImage;

    private void Awake()
    {
        HideLegacyLayers();
        NormalizeCanvas();

        LanguageManager.EnsureExists();
        string sceneName = SceneManager.GetActiveScene().name;
        cityData = CityDataRepository.Load(sceneName);
        if (cityData == null || cityData.segments == null || cityData.segments.Length < quizList.Count)
        {
            cityData = BuildFallbackData(sceneName);
        }

        MergeLegacyChineseContent();

        if (videoPlayer == null)
        {
            Debug.LogError("VideoQuizManager requires a VideoPlayer reference.");
            enabled = false;
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;

        AudioController audioController = videoPlayer.GetComponent<AudioController>();
        if (audioController == null)
        {
            audioController = videoPlayer.gameObject.AddComponent<AudioController>();
        }
        audioController.Initialize(videoPlayer, false);

        videoImage = videoPlayer.GetComponent<RawImage>();
        view = gameObject.AddComponent<UnifiedCityView>();
        view.Initialize(transform as RectTransform, cityData);
        view.StartRequested += OnStartRequested;
        view.AnswerRequested += OnAnswerRequested;
        view.FeedbackActionRequested += OnFeedbackActionRequested;
        view.CompleteRequested += OnCompleteRequested;
        view.ExitRequested += OnExitRequested;
        view.ExitConfirmed += OnExitConfirmed;

        subtitleManager = gameObject.AddComponent<SubtitleManager>();
        subtitleManager.Initialize(videoPlayer, view);

        transitionController = gameObject.AddComponent<TransitionController>();
        transitionController.Initialize(view.FadeGroup);

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        currentIndex = 0;
        State = CityInteractionState.Intro;
        PrepareInitialFrame();
    }

    private void PrepareInitialFrame()
    {
        QuizData legacy = GetLegacyQuiz(currentIndex);
        VideoSegmentData segment = GetSegment(currentIndex);
        if (legacy == null || !ConfigureMedia(segment))
        {
            Debug.LogError("The first city video is missing.");
            return;
        }

        subtitleManager.SetSegment(segment);
        subtitleManager.SetPlaybackVisible(false);
        videoPlayer.Prepare();
    }

    private void OnStartRequested()
    {
        if (State != CityInteractionState.Intro || transitionController.IsBusy)
        {
            return;
        }

        StartCoroutine(BeginSegment(currentIndex));
    }

    private IEnumerator BeginSegment(int index)
    {
        QuizData legacy = GetLegacyQuiz(index);
        VideoSegmentData segment = GetSegment(index);
        if (legacy == null || segment == null || string.IsNullOrWhiteSpace(segment.mediaFile))
        {
            Debug.LogError("Missing media file at segment " + index + ".");
            ShowComplete();
            yield break;
        }

        State = CityInteractionState.Transitioning;
        view.SetControlsInteractable(false);
        subtitleManager.SetPlaybackVisible(false);
        yield return transitionController.FadeTo(1f, FadeOutDuration);

        videoPlayer.Stop();
        if (!ConfigureMedia(segment))
        {
            Debug.LogError("Unable to configure media at segment " + index + ".");
            ShowComplete();
            yield break;
        }
        videoPlayer.playbackSpeed = 1f;
        subtitleManager.SetSegment(segment);
        videoPlayer.Prepare();

        float elapsed = 0f;
        while (!videoPlayer.isPrepared && elapsed < PrepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("Video preparation timed out: " + segment.mediaFile);
            view.SetControlsInteractable(true);
            yield return transitionController.FadeTo(0f, FadeInDuration);
            ShowComplete();
            yield break;
        }

        ApplyPreparedAspect();
        view.ShowPlaying(segment);
        yield return transitionController.FadeTo(0f, FadeInDuration);

        videoPlayer.Play();
        subtitleManager.SetPlaybackVisible(true);
        view.SetControlsInteractable(true);
        State = CityInteractionState.PlayingVideo;
        Debug.Log("Playing city segment: " + segment.mediaFile);
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        ApplyPreparedAspect();

        if (State == CityInteractionState.Intro)
        {
            try
            {
                player.frame = 0;
                player.StepForward();
                player.Pause();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("First-frame preview is not available on this platform: " + exception.Message);
            }
        }
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        if (State != CityInteractionState.PlayingVideo)
        {
            return;
        }

        subtitleManager.SetPlaybackVisible(false);
        bool isLast = currentIndex >= quizList.Count - 1;
        VideoSegmentData segment = GetSegment(currentIndex);

        if (isLast)
        {
            ShowComplete();
            return;
        }

        if (segment == null || segment.question == null || string.IsNullOrEmpty(segment.question.questionId))
        {
            currentIndex++;
            StartCoroutine(BeginSegment(currentIndex));
            return;
        }

        State = CityInteractionState.ShowingQuestion;
        view.ShowQuestion(segment.question);
    }

    private void OnAnswerRequested(string selectedOptionId)
    {
        if (State != CityInteractionState.ShowingQuestion)
        {
            return;
        }

        VideoSegmentData segment = GetSegment(currentIndex);
        if (segment == null || segment.question == null)
        {
            return;
        }

        lastAnswerCorrect = string.Equals(selectedOptionId, segment.question.correctOptionId, System.StringComparison.Ordinal);
        State = CityInteractionState.ShowingFeedback;

        if (lastAnswerCorrect)
        {
            view.ShowFeedback(true, segment.question.successZh, segment.question.successEn);
        }
        else
        {
            view.ShowFeedback(false, "答案不对，再看看题目中的文化线索吧。", "That is not the answer. Look at the cultural clue and try again.");
        }
    }

    private void OnFeedbackActionRequested()
    {
        if (State != CityInteractionState.ShowingFeedback || transitionController.IsBusy)
        {
            return;
        }

        if (!lastAnswerCorrect)
        {
            State = CityInteractionState.ShowingQuestion;
            VideoSegmentData currentSegment = GetSegment(currentIndex);
            if (currentSegment != null && currentSegment.question != null)
            {
                view.ShowQuestion(currentSegment.question);
            }
            return;
        }

        currentIndex++;
        if (currentIndex < quizList.Count)
        {
            StartCoroutine(BeginSegment(currentIndex));
        }
        else
        {
            ShowComplete();
        }
    }

    private void ShowComplete()
    {
        videoPlayer.Pause();
        subtitleManager.SetPlaybackVisible(false);
        State = CityInteractionState.Completed;

        string nextSceneName = GetNextSceneName();
        if (string.IsNullOrEmpty(nextSceneName))
        {
            view.ShowComplete(string.Empty, string.Empty);
            return;
        }

        CityInteractionData nextData = CityDataRepository.Load(nextSceneName);
        if (nextData != null)
        {
            view.ShowComplete(nextData.cityNameZh, nextData.cityNameEn);
        }
        else
        {
            view.ShowComplete(nextSceneName, nextSceneName);
        }
    }

    private void OnCompleteRequested()
    {
        if (State != CityInteractionState.Completed || transitionController.IsBusy)
        {
            return;
        }

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnExitRequested()
    {
        if (!transitionController.IsBusy)
        {
            view.ShowExitConfirmation();
        }
    }

    private void OnExitConfirmed()
    {
        if (!transitionController.IsBusy)
        {
            SceneManager.LoadScene(0);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || view == null)
        {
            return;
        }

        if (view.SettingsVisible || view.ExitConfirmationVisible)
        {
            view.CloseTopModal();
        }
        else
        {
            OnExitRequested();
        }
    }

    private void NormalizeCanvas()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void HideLegacyLayers()
    {
        if (questionPanel != null) questionPanel.SetActive(false);
        if (correctPopup != null) correctPopup.SetActive(false);
        if (successPopup != null) successPopup.SetActive(false);
        if (errorPopup != null) errorPopup.SetActive(false);
        if (completePopup != null) completePopup.SetActive(false);
    }

    private void MergeLegacyChineseContent()
    {
        for (int i = 0; i < quizList.Count && i < cityData.segments.Length; i++)
        {
            QuizData legacy = quizList[i];
            VideoSegmentData segment = cityData.segments[i];
            if (segment == null || legacy == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(legacy.question))
            {
                segment.question = null;
                continue;
            }

            if (segment.question == null)
            {
                segment.question = new QuestionData();
            }

            QuestionData question = segment.question;
            if (string.IsNullOrEmpty(question.questionId))
            {
                question.questionId = cityData.cityId + "_q" + (i + 1).ToString("00");
            }

            question.questionZh = legacy.question;
            question.successZh = legacy.successMessage;
            int answerCount = legacy.answers != null ? legacy.answers.Length : 0;

            if (question.options == null || question.options.Length != answerCount)
            {
                OptionData[] rebuilt = new OptionData[answerCount];
                for (int optionIndex = 0; optionIndex < answerCount; optionIndex++)
                {
                    rebuilt[optionIndex] = new OptionData
                    {
                        optionId = "option_" + (char)('a' + optionIndex),
                        textEn = legacy.answers[optionIndex]
                    };
                }
                question.options = rebuilt;
            }

            for (int optionIndex = 0; optionIndex < answerCount; optionIndex++)
            {
                if (question.options[optionIndex] == null)
                {
                    question.options[optionIndex] = new OptionData();
                }
                if (string.IsNullOrEmpty(question.options[optionIndex].optionId))
                {
                    question.options[optionIndex].optionId = "option_" + (char)('a' + optionIndex);
                }
                question.options[optionIndex].textZh = legacy.answers[optionIndex];
            }

            if (legacy.correctIndex >= 0 && legacy.correctIndex < question.options.Length)
            {
                question.correctOptionId = question.options[legacy.correctIndex].optionId;
            }
        }
    }

    private CityInteractionData BuildFallbackData(string sceneName)
    {
        CityInteractionData fallback = new CityInteractionData
        {
            cityId = "city_" + sceneName,
            sceneName = sceneName,
            cityNameZh = GetFallbackCityName(sceneName, false),
            cityNameEn = GetFallbackCityName(sceneName, true),
            titleZh = "江南城市文化互动体验",
            titleEn = "Jiangnan City Cultural Experience",
            segments = new VideoSegmentData[quizList.Count]
        };

        for (int i = 0; i < quizList.Count; i++)
        {
            fallback.segments[i] = new VideoSegmentData
            {
                segmentId = fallback.cityId + "_segment_" + (i + 1).ToString("00"),
                chapterZh = "文化章节 " + (i + 1),
                chapterEn = "Chapter " + (i + 1),
                subtitles = new SubtitleCue[0]
            };

            QuizData legacy = quizList[i];
            if (legacy != null && !string.IsNullOrEmpty(legacy.question))
            {
                int count = legacy.answers != null ? legacy.answers.Length : 0;
                OptionData[] options = new OptionData[count];
                for (int optionIndex = 0; optionIndex < count; optionIndex++)
                {
                    options[optionIndex] = new OptionData
                    {
                        optionId = "option_" + (char)('a' + optionIndex),
                        textZh = legacy.answers[optionIndex],
                        textEn = legacy.answers[optionIndex]
                    };
                }

                fallback.segments[i].question = new QuestionData
                {
                    questionId = fallback.cityId + "_q" + (i + 1).ToString("00"),
                    questionZh = legacy.question,
                    questionEn = legacy.question,
                    options = options,
                    correctOptionId = count > 0 ? options[Mathf.Clamp(legacy.correctIndex, 0, count - 1)].optionId : string.Empty,
                    successZh = legacy.successMessage,
                    successEn = legacy.successMessage
                };
            }
        }

        return fallback;
    }

    private static string GetFallbackCityName(string sceneName, bool english)
    {
        switch (sceneName)
        {
            case "1": return english ? "Yangzhou" : "扬州";
            case "2": return english ? "Huai'an" : "淮安";
            case "3": return english ? "Wuxi" : "无锡";
            case "4": return english ? "Suzhou" : "苏州";
            case "5": return english ? "Nanjing" : "南京";
            default: return sceneName;
        }
    }

    private void ApplyPreparedAspect()
    {
        if (view != null && videoImage != null && videoPlayer.height > 0)
        {
            view.SetVideoAspect(videoImage, (float)videoPlayer.width / videoPlayer.height);
        }
    }

    private bool ConfigureMedia(VideoSegmentData segment)
    {
        if (segment == null || string.IsNullOrWhiteSpace(segment.mediaFile))
        {
            return false;
        }

        string url = MediaPathResolver.GetUrl(segment.mediaFile);
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        if (view != null && videoImage != null)
        {
            view.SetVideoAspect(videoImage, 9f / 16f);
        }
        return true;
    }

    private QuizData GetLegacyQuiz(int index)
    {
        return index >= 0 && index < quizList.Count ? quizList[index] : null;
    }

    private VideoSegmentData GetSegment(int index)
    {
        return cityData != null && cityData.segments != null && index >= 0 && index < cityData.segments.Length
            ? cityData.segments[index]
            : null;
    }

    private string GetNextSceneName()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return string.Empty;
        }

        string path = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileNameWithoutExtension(path);
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError("VideoPlayer error in " + cityData.cityId + ": " + message);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }

        if (view != null)
        {
            view.StartRequested -= OnStartRequested;
            view.AnswerRequested -= OnAnswerRequested;
            view.FeedbackActionRequested -= OnFeedbackActionRequested;
            view.CompleteRequested -= OnCompleteRequested;
            view.ExitRequested -= OnExitRequested;
            view.ExitConfirmed -= OnExitConfirmed;
        }
    }
}
