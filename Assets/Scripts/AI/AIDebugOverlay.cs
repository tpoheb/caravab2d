using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Простой дебажный оверлей для ИИ-операций.
/// Фиксированная панель, TextMeshPro без ScrollRect — максимально надёжно.
/// Клавиша Tab — показать/скрыть.
/// </summary>
public class AIDebugOverlay : MonoBehaviour
{
    [Header("Видимость")]
    [SerializeField] private KeyCode _toggleKey      = KeyCode.Tab;
    [SerializeField] private bool    _visibleOnStart = true;

    [Header("Панель")]
    [SerializeField] private Vector2 _panelSize   = new Vector2(420f, 500f);
    [SerializeField] private Vector2 _panelOffset = new Vector2(16f, 16f);

    [Header("Цвета")]
    [SerializeField] private Color _bgColor      = new Color(0f,   0f,   0f,   0.80f);
    [SerializeField] private Color _headerColor  = new Color(0.9f, 0.75f, 0.3f, 1f);
    [SerializeField] private Color _buyColor     = new Color(0.4f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color _sellColor    = new Color(0.9f, 0.5f, 0.3f, 1f);
    [SerializeField] private Color _moveColor    = new Color(0.5f, 0.8f, 1f,   1f);
    [SerializeField] private Color _financeColor = new Color(1f,   0.9f, 0.4f, 1f);
    [SerializeField] private Color _textColor    = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("Шрифт")]
    [SerializeField] private int _headerSize = 13;
    [SerializeField] private int _lineSize   = 11;

    [Tooltip("Сколько последних строк держать")]
    [SerializeField] private int _maxLines = 25;

    // ── runtime ──────────────────────────────────────────────────────
    private GameObject      _root;
    private TextMeshProUGUI _headerTMP;
    private TextMeshProUGUI _bodyTMP;

    private readonly List<string> _lines = new List<string>();

    // ── lifecycle ────────────────────────────────────────────────────

    private void Awake()       => Build();
    private void OnEnable()    { AIDebugLog.OnNewTurn += OnNewTurn; AIDebugLog.OnEntryAdded += OnEntry; }
    private void OnDisable()   { AIDebugLog.OnNewTurn -= OnNewTurn; AIDebugLog.OnEntryAdded -= OnEntry; }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[ToKey(_toggleKey)].wasPressedThisFrame)
            _root.SetActive(!_root.activeSelf);
    }

    // ── handlers ─────────────────────────────────────────────────────

    private void OnNewTurn(int turn)
    {
        _lines.Clear();
        _headerTMP.text = $"⚙  ИИ-лог  |  Ход {turn}  |  {_toggleKey} — скрыть";
        Repaint();
    }

    private void OnEntry(string line)
    {
        _lines.Add(Colorize(line));
        while (_lines.Count > _maxLines)
            _lines.RemoveAt(0);
        Repaint();
    }

    private void Repaint()
    {
    if (_bodyTMP == null)
        {
        Debug.LogError("[AIDebugOverlay] _bodyTMP == null при Repaint!");
        return;
        }
        _bodyTMP.text = _lines.Count > 0
        ? string.Join("\n", _lines)
        : "<color=#555555><i>нет событий</i></color>";
    }

    // ── colorize ─────────────────────────────────────────────────────

    private string Colorize(string line)
    {
        if (line.StartsWith("[Куп]"))    return Wrap(line, _buyColor);
        if (line.StartsWith("[Прод]"))   return Wrap(line, _sellColor);
        if (line.StartsWith("[Ход]"))    return Wrap(line, _moveColor);
        if (line.StartsWith("[Золото]")) return Wrap(line, _financeColor);
        return line;
    }

    private static string Wrap(string s, Color c) =>
        $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";

    // ── build UI ─────────────────────────────────────────────────────

    private void Build()
    {
        // Canvas на этом же GameObject
        var canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (!gameObject.GetComponent<GraphicRaycaster>())
            gameObject.AddComponent<GraphicRaycaster>();

        // Корневая панель (правый верхний угол)
        _root = MakeGO("DebugPanel", gameObject.transform);
        var rootRect = _root.AddComponent<RectTransform>();
        rootRect.anchorMin        = new Vector2(0, 1);
        rootRect.anchorMax        = new Vector2(0, 1);
        rootRect.pivot            = new Vector2(0, 1);
        rootRect.anchoredPosition = new Vector2(_panelOffset.x, -_panelOffset.y);
        rootRect.sizeDelta        = _panelSize;

        var bg = _root.AddComponent<Image>();
        bg.color = _bgColor;

        // Заголовок
        var headerGO = MakeGO("Header", _root.transform);
        var headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot     = new Vector2(0, 1);
        headerRect.offsetMin = new Vector2(8,  0);
        headerRect.offsetMax = new Vector2(-8, 0);
        headerRect.sizeDelta = new Vector2(0, 26);

        _headerTMP           = headerGO.AddComponent<TextMeshProUGUI>();
        _headerTMP.text      = $"⚙  ИИ-лог  |  {_toggleKey} — скрыть";
        _headerTMP.fontSize  = _headerSize;
        _headerTMP.color     = _headerColor;
        _headerTMP.fontStyle = FontStyles.Bold;
        _headerTMP.overflowMode = TextOverflowModes.Ellipsis;

        // Разделитель
        var divGO   = MakeGO("Divider", _root.transform);
        var divRect = divGO.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0, 1);
        divRect.anchorMax = new Vector2(1, 1);
        divRect.pivot     = new Vector2(0, 1);
        divRect.offsetMin = new Vector2(6,  0);
        divRect.offsetMax = new Vector2(-6, 0);
        divRect.anchoredPosition = new Vector2(0, -28);
        divRect.sizeDelta        = new Vector2(0, 1);
        divGO.AddComponent<Image>().color = new Color(1, 1, 1, 0.2f);

        // Тело лога
        var bodyGO   = MakeGO("Body", _root.transform);
        var bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.offsetMin = new Vector2(8,  6);
        bodyRect.offsetMax = new Vector2(-8, -32);

        _bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyTMP.text            = "<color=#555555><i>Ожидание хода...</i></color>";
        _bodyTMP.fontSize        = _lineSize;
        _bodyTMP.color           = _textColor;
        _bodyTMP.richText        = true;
        _bodyTMP.enableWordWrapping  = true;
        _bodyTMP.overflowMode        = TextOverflowModes.Truncate;
        _bodyTMP.verticalAlignment   = VerticalAlignmentOptions.Top;

        _root.SetActive(_visibleOnStart);
    }

    private static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    // ── KeyCode → Key ────────────────────────────────────────────────

    private static Key ToKey(KeyCode kc) => kc switch
    {
        KeyCode.Tab       => Key.Tab,
        KeyCode.Space     => Key.Space,
        KeyCode.BackQuote => Key.Backquote,
        KeyCode.F1        => Key.F1,
        KeyCode.F2        => Key.F2,
        KeyCode.F3        => Key.F3,
        KeyCode.F4        => Key.F4,
        KeyCode.F5        => Key.F5,
        KeyCode.F6        => Key.F6,
        KeyCode.F7        => Key.F7,
        KeyCode.F8        => Key.F8,
        KeyCode.Alpha1    => Key.Digit1,
        KeyCode.Alpha2    => Key.Digit2,
        KeyCode.Alpha3    => Key.Digit3,
        KeyCode.Alpha4    => Key.Digit4,
        KeyCode.Alpha5    => Key.Digit5,
        _                 => Key.Tab
    };
}