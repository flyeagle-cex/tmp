using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class JiangnanProjectValidator
{
    [Serializable]
    private sealed class CityValidation
    {
        public string scene;
        public string cityId;
        public int segments;
        public int questions;
        public int subtitleCues;
        public int videosWithAudio;
        public bool passed;
    }

    [Serializable]
    private sealed class ValidationReport
    {
        public string unityVersion;
        public string generatedAt;
        public List<CityValidation> cities = new List<CityValidation>();
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public bool passed;
    }

    public static void Run()
    {
        ValidationReport report = new ValidationReport
        {
            unityVersion = Application.unityVersion,
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string[] expectedIds = { "yangzhou", "huaian", "wuxi", "suzhou", "nanjing" };
        int[] expectedSegments = { 6, 6, 5, 7, 5 };

        ValidateBuildSettings(report);
        ValidateFont(report);

        for (int cityIndex = 0; cityIndex < 5; cityIndex++)
        {
            string sceneName = (cityIndex + 1).ToString();
            string scenePath = "Assets/Scenes/" + sceneName + ".unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            VideoQuizManager manager = UnityEngine.Object.FindFirstObjectByType<VideoQuizManager>();
            CityInteractionData data = CityDataRepository.Load(sceneName);

            CityValidation city = new CityValidation
            {
                scene = scenePath,
                cityId = data != null ? data.cityId : string.Empty,
                passed = true
            };

            if (!scene.IsValid() || manager == null)
            {
                AddError(report, city, sceneName + ": missing scene or VideoQuizManager.");
                report.cities.Add(city);
                continue;
            }

            if (data == null || data.cityId != expectedIds[cityIndex])
            {
                AddError(report, city, sceneName + ": city data ID does not match the expected city.");
                report.cities.Add(city);
                continue;
            }

            city.segments = data.segments.Length;
            if (city.segments != expectedSegments[cityIndex] || city.segments != manager.quizList.Count)
            {
                AddError(report, city, sceneName + ": segment count does not match the legacy video list.");
            }

            CanvasScaler scaler = manager.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution != new Vector2(1920f, 1080f) || Mathf.Abs(scaler.matchWidthOrHeight - 0.5f) > 0.001f)
            {
                AddError(report, city, sceneName + ": CanvasScaler is not normalized to 1920x1080 / Match 0.5.");
            }

            if (manager.questionPanel != null && manager.questionPanel.activeSelf)
            {
                AddError(report, city, sceneName + ": question panel is active in the serialized first-frame state.");
            }

            if (manager.videoPlayer == null || manager.videoPlayer.playOnAwake || Mathf.Abs(manager.videoPlayer.playbackSpeed - 1f) > 0.001f)
            {
                AddError(report, city, sceneName + ": VideoPlayer initial playback settings are not normalized.");
            }

            for (int segmentIndex = 0; segmentIndex < data.segments.Length; segmentIndex++)
            {
                VideoSegmentData segment = data.segments[segmentIndex];
                VideoQuizManager.QuizData legacy = manager.quizList[segmentIndex];
                if (legacy == null)
                {
                    AddError(report, city, sceneName + ": missing legacy question data at segment " + segmentIndex + ".");
                    continue;
                }

                if (legacy.videoClip != null)
                {
                    AddError(report, city, sceneName + ": legacy VideoClip reference must be cleared for WebGL at segment " + segmentIndex + ".");
                }

                string mediaPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "WebMedia", segment.mediaFile ?? string.Empty));
                if (string.IsNullOrWhiteSpace(segment.mediaFile) || !File.Exists(mediaPath))
                {
                    AddError(report, city, sceneName + ": missing URL media file at segment " + segmentIndex + ".");
                    continue;
                }

                string sourcePath = AssetDatabase.GUIDToAssetPath(segment.sourceGuid);
                VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(sourcePath);
                if (clip == null)
                {
                    AddError(report, city, sceneName + ": original source video cannot be resolved at segment " + segmentIndex + ".");
                    continue;
                }

                if (clip.audioTrackCount == 0)
                {
                    AddError(report, city, sceneName + ": video has no imported audio track: " + clip.name);
                }
                else
                {
                    city.videosWithAudio++;
                }

                if (clip.width == 0 || clip.height == 0)
                {
                    AddError(report, city, sceneName + ": invalid video dimensions: " + clip.name);
                }

                if (segment.subtitles == null || segment.subtitles.Length == 0)
                {
                    AddError(report, city, sceneName + ": missing subtitle cues for " + segment.segmentId + ".");
                }
                else
                {
                    double previousEnd = -1d;
                    for (int cueIndex = 0; cueIndex < segment.subtitles.Length; cueIndex++)
                    {
                        SubtitleCue cue = segment.subtitles[cueIndex];
                        city.subtitleCues++;
                        if (cue == null || cue.startTime < previousEnd || cue.endTime <= cue.startTime ||
                            string.IsNullOrWhiteSpace(cue.chinese) || string.IsNullOrWhiteSpace(cue.english))
                        {
                            AddError(report, city, sceneName + ": invalid subtitle cue in " + segment.segmentId + ".");
                            break;
                        }

                        if (clip.length > 0d && cue.endTime > clip.length + 1.25d)
                        {
                            AddError(report, city, sceneName + ": subtitle cue exceeds video duration in " + segment.segmentId + ".");
                        }
                        previousEnd = cue.endTime;
                    }
                }

                if (string.IsNullOrEmpty(legacy.question))
                {
                    continue;
                }

                city.questions++;
                QuestionData question = segment.question;
                if (question == null || string.IsNullOrWhiteSpace(question.questionId) || string.IsNullOrWhiteSpace(question.questionEn) ||
                    question.options == null || legacy.answers == null || question.options.Length != legacy.answers.Length)
                {
                    AddError(report, city, sceneName + ": incomplete bilingual question at segment " + segmentIndex + ".");
                    continue;
                }

                for (int optionIndex = 0; optionIndex < question.options.Length; optionIndex++)
                {
                    OptionData option = question.options[optionIndex];
                    if (option == null || string.IsNullOrWhiteSpace(option.optionId) || string.IsNullOrWhiteSpace(option.textEn))
                    {
                        AddError(report, city, sceneName + ": incomplete bilingual option in " + question.questionId + ".");
                    }
                }

                if (legacy.correctIndex < 0 || legacy.correctIndex >= question.options.Length ||
                    question.correctOptionId != question.options[legacy.correctIndex].optionId)
                {
                    AddError(report, city, sceneName + ": correctOptionId does not preserve the legacy correct answer in " + question.questionId + ".");
                }
            }

            if (sceneName == "5" && manager.completeButton == null)
            {
                report.warnings.Add("Nanjing legacy completeButton was null; the shared runtime completion view replaces it.");
            }

            report.cities.Add(city);
        }

        report.passed = report.errors.Count == 0;
        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../QA/editor-validation.json"));
        File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));

        if (!report.passed)
        {
            Debug.LogError("JIANGNAN_VALIDATION_FAIL: " + report.errors.Count + " error(s). See " + outputPath);
            throw new Exception("Jiangnan project validation failed.");
        }

        Debug.Log("JIANGNAN_VALIDATION_PASS: five cities, " + report.cities.Count + " validation records. Report: " + outputPath);
    }

    private static void ValidateBuildSettings(ValidationReport report)
    {
        if (EditorBuildSettings.scenes.Length != 6)
        {
            report.errors.Add("Build Settings must contain Start plus five city scenes.");
            return;
        }

        string[] expected = { "Start", "1", "2", "3", "4", "5" };
        for (int i = 0; i < expected.Length; i++)
        {
            string actual = Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[i].path);
            if (!EditorBuildSettings.scenes[i].enabled || actual != expected[i])
            {
                report.errors.Add("Build Settings scene order is invalid at index " + i + ".");
            }
        }
    }

    private static void ValidateFont(ValidationReport report)
    {
        Font font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (font == null)
        {
            report.errors.Add("Noto Sans SC font resource is missing.");
        }
    }

    private static void AddError(ValidationReport report, CityValidation city, string message)
    {
        city.passed = false;
        report.errors.Add(message);
    }
}
