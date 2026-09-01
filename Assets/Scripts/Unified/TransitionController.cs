using System.Collections;
using UnityEngine;

public sealed class TransitionController : MonoBehaviour
{
    private CanvasGroup fadeGroup;

    public bool IsBusy { get; private set; }

    public void Initialize(CanvasGroup group)
    {
        fadeGroup = group;
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        IsBusy = true;
        fadeGroup.blocksRaycasts = true;

        float startAlpha = fadeGroup.alpha;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
        fadeGroup.blocksRaycasts = targetAlpha > 0.01f;
        IsBusy = false;
    }
}
