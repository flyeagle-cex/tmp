using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class JiangnanBuild
{
    public static void BuildWindows()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(projectRoot, "Builds", "Windows", "JiangnanProject.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        Build(BuildTarget.StandaloneWindows64, output, "Windows");
    }

    public static void BuildWebGL()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(projectRoot, "Builds", "WebGL");
        Directory.CreateDirectory(output);
        // Keep the deliverable deployable on ordinary static hosting without
        // requiring custom Content-Encoding headers for Unity's .gz files.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Build(BuildTarget.WebGL, output, "WebGL");
    }

    private static void Build(BuildTarget target, string output, string label)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        ClearPreviouslyPublishedMedia(target, output);
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            CopyWebMedia(target, output, projectRoot);
        }
        string message = label + " build: " + summary.result + ", errors=" + summary.totalErrors +
                         ", warnings=" + summary.totalWarnings + ", bytes=" + summary.totalSize;
        Debug.Log("JIANGNAN_BUILD_RESULT " + message);

        File.WriteAllText(Path.Combine(projectRoot, "QA", "build-" + label.ToLowerInvariant() + ".txt"), message);
        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception(message);
        }
    }

    private static void CopyWebMedia(BuildTarget target, string output, string projectRoot)
    {
        string source = Path.Combine(projectRoot, "WebMedia");
        string destination = GetPublishedMediaDirectory(target, output);
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException("Web media source is missing: " + source);
        }

        Directory.CreateDirectory(destination);
        string[] mediaFiles = Directory.GetFiles(source, "*.mp4", SearchOption.TopDirectoryOnly);
        if (mediaFiles.Length != 30)
        {
            throw new InvalidDataException("Expected 30 web media files, found " + mediaFiles.Length + ".");
        }

        for (int i = 0; i < mediaFiles.Length; i++)
        {
            File.Copy(mediaFiles[i], Path.Combine(destination, Path.GetFileName(mediaFiles[i])), true);
        }
    }

    private static string GetPublishedMediaDirectory(BuildTarget target, string output)
    {
        if (target == BuildTarget.WebGL)
        {
            return Path.Combine(output, "StreamingAssets", "Media");
        }

        string playerDirectory = Path.GetDirectoryName(output);
        string dataDirectory = Path.GetFileNameWithoutExtension(output) + "_Data";
        return Path.Combine(playerDirectory, dataDirectory, "StreamingAssets", "Media");
    }

    private static void ClearPreviouslyPublishedMedia(BuildTarget target, string output)
    {
        string directory = GetPublishedMediaDirectory(target, output);
        if (!Directory.Exists(directory))
        {
            return;
        }

        string[] generatedFiles = Directory.GetFiles(directory, "*.mp4", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < generatedFiles.Length; i++)
        {
            File.Delete(generatedFiles[i]);
        }
    }
}
