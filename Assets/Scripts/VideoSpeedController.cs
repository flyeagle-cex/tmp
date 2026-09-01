using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class VideoSpeedController : MonoBehaviour
{
    private static readonly float[] Speeds = { 0.75f, 1f, 1.25f, 1.5f };
    private static int selectedSpeedIndex = 1;

    public VideoPlayer videoPlayer;
    public Button speedButton;
    public Text speedLabel;

    public float CurrentSpeed => Speeds[selectedSpeedIndex];

    private void Start()
    {
        if (speedButton != null)
        {
            speedButton.onClick.AddListener(SelectNextSpeed);
        }
        if (videoPlayer != null)
        {
            videoPlayer.started += OnVideoStarted;
        }

        LanguageManager language = LanguageManager.EnsureExists();
        language.LanguageChanged -= RefreshLabel;
        language.LanguageChanged += RefreshLabel;
        ApplySpeed();
    }

    public void SelectNextSpeed()
    {
        selectedSpeedIndex = (selectedSpeedIndex + 1) % Speeds.Length;
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        if (videoPlayer != null && videoPlayer.canSetPlaybackSpeed)
        {
            videoPlayer.playbackSpeed = CurrentSpeed;
        }
        RefreshLabel();
    }

    private void OnVideoStarted(VideoPlayer source)
    {
        if (source.canSetPlaybackSpeed)
        {
            source.playbackSpeed = CurrentSpeed;
        }
    }

    private void RefreshLabel()
    {
        if (speedLabel == null) return;
        string value = CurrentSpeed.ToString("0.##") + "×";
        speedLabel.text = LanguageManager.EnsureExists().IsEnglish
            ? "SPEED " + value
            : "语速 " + value;
    }

    private void OnDestroy()
    {
        if (speedButton != null)
        {
            speedButton.onClick.RemoveListener(SelectNextSpeed);
        }
        if (videoPlayer != null)
        {
            videoPlayer.started -= OnVideoStarted;
        }
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged -= RefreshLabel;
        }
    }
}
