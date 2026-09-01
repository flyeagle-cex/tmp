using UnityEngine;
using UnityEngine.Video;

public sealed class AudioController : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private LanguageManager settings;

    public void Initialize(VideoPlayer player, bool playOnAwake)
    {
        videoPlayer = player;
        settings = LanguageManager.EnsureExists();

        audioSource = player.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = player.gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        videoPlayer.playOnAwake = playOnAwake;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);

        settings.SoundChanged -= ApplySoundSetting;
        settings.SoundChanged += ApplySoundSetting;
        ApplySoundSetting();
    }

    private void ApplySoundSetting()
    {
        if (audioSource != null && settings != null)
        {
            audioSource.mute = !settings.SoundEnabled;
        }
    }

    private void OnDestroy()
    {
        if (settings != null)
        {
            settings.SoundChanged -= ApplySoundSetting;
        }
    }
}
