using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Пауза поверх игровых окон, без изменения фазы личного хода.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance._paused;

    [Header("Оформление меню")]
    [SerializeField] private TMP_FontAsset _font;
    [SerializeField] private Sprite _panelSprite;
    [SerializeField] private Sprite _buttonSprite;

    private GameObject _canvasRoot;
    private GameObject _overlay;
    private GameObject _menuPanel;
    private GameObject _quitPanel;
    private Button _openButton;
    private Button _resumeButton;
    private Button _cancelQuitButton;
    private GameObject _previousSelection;
    private bool _paused;
    private float _previousTimeScale;
    private bool _previousAudioPause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildUI();
    }

    private void OnEnable()
    {
        if (_canvasRoot != null) _canvasRoot.SetActive(true);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void OnDisable()
    {
        Resume();
        if (_canvasRoot != null) _canvasRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Resume();
        Instance = null;
        if (_canvasRoot != null)
        {
            if (Application.isPlaying) Destroy(_canvasRoot);
            else DestroyImmediate(_canvasRoot);
        }
    }

    public void TogglePause()
    {
        if (!_paused) Pause();
        else if (_quitPanel.activeSelf) CancelQuit();
        else Resume();
    }

    public void Pause()
    {
        if (_paused || !isActiveAndEnabled || _overlay == null) return;

        _previousTimeScale = Time.timeScale;
        _previousAudioPause = AudioListener.pause;
        _previousSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        _paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        _openButton.gameObject.SetActive(false);
        _overlay.SetActive(true);
        _menuPanel.SetActive(true);
        _quitPanel.SetActive(false);
        Select(_resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!_paused) return;

        _paused = false;
        Time.timeScale = _previousTimeScale;
        AudioListener.pause = _previousAudioPause;
        if (_overlay != null) _overlay.SetActive(false);
        if (_openButton != null) _openButton.gameObject.SetActive(true);
        Select(_previousSelection != null && _previousSelection.activeInHierarchy
            ? _previousSelection : null);
        _previousSelection = null;
    }

    public void RequestQuit()
    {
        if (!_paused) return;
        _menuPanel.SetActive(false);
        _quitPanel.SetActive(true);
        Select(_cancelQuitButton.gameObject);
    }

    public void CancelQuit()
    {
        if (!_paused) return;
        _quitPanel.SetActive(false);
        _menuPanel.SetActive(true);
        Select(_resumeButton.gameObject);
    }

    public void ConfirmQuit()
    {
        if (!_paused || !_quitPanel.activeSelf) return;
        Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }

    private void BuildUI()
    {
        var root = new GameObject("Меню паузы — Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        _canvasRoot = root;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _openButton = CreateButton(root.transform, "Пауза", "Пауза  ·  Esc", Vector2.zero,
            new Vector2(180f, 52f), Pause);
        var openRect = (RectTransform)_openButton.transform;
        openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
        openRect.pivot = new Vector2(1f, 1f);
        openRect.anchoredPosition = new Vector2(-24f, -24f);
        _openButton.navigation = new Navigation { mode = Navigation.Mode.None };

        var overlay = CreateRect("Затемнение и блокировка ввода", root.transform);
        Stretch(overlay);
        overlay.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.02f, 0.015f, 0.78f);
        _overlay = overlay.gameObject;
        var backdropButton = _overlay.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.navigation = new Navigation { mode = Navigation.Mode.None };
        backdropButton.onClick.AddListener(() => Select(_quitPanel.activeSelf
            ? _cancelQuitButton.gameObject : _resumeButton.gameObject));

        _menuPanel = CreatePanel("Пауза", overlay);
        CreateLabel(_menuPanel.transform, "1000 ДОРОГ", new Vector2(0f, 145f), 19f, Gold);
        CreateLabel(_menuPanel.transform, "Пауза", new Vector2(0f, 92f), 44f, Cream);
        CreateLabel(_menuPanel.transform, "Караван ждёт вашего возвращения", new Vector2(0f, 44f), 20f, Cream);
        _resumeButton = CreateButton(_menuPanel.transform, "Продолжить", "Продолжить",
            new Vector2(0f, -25f), new Vector2(390f, 64f), Resume);
        var quitButton = CreateButton(_menuPanel.transform, "Выйти из игры", "Выйти из игры",
            new Vector2(0f, -103f), new Vector2(390f, 64f), RequestQuit);
        LinkNavigation(_resumeButton, quitButton);
        CreateLabel(_menuPanel.transform, "Esc — вернуться в игру", new Vector2(0f, -164f), 18f, Gold);

        _quitPanel = CreatePanel("Подтверждение выхода", overlay);
        CreateLabel(_quitPanel.transform, "ЗАВЕРШЕНИЕ ПУТЕШЕСТВИЯ", new Vector2(0f, 145f), 18f, Gold);
        CreateLabel(_quitPanel.transform, "Выйти из игры?", new Vector2(0f, 92f), 36f, Cream);
        CreateLabel(_quitPanel.transform, "Текущая партия не будет сохранена.", new Vector2(0f, 40f), 20f, Cream);
        _cancelQuitButton = CreateButton(_quitPanel.transform, "Остаться", "Остаться",
            new Vector2(0f, -25f), new Vector2(390f, 64f), CancelQuit);
        var confirmButton = CreateButton(_quitPanel.transform, "Подтвердить выход", "Выйти из игры",
            new Vector2(0f, -103f), new Vector2(390f, 64f), ConfirmQuit);
        LinkNavigation(_cancelQuitButton, confirmButton);
        CreateLabel(_quitPanel.transform, "Esc — отменить выход", new Vector2(0f, -164f), 18f, Gold);
        _quitPanel.SetActive(false);
        _overlay.SetActive(false);
    }

    private static Color Gold => new Color(0.83f, 0.68f, 0.42f);
    private static Color Cream => new Color(0.96f, 0.91f, 0.80f);

    private GameObject CreatePanel(string name, Transform parent)
    {
        var border = CreateRect(name, parent);
        border.sizeDelta = new Vector2(520f, 420f);
        border.gameObject.AddComponent<Image>().color = Gold;
        var body = CreateRect("Фон", border);
        Stretch(body, 2f);
        var image = body.gameObject.AddComponent<Image>();
        image.sprite = _panelSprite;
        image.color = _panelSprite != null ? Color.white : new Color(0.16f, 0.12f, 0.09f);
        return border.gameObject;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var rect = CreateRect(name, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = _buttonSprite;
        image.type = Image.Type.Sliced;
        image.color = _buttonSprite != null ? Color.white : new Color(0.28f, 0.21f, 0.14f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.8f, 0.73f, 0.6f);
        button.colors = colors;
        button.onClick.AddListener(action);
        var text = CreateLabel(rect, label, Vector2.zero, size.y < 60f ? 20f : 25f, Cream);
        Stretch(text.rectTransform, 8f);
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 position,
        float fontSize, Color color)
    {
        var rect = CreateRect(text, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(480f, 52f);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void LinkNavigation(Button first, Button second)
    {
        // Фокус остаётся в меню и не попадает на игровые кнопки под затемнением.
        first.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnUp = second, selectOnDown = second
        };
        second.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnUp = first, selectOnDown = first
        };
    }
}
