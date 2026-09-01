using UnityEngine;
using UnityEngine.UI;

public sealed class BilingualText : MonoBehaviour
{
    public Text targetText;
    [TextArea] public string chineseText;
    [TextArea] public string englishText;
    public Graphic englishOverlayBackground;
    public bool hideWhenEmpty;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        LanguageManager manager = LanguageManager.EnsureExists();
        manager.LanguageChanged -= Refresh;
        manager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (targetText == null)
        {
            return;
        }

        bool english = LanguageManager.EnsureExists().IsEnglish;
        string value = english ? englishText : chineseText;
        targetText.text = value;
        bool visible = !hideWhenEmpty || !string.IsNullOrEmpty(value);
        targetText.enabled = visible;
        if (englishOverlayBackground != null)
        {
            englishOverlayBackground.enabled = visible;
        }
    }
}
