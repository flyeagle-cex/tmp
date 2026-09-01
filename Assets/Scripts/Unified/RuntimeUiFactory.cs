using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RuntimeUiFactory
{
    public static readonly Color DeepBlue = new Color32(7, 23, 38, 255);
    public static readonly Color Navy = new Color32(12, 38, 60, 255);
    public static readonly Color PanelBlue = new Color32(18, 51, 76, 248);
    public static readonly Color Jade = new Color32(77, 177, 161, 255);
    public static readonly Color Gold = new Color32(211, 177, 111, 255);
    public static readonly Color Paper = new Color32(239, 237, 225, 255);
    public static readonly Color Muted = new Color32(161, 178, 188, 255);

    private static TMP_FontAsset runtimeFont;

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject host = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject host = new GameObject(name, typeof(RectTransform));
        RectTransform rect = host.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        TMP_FontAsset font = GetFont();
        if (font != null)
        {
            text.font = font;
        }
        return text;
    }

    public static Button CreateButton(string name, Transform parent, string label, UnityAction action, Color background, Color foreground, float fontSize = 28f)
    {
        Image image = CreatePanel(name, parent, background);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.85f, 0.87f, 1f);
        colors.disabledColor = new Color(0.45f, 0.48f, 0.5f, 0.65f);
        button.colors = colors;
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        TMP_Text text = CreateText("Label", image.transform, label, fontSize, foreground, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 14f, 8f, 14f, 8f);
        return button;
    }

    public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static TMP_FontAsset GetFont()
    {
        if (runtimeFont != null)
        {
            return runtimeFont;
        }

        Font source = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (source == null)
        {
            Debug.LogWarning("NotoSansSC-Regular could not be loaded; TMP default font will be used.");
            return null;
        }

        runtimeFont = TMP_FontAsset.CreateFontAsset(source);
        runtimeFont.name = "Jiangnan NotoSansSC Dynamic";
        runtimeFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        return runtimeFont;
    }
}
