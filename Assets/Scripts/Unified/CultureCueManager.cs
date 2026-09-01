using UnityEngine;
using UnityEngine.Video;

public sealed class CultureCueManager : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private ExhibitionCityView view;
    private VideoSegmentData segment;
    private bool revealed;

    public void Initialize(VideoPlayer player, ExhibitionCityView cityView)
    {
        videoPlayer = player;
        view = cityView;
    }

    public void SetSegment(VideoSegmentData value)
    {
        segment = value;
        revealed = false;
        if (view != null) view.HideCultureCue();
    }

    private void Update()
    {
        if (revealed || segment == null || videoPlayer == null || view == null || !videoPlayer.isPlaying)
        {
            return;
        }

        double revealTime = 3d;
        if (segment.subtitles != null && segment.subtitles.Length > 0 && segment.subtitles[0] != null)
        {
            revealTime = System.Math.Max(2d, segment.subtitles[0].startTime);
        }
        if (videoPlayer.time < revealTime)
        {
            return;
        }

        SubtitleCue clue = segment.subtitles != null && segment.subtitles.Length > 0 ? segment.subtitles[0] : null;
        string bodyZh = clue != null ? clue.chinese : segment.chapterZh;
        string bodyEn = clue != null ? clue.english : segment.chapterEn;
        view.ShowCultureCue(segment.chapterZh, segment.chapterEn, bodyZh, bodyEn);
        revealed = true;
    }
}
