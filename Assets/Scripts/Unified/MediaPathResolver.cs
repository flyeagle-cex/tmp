using System.IO;
using UnityEngine;

public static class MediaPathResolver
{
    private const string MediaFolder = "Media";

    public static string GetUrl(string mediaFile)
    {
        if (string.IsNullOrWhiteSpace(mediaFile))
        {
            return string.Empty;
        }

        string mediaRoot;
#if UNITY_EDITOR
        mediaRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "WebMedia"));
#else
        mediaRoot = Path.Combine(Application.streamingAssetsPath, MediaFolder);
#endif
        string path = Path.Combine(mediaRoot, mediaFile).Replace('\\', '/');
#if UNITY_WEBGL && !UNITY_EDITOR
        if (path.Contains("://"))
        {
            return path;
        }
        return path;
#else
        // Windows Media Foundation cannot reliably open video below a path that
        // contains CJK characters. Copy each untouched delivery MP4 to Unity's
        // ASCII temporary cache before handing the path to VideoPlayer.
        string cacheDirectory = Path.Combine(Path.GetTempPath(), "JiangnanProjectMedia");
        Directory.CreateDirectory(cacheDirectory);
        string cachedPath = Path.Combine(cacheDirectory, mediaFile);
        FileInfo sourceInfo = new FileInfo(path);
        FileInfo cachedInfo = new FileInfo(cachedPath);
        if (!cachedInfo.Exists || cachedInfo.Length != sourceInfo.Length)
        {
            File.Copy(path, cachedPath, true);
        }
        return cachedPath.Replace('\\', '/');
#endif
    }
}
