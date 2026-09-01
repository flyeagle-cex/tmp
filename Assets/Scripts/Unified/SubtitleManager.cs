using UnityEngine;
using UnityEngine.Video;

public sealed class SubtitleManager : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private ExhibitionCityView view;
    private VideoSegmentData segment;
    private LanguageManager settings;
    private bool playbackVisible;
    private int currentCueIndex = -1;

    public void Initialize(VideoPlayer player, ExhibitionCityView cityView)
    {
        videoPlayer = player;
        view = cityView;
        settings = LanguageManager.EnsureExists();
        settings.SubtitleSettingsChanged += RefreshImmediately;
    }

    public void SetSegment(VideoSegmentData videoSegment)
    {
        segment = videoSegment;
        currentCueIndex = -1;
        RefreshImmediately();
    }

    public void SetPlaybackVisible(bool visible)
    {
        playbackVisible = visible;
        RefreshImmediately();
    }

    private void Update()
    {
        RefreshImmediately();
    }

    private void RefreshImmediately()
    {
        if (!playbackVisible || videoPlayer == null || view == null || segment == null ||
            segment.subtitles == null || settings.CurrentSubtitleMode == SubtitleMode.Off)
        {
            currentCueIndex = -1;
            if (view != null)
            {
                view.HideSubtitle();
            }
            return;
        }

        double time = videoPlayer.time;
        int found = -1;
        for (int i = 0; i < segment.subtitles.Length; i++)
        {
            SubtitleCue cue = segment.subtitles[i];
            if (cue != null && time >= cue.startTime && time < cue.endTime)
            {
                found = i;
                break;
            }
        }

        if (found < 0)
        {
            currentCueIndex = -1;
            view.HideSubtitle();
            return;
        }

        currentCueIndex = found;
        SubtitleCue activeCue = segment.subtitles[currentCueIndex];
        view.ShowSubtitle(activeCue.chinese, activeCue.english, settings.CurrentSubtitleMode, settings.CurrentSubtitleSize);
    }

    private void OnDestroy()
    {
        if (settings != null)
        {
            settings.SubtitleSettingsChanged -= RefreshImmediately;
        }
    }
}
