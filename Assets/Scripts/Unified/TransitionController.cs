using System.Collections;
using UnityEngine;

public sealed class TransitionController : MonoBehaviour
{
    private CanvasGroup fadeGroup;
    private RectTransform[] ripples;

    public bool IsBusy { get; private set; }

    public void Initialize(CanvasGroup group)
    {
        fadeGroup = group;
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
        ripples = new RectTransform[3];
        for (int i = 0; i < ripples.Length; i++)
        {
            Transform ripple = group.transform.Find("Ripple" + (i + 1));
            ripples[i] = ripple as RectTransform;
        }
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
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            for (int i = 0; i < ripples.Length; i++)
            {
                if (ripples[i] != null)
                {
                    float pulse = 1f + Mathf.Sin((t + i * 0.17f) * Mathf.PI) * (0.05f + i * 0.025f);
                    ripples[i].localScale = new Vector3(pulse, 1f, 1f);
                }
            }
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
        fadeGroup.blocksRaycasts = targetAlpha > 0.01f;
        IsBusy = false;
    }
}
