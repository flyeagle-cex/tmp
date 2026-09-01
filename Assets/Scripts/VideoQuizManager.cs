using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoQuizManager : MonoBehaviour
{
    [Header("=== UI组件 ===")]
    public VideoPlayer videoPlayer;
    public GameObject questionPanel;        // 答题面板
    public Text questionText;               // 题目文本
    public Button[] answerButtons;          // 4个选项按钮

    [Header("=== 反馈弹窗（独立UI）===")]
    public GameObject correctPopup;         // ✅ 正确弹窗
    public Text correctPopupText;
    public GameObject successPopup;         // 🎉 鼓励语弹窗
    public Text successPopupText;
    public GameObject errorPopup;           // ❌ 错误弹窗
    public Text errorPopupText;

    [Header("=== 全部完成弹窗 ===")]
    public GameObject completePopup;        // 🏁 全部完成弹窗
    public Text completePopupText;          // 可编辑的文本
    public Button completeButton;           // 跳转按钮

    [Header("=== 题目数据（手动录入）===")]
    public List<QuizData> quizList = new List<QuizData>();

    [System.Serializable]
    public class QuizData
    {
        [Header("视频 (直接拖入)")]
        public VideoClip videoClip;

        [Header("题目 (最后一个视频可留空)")]
        [TextArea(2, 3)] public string question;
        public string[] answers = new string[4];
        public int correctIndex;

        [Header("English UI text")]
        [TextArea(2, 3)] public string questionEnglish;
        public string[] answersEnglish = new string[4];

        [Header("答对鼓励语")]
        [TextArea(2, 3)] public string successMessage = "太棒了！继续加油哦！";
        [TextArea(2, 3)] public string successMessageEnglish;
    }

    // ===== 内部状态 =====
    private int currentIndex = 0;
    private bool isAnswering = false;
    private bool isWaitingForRetry = false;
    private string originalCompletePopupText;
    private string originalCompleteButtonText;

    void Start()
    {
        originalCompletePopupText = completePopupText != null ? completePopupText.text : string.Empty;
        Text completeLabel = completeButton != null ? completeButton.GetComponentInChildren<Text>(true) : null;
        originalCompleteButtonText = completeLabel != null ? completeLabel.text : string.Empty;

        // 初始化UI：隐藏所有弹窗
        questionPanel.SetActive(false);
        HideAllPopups();
        if (completePopup != null) completePopup.SetActive(false);

        // 绑定视频结束事件
        videoPlayer.loopPointReached += OnVideoFinished;

        // 绑定按钮事件
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }

        // 绑定完成按钮事件
        if (completeButton != null)
        {
            completeButton.onClick.AddListener(OnCompleteButtonClicked);
        }

        LanguageManager language = LanguageManager.EnsureExists();
        language.LanguageChanged -= RefreshLanguage;
        language.LanguageChanged += RefreshLanguage;

        // 如果列表为空，添加示例数据
        if (quizList.Count == 0) AddExampleData();

        // 开始播放第一个视频
        PlayVideoByIndex(0);
        RefreshLanguage();
    }

    void HideAllPopups()
    {
        if (correctPopup != null) correctPopup.SetActive(false);
        if (successPopup != null) successPopup.SetActive(false);
        if (errorPopup != null) errorPopup.SetActive(false);
    }

    void AddExampleData()
    {
        // 示例1：正常答题
        QuizData q1 = new QuizData
        {
            question = "大运河最早开凿于哪个朝代？",
            answers = new string[] { "隋朝", "唐朝", "元朝", "明朝" },
            correctIndex = 0,
            successMessage = "太棒了！大运河可是古人用双手创造的奇迹哦！"
        };

        // 示例2：正常答题
        QuizData q2 = new QuizData
        {
            question = "大运河是世界上最长的人工运河，对吗？",
            answers = new string[] { "✅ 正确", "❌ 错误" },
            correctIndex = 0,
            successMessage = "没错！大运河确实是世界之最！"
        };

        // 示例3：最后一个视频（不答题）
        QuizData q3 = new QuizData
        {
            // 视频拖入即可，题目留空
            question = "",
            answers = new string[] { "" },
            correctIndex = 0,
            successMessage = ""
        };

        quizList.Add(q1);
        quizList.Add(q2);
        quizList.Add(q3);
    }

    // ===== 播放指定索引的视频 =====
    void PlayVideoByIndex(int index)
    {
        if (index >= quizList.Count)
        {
            // 所有视频播放完毕（安全兜底）
            ShowCompletePopup();
            return;
        }

        videoPlayer.clip = quizList[index].videoClip;
        videoPlayer.Play();
        Debug.Log("▶️ 播放视频: " + quizList[index].videoClip.name);
    }

    // ===== 视频播放结束回调 =====
    void OnVideoFinished(VideoPlayer vp)
    {
        // ✅ 判断是否是最后一个视频
        bool isLastVideo = (currentIndex == quizList.Count - 1);

        if (isLastVideo)
        {
            // 🏁 最后一个视频：直接弹完成窗，不答题
            Debug.Log("🏁 最后一个视频播放完毕，直接弹窗！");
            ShowCompletePopup();
        }
        else
        {
            // 其他视频：正常答题
            Debug.Log("⏹️ 视频结束，显示题目");
            ShowQuestion();
        }
    }

    // ===== 显示题目（动态控制选项显示） =====
    void ShowQuestion()
    {
        if (currentIndex >= quizList.Count) return;

        QuizData data = quizList[currentIndex];
        ApplyQuestionText(data);

        // 根据实际选项数量，动态显示/隐藏按钮
        int answerCount = data.answers.Length;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < answerCount && !string.IsNullOrEmpty(data.answers[i]))
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<Text>().text = GetAnswer(data, i);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        // 重置UI状态
        HideAllPopups();
        questionPanel.SetActive(true);
        isAnswering = true;
        isWaitingForRetry = false;
    }

    // ===== 点击选项 =====
    void OnAnswerSelected(int selectedIndex)
    {
        if (!isAnswering || isWaitingForRetry) return;

        QuizData data = quizList[currentIndex];
        bool isCorrect = (selectedIndex == data.correctIndex);

        if (isCorrect)
        {
            // ✅ 答对流程
            isAnswering = false;
            questionPanel.SetActive(false);

            // 步骤1：显示"正确"弹窗
            ShowCorrectPopup();

            // 步骤2：1秒后显示鼓励语弹窗
            StartCoroutine(ShowSuccessPopupAfterDelay(1f));
        }
        else
        {
            // ❌ 答错：显示错误弹窗，2秒后重试
            ShowErrorPopup();
            isAnswering = false;
            isWaitingForRetry = true;
            StartCoroutine(DelayRetry(2f));
        }
    }

    // ===== 显示正确弹窗 =====
    void ShowCorrectPopup()
    {
        if (correctPopup != null)
        {
            correctPopup.SetActive(true);
            if (correctPopupText != null)
                correctPopupText.text = LanguageManager.EnsureExists().IsEnglish ? "✅ Correct!" : "✅ 回答正确！";
        }
    }

    // ===== 1秒后显示鼓励语弹窗 =====
    IEnumerator ShowSuccessPopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 隐藏正确弹窗
        if (correctPopup != null) correctPopup.SetActive(false);

        // 显示鼓励语弹窗
        if (successPopup != null)
        {
            QuizData data = quizList[currentIndex];
            successPopup.SetActive(true);
            if (successPopupText != null)
                successPopupText.text = GetSuccessMessage(data);
        }

        // 1.5秒后进入下一题
        StartCoroutine(DelayNextQuestion(1.5f));
    }

    // ===== 显示错误弹窗 =====
    void ShowErrorPopup()
    {
        if (errorPopup != null)
        {
            errorPopup.SetActive(true);
            if (errorPopupText != null)
                errorPopupText.text = LanguageManager.EnsureExists().IsEnglish ? "❌ Please try again." : "❌ 再想想哦～";
        }
    }

    // ===== 协程：答对后进入下一题 =====
    IEnumerator DelayNextQuestion(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 隐藏鼓励语弹窗
        if (successPopup != null) successPopup.SetActive(false);

        // 移动到下一题
        currentIndex++;

        // 播放下一个视频
        PlayVideoByIndex(currentIndex);
    }

    // ===== 协程：答错后2秒重试 =====
    IEnumerator DelayRetry(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 隐藏错误弹窗
        if (errorPopup != null) errorPopup.SetActive(false);

        // 重新显示当前题目
        isWaitingForRetry = false;
        isAnswering = true;
        questionPanel.SetActive(true);

        // 刷新题目内容
        QuizData data = quizList[currentIndex];
        ApplyQuestionText(data);

        int answerCount = data.answers.Length;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < answerCount && !string.IsNullOrEmpty(data.answers[i]))
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<Text>().text = GetAnswer(data, i);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        Debug.Log("🔄 重新答题");
    }

    // ===== 🏁 显示全部完成弹窗 =====
    void ShowCompletePopup()
    {
        // 隐藏所有其他UI
        questionPanel.SetActive(false);
        HideAllPopups();

        if (completePopup != null)
        {
            completePopup.SetActive(true);

            // 如果设置了自定义文本，使用它；否则使用默认文本
            if (completePopupText != null && string.IsNullOrEmpty(completePopupText.text))
            {
                completePopupText.text = LanguageManager.EnsureExists().IsEnglish ? "🎉 Continue to the next city" : "🎉 前往下一站";
            }
        }
        RefreshCompleteButtonText();
        Debug.Log("🏁 所有视频已完成！");
    }

    // ===== 点击完成按钮：跳转到下一个场景 =====
    void OnCompleteButtonClicked()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        // 检查下一个场景是否存在
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("🚀 跳转到场景: " + nextSceneIndex);
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("⚠️ 没有下一个场景！当前场景索引: " + currentSceneIndex);
            // 可以在这里做其他处理，比如返回主菜单
            // SceneManager.LoadScene(0);
        }
    }

    private void ApplyQuestionText(QuizData data)
    {
        if (questionText != null)
        {
            questionText.text = LanguageManager.EnsureExists().IsEnglish && !string.IsNullOrEmpty(data.questionEnglish)
                ? data.questionEnglish
                : data.question;
        }
    }

    private string GetAnswer(QuizData data, int index)
    {
        if (LanguageManager.EnsureExists().IsEnglish && data.answersEnglish != null &&
            index < data.answersEnglish.Length && !string.IsNullOrEmpty(data.answersEnglish[index]))
        {
            return data.answersEnglish[index];
        }
        return data.answers[index];
    }

    private string GetSuccessMessage(QuizData data)
    {
        return LanguageManager.EnsureExists().IsEnglish && !string.IsNullOrEmpty(data.successMessageEnglish)
            ? data.successMessageEnglish
            : data.successMessage;
    }

    private void RefreshLanguage()
    {
        if (currentIndex < quizList.Count)
        {
            QuizData data = quizList[currentIndex];
            if (questionPanel != null && questionPanel.activeSelf)
            {
                ApplyQuestionText(data);
                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (answerButtons[i] != null && answerButtons[i].gameObject.activeSelf && i < data.answers.Length)
                    {
                        Text label = answerButtons[i].GetComponentInChildren<Text>();
                        if (label != null) label.text = GetAnswer(data, i);
                    }
                }
            }
            if (successPopup != null && successPopup.activeSelf && successPopupText != null)
            {
                successPopupText.text = GetSuccessMessage(data);
            }
        }

        if (correctPopup != null && correctPopup.activeSelf && correctPopupText != null)
        {
            correctPopupText.text = LanguageManager.EnsureExists().IsEnglish ? "✅ Correct!" : "✅ 回答正确！";
        }
        if (errorPopup != null && errorPopup.activeSelf && errorPopupText != null)
        {
            errorPopupText.text = LanguageManager.EnsureExists().IsEnglish ? "❌ Please try again." : "❌ 再想想哦～";
        }
        if (completePopup != null && completePopup.activeSelf && completePopupText != null)
        {
            completePopupText.text = LanguageManager.EnsureExists().IsEnglish
                ? (SceneManager.GetActiveScene().buildIndex == SceneManager.sceneCountInBuildSettings - 1
                    ? "🎉 The five-city journey is complete"
                    : "🎉 Continue to the next city")
                : (!string.IsNullOrEmpty(originalCompletePopupText) ? originalCompletePopupText : "🎉 前往下一站");
            RefreshCompleteButtonText();
        }
    }

    private void RefreshCompleteButtonText()
    {
        Text label = completeButton != null ? completeButton.GetComponentInChildren<Text>(true) : null;
        if (label == null) return;
        label.text = LanguageManager.EnsureExists().IsEnglish
            ? (SceneManager.GetActiveScene().buildIndex == SceneManager.sceneCountInBuildSettings - 1 ? "FINISH" : "NEXT CITY")
            : originalCompleteButtonText;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged -= RefreshLanguage;
        }
    }

    // ===== 编辑器辅助 =====
    [ContextMenu("添加示例题目")]
    void AddExampleDataMenu()
    {
        AddExampleData();
    }
}
