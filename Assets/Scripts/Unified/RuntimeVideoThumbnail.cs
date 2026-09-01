using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class RuntimeVideoThumbnail : MonoBehaviour
{
    private RawImage target;
    private VideoPlayer player;
    private RenderTexture preview;
    private bool captured;

    public void Initialize(RawImage image, string mediaFile)
    {
        target = image;
        if (target == null || string.IsNullOrWhiteSpace(mediaFile))
        {
            return;
        }

        preview = new RenderTexture(320, 480, 0, RenderTextureFormat.ARGB32);
        preview.name = "CityCardPreview";
        preview.Create();
        target.texture = preview;

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = false;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = preview;
        player.source = VideoSource.Url;
        player.url = MediaPathResolver.GetUrl(mediaFile);
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.sendFrameReadyEvents = true;
        player.prepareCompleted += OnPrepared;
        player.frameReady += OnFrameReady;
        player.errorReceived += OnError;
        player.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        source.time = source.length > 4d ? 2d : 0d;
        source.Play();
    }

    private void OnFrameReady(VideoPlayer source, long frameIndex)
    {
        if (captured || frameIndex <= 0)
        {
            return;
        }

        captured = true;
        source.Pause();
        source.prepareCompleted -= OnPrepared;
        source.frameReady -= OnFrameReady;
        source.errorReceived -= OnError;
        Destroy(source);
        player = null;
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogWarning("City card preview unavailable: " + message);
        source.prepareCompleted -= OnPrepared;
        source.frameReady -= OnFrameReady;
        source.errorReceived -= OnError;
        Destroy(source);
        player = null;
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.prepareCompleted -= OnPrepared;
            player.frameReady -= OnFrameReady;
            player.errorReceived -= OnError;
        }
        if (preview != null)
        {
            preview.Release();
            Destroy(preview);
        }
    }
}
