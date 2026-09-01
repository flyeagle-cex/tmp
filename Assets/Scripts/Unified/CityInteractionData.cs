using System;

[Serializable]
public sealed class LocalizedText
{
    public string zh;
    public string en;

    public string Get(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(en))
        {
            return en;
        }

        return zh ?? string.Empty;
    }
}

[Serializable]
public sealed class OptionData
{
    public string optionId;
    public string textZh;
    public string textEn;

    public string GetText(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(textEn))
        {
            return textEn;
        }

        return textZh ?? string.Empty;
    }
}

[Serializable]
public sealed class QuestionData
{
    public string questionId;
    public string questionZh;
    public string questionEn;
    public OptionData[] options;
    public string correctOptionId;
    public string successZh;
    public string successEn;

    public string GetQuestion(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(questionEn))
        {
            return questionEn;
        }

        return questionZh ?? string.Empty;
    }

    public string GetSuccess(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(successEn))
        {
            return successEn;
        }

        return successZh ?? string.Empty;
    }
}

[Serializable]
public sealed class SubtitleCue
{
    public double startTime;
    public double endTime;
    public string chinese;
    public string english;
}

[Serializable]
public sealed class VideoSegmentData
{
    public string segmentId;
    public string mediaFile;
    public string sourceGuid;
    public string chapterZh;
    public string chapterEn;
    public QuestionData question;
    public SubtitleCue[] subtitles;

    public string GetChapter(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(chapterEn))
        {
            return chapterEn;
        }

        return chapterZh ?? string.Empty;
    }
}

[Serializable]
public sealed class CityInteractionData
{
    public string cityId;
    public string sceneName;
    public string cityNameZh;
    public string cityNameEn;
    public string titleZh;
    public string titleEn;
    public VideoSegmentData[] segments;

    public string GetCityName(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(cityNameEn))
        {
            return cityNameEn;
        }

        return cityNameZh ?? sceneName ?? string.Empty;
    }

    public string GetTitle(AppLanguage language)
    {
        if (language == AppLanguage.English && !string.IsNullOrEmpty(titleEn))
        {
            return titleEn;
        }

        return titleZh ?? string.Empty;
    }
}
