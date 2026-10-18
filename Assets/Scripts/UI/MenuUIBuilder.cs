using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Общее оформление главного меню, настроек и загрузки.</summary>
public sealed class MenuUIBuilder
{
    public static Color Gold => new Color(0.83f, 0.68f, 0.42f);
    public static Color Cream => new Color(0.96f, 0.91f, 0.80f);
    private readonly TMP_FontAsset _font;
    private readonly Sprite _button;
    public MenuUIBuilder(TMP_FontAsset font, Sprite button) { _font = font; _button = button; }
    public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    public static void Stretch(RectTransform rect, float inset = 0)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset; }
    public static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    public RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var border = Image(name, parent, position, size, Gold);
        var body = Image("Тёмное дерево", border.transform, Vector2.zero, size, new Color(0.10f, 0.075f, 0.05f, 0.96f));
        Stretch(body.rectTransform, 2);
        return border.rectTransform;
    }
    public TextMeshProUGUI Label(Transform parent, string text, Vector2 position, Vector2 size, float fontSize,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool gold = false)
    {
        var label = Rect(text, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.text = text; label.fontSize = fontSize; label.color = gold ? Gold : Cream;
        label.alignment = alignment; label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }
    public Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityAction action)
    {
        var image = Image(text, parent, position, size, _button == null ? new Color(0.28f, 0.21f, 0.14f) : Color.white);
        image.sprite = _button; image.type = UnityEngine.UI.Image.Type.Sliced;
        // Масштаб рамки зависит от высоты кнопки; середина растягивается по ширине.
        if (_button != null) image.pixelsPerUnitMultiplier = Mathf.Max(1, _button.rect.height / size.y);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1.15f, 1.1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.73f, 0.6f); colors.disabledColor = new Color(0.48f, 0.45f, 0.4f);
        button.colors = colors; button.onClick.AddListener(action);
        var textLabel = Label(button.transform, text, Vector2.zero, size, 26);
        Stretch(textLabel.rectTransform, 8);
        return button;
    }
    public Slider Slider(Transform parent, string name, Vector2 position, float value, UnityAction<float> changed)
    {
        var root = Rect(name, parent, position, new Vector2(350, 46));
        var track = Image("Дорожка", root, Vector2.zero, new Vector2(350, 8), new Color(0.28f, 0.22f, 0.15f));
        track.raycastTarget = false;
        var fillArea = Rect("Область заполнения", root, Vector2.zero, new Vector2(330, 8));
        var fill = Image("Золото", fillArea, Vector2.zero, Vector2.zero, Gold); Stretch(fill.rectTransform);
        var handleArea = Rect("Область бегунка", root, Vector2.zero, new Vector2(330, 28));
        var handle = Image("Бегунок", handleArea, Vector2.zero, new Vector2(20, 0), Cream);
        var slider = root.gameObject.AddComponent<Slider>();
        slider.targetGraphic = handle; slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform;
        slider.minValue = 0; slider.maxValue = 1; slider.value = value; slider.onValueChanged.AddListener(changed);
        return slider;
    }
}
