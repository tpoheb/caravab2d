using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Canvas Overlay поверх игры — показывает лог ИИ-операций текущего хода.
///
/// Настройка (один раз):
/// 1. Создай пустой GameObject "AIDebugOverlay" в сцене.
/// 2. Добавь этот компонент.
/// 3. Запусти игру — Canvas и UI создадутся автоматически.
/// 4. Клавиша Tab по умолчанию переключает видимость.
///
/// Зависимости: TextMeshPro, Unity Input System.
/// </summary>
public class AIDebugOverlay : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------

    [Header("Видимость")]
    [Tooltip("Горячая клавиша показа/скрытия оверлея")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;

    [Tooltip("Показывать оверлей при старте игры")]
    [SerializeField] private bool _visibleOnStart = true;

    [Header("Позиция и размер панели")]
    [Tooltip("Отступ от угла экрана в пикселях")]
    [SerializeField] private Vector2 _panelOffset = new Vector2(16f, 16f);
    [SerializeField] private Vector2 _panelSize   = new Vector2(400f, 520f);

    [Header("Визуал")]
    [SerializeField] private Color _bgColor      = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color _headerColor  = new Color(0.9f, 0.75f, 0.3f, 1f);
    [SerializeField] private Color _textColor    = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color _buyColor     = new Color(0.4f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color _sellColor    = new Color(0.9f, 0.5f, 0.3f, 1f);
    [SerializeField] private Color _moveColor    = new Color(0.5f, 0.8f, 1f,  1f);
    [SerializeField] private Color _financeColor = new Color(1f,   0.9f, 0.4f, 1f);

    [SerializeField] private int _headerFontSize  = 14;
    [SerializeField] private int _entryFontSize   = 12;

    [Tooltip("Максимум строк в скролле (старые обрезаются при превышении)")]
    [SerializeField] private int _maxVisibleLines = 30;

    // ------------------------------------------------------------------
    // Runtime refs
    // ------------------------------------------------------------------

    private Canvas          _canvas;
    private GameObject      _panel;
    private TextMeshProUGUI _headerText;
    private TextMeshProUGUI _contentText;
    private ScrollRect      _scrollRect;

    private readonly List<string> _displayLines = new List<string>();
    private bool _isDirty;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        BuildUI();
        SetVisible(_visibleOnStart);
    }

    private void OnEnable()
    {
        AIDebugLog.OnNewTurn    += HandleNewTurn;
        AIDebugLog.OnEntryAdded += HandleEntryAdded;
    }

    private void OnDisable()
    {
        AIDebugLog.OnNewTurn    -= HandleNewTurn;
        AIDebugLog.OnEntryAdded -= HandleEntryAdded;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current[ToKey(_toggleKey)].wasPressedThisFrame)
            SetVisible(!_panel.activeSelf);

        if (_isDirty)
        {
            RefreshContent();
            _isDirty = false;
        }
    }

    // ------------------------------------------------------------------
    // Handlers
    // ------------------------------------------------------------------

    private void HandleNewTurn(int turn)
    {
        _displayLines.Clear();
        UpdateHeader(turn);
        _isDirty = true;
    }

    private void HandleEntryAdded(string line)
    {
        _displayLines.Add(ColorLine(line));

        while (_displayLines.Count > _maxVisibleLines)
            _displayLines.RemoveAt(0);

        _isDirty = true;
    }

    // ------------------------------------------------------------------
    // Логика
    // ------------------------------------------------------------------

    private string ColorLine(string line)
    {
        if (line.StartsWith("[Куп]"))    return Colorize(line, _buyColor);
        if (line.StartsWith("[Прод]"))   return Colorize(line, _sellColor);
        if (line.StartsWith("[Ход]"))    return Colorize(line, _moveColor);
        if (line.StartsWith("[Золото]")) return Colorize(line, _financeColor);
        return line;
    }

    private static string Colorize(string text, Color c)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
    }

    private void UpdateHeader(int turn)
    {
        if (_headerText != null)
            _headerText.text = $"⚙ ИИ-лог  |  Ход {turn}  |  [{_toggleKey} — скрыть]";
    }

    private void RefreshContent()
    {
        if (_contentText == null) return;
        _contentText.text = string.Join("\n", _displayLines);

        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetVisible(bool visible)
    {
        if (_panel != null)
            _panel.SetActive(visible);
    }

    // ------------------------------------------------------------------
    // Конвертация KeyCode → Key (без extension method)
    // ------------------------------------------------------------------

    private static Key ToKey(KeyCode kc) => kc switch
    {
        KeyCode.Tab        => Key.Tab,
        KeyCode.Space      => Key.Space,
        KeyCode.BackQuote  => Key.Backquote,
        KeyCode.F1         => Key.F1,
        KeyCode.F2         => Key.F2,
        KeyCode.F3         => Key.F3,
        KeyCode.F4         => Key.F4,
        KeyCode.F5         => Key.F5,
        KeyCode.F6         => Key.F6,
        KeyCode.F7         => Key.F7,
        KeyCode.F8         => Key.F8,
        KeyCode.Alpha1     => Key.Digit1,
        KeyCode.Alpha2     => Key.Digit2,
        KeyCode.Alpha3     => Key.Digit3,
        KeyCode.Alpha4     => Key.Digit4,
        KeyCode.Alpha5     => Key.Digit5,
        _                  => Key.Tab
    };

    // ------------------------------------------------------------------
    // Построение UI программно
    // ------------------------------------------------------------------

    private void BuildUI()
    {
        // Canvas
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // Панель
        _panel = new GameObject("Panel");
        _panel.transform.SetParent(_canvas.transform, false);

        var panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(1f, 1f);
        panelRect.anchorMax        = new Vector2(1f, 1f);
        panelRect.pivot            = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-_panelOffset.x, -_panelOffset.y);
        panelRect.sizeDelta        = _panelSize;

        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = _bgColor;

        var layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding               = new RectOffset(8, 8, 6, 6);
        layout.spacing               = 4f;
        layout.childControlWidth     = true;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        // Заголовок
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(_panel.transform, false);
        _headerText           = headerGO.AddComponent<TextMeshProUGUI>();
        _headerText.text      = $"⚙ ИИ-лог  |  Ход 0  |  [{_toggleKey} — скрыть]";
        _headerText.fontSize  = _headerFontSize;
        _headerText.color     = _headerColor;
        _headerText.fontStyle = FontStyles.Bold;
        headerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);

        // Разделитель
        var dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(_panel.transform, false);
        dividerGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        dividerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);

        // ScrollView
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(_panel.transform, false);
        scrollGO.AddComponent<RectTransform>();
        scrollGO.AddComponent<LayoutElement>().flexibleHeight = 1f;

        _scrollRect                   = scrollGO.AddComponent<ScrollRect>();
        _scrollRect.horizontal        = false;
        _scrollRect.vertical          = true;
        _scrollRect.scrollSensitivity = 20f;

        // Viewport
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var vpRect        = viewportGO.AddComponent<RectTransform>();
        vpRect.anchorMin  = Vector2.zero;
        vpRect.anchorMax  = Vector2.one;
        vpRect.offsetMin  = Vector2.zero;
        vpRect.offsetMax  = Vector2.zero;
        var vpMask        = viewportGO.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        viewportGO.AddComponent<Image>().color = Color.clear;
        _scrollRect.viewport = vpRect;

        // Content
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRect       = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot     = new Vector2(0f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        contentGO.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        _contentText                    = contentGO.AddComponent<TextMeshProUGUI>();
        _contentText.fontSize           = _entryFontSize;
        _contentText.color              = _textColor;
        _contentText.richText           = true;
        _contentText.enableWordWrapping = true;
        _contentText.text               = "<color=#888888><i>Ожидание хода...</i></color>";

        _scrollRect.content = contentRect;
    }
}