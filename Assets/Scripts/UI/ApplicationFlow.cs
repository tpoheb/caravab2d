using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Главное меню, асинхронная загрузка, настройки и переходы из партии.</summary>
[DefaultExecutionOrder(-200)]
public sealed class ApplicationFlow : MonoBehaviour
{
    public static ApplicationFlow Instance { get; private set; }
    [SerializeField] private Texture2D _background;
    [SerializeField] private TMP_FontAsset _font;
    [SerializeField] private Sprite _buttonSprite;
    [SerializeField] private SaveGameCatalog _catalog;
    public SaveGameCatalog Catalog => _catalog;
    public bool IsBusy { get; private set; }
    public bool IsRestoring { get; private set; }
    public bool HasOverlay => _canvasRoot != null && _canvasRoot.activeSelf;
    public bool BlocksPauseInput => HasOverlay || _escapeConsumedFrame == Time.frameCount;
    private GameObject _canvasRoot, _main, _loading, _dialog, _events;
    private MenuUIBuilder _ui;
    private SettingsMenu _settings;
    private RawImage _art;
    private Image _progress;
    private TMP_Text _loadingText, _saveText, _message;
    private Button _continue, _newGame, _dialogAccept, _dialogCancel;
    private Action _accept, _cancel;
    private string _lastSaveError;
    private bool _settingsFromGame;
    private GameObject _previousSelection;
    private float _aspect;
    private Sprite _progressSprite;
    private int _escapeConsumedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        GameSettings.Load(); BuildUI();
    }
    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") ShowMainMenu();
        else _canvasRoot.SetActive(false);
    }
    private void Update()
    {
        // Кадрирование сохраняет пропорции иллюстрации на любом экране.
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        if (!Mathf.Approximately(aspect, _aspect))
        {
            _aspect = aspect;
            float artAspect = (float)_background.width / _background.height;
            float width = Mathf.Min(1, aspect / artAspect), height = Mathf.Min(1, artAspect / aspect);
            _art.uvRect = new Rect((1 - width) / 2, (1 - height) / 2, width, height);
        }
        if (!HasOverlay || IsBusy || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        _escapeConsumedFrame = Time.frameCount;
        if (_settings.Root.activeSelf) _settings.Close();
        else if (_dialog.activeSelf) CancelDialog();
    }
    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (_progressSprite != null) Destroy(_progressSprite);
    }

    public void ShowMainMenu()
    {
        IsBusy = false; IsRestoring = false; _settingsFromGame = false;
        Time.timeScale = 1; AudioListener.pause = false;
        _canvasRoot.SetActive(true); _main.SetActive(true); _loading.SetActive(false); _dialog.SetActive(false);
        _settings.Root.SetActive(false); _art.gameObject.SetActive(true);
        _events.SetActive(true); RefreshContinue(); Select(_continue.interactable ? _continue : _newGame);
    }
    private void RefreshContinue()
    {
        bool exists = GameSaveService.TryRead(out var data, out var error);
        _continue.interactable = exists;
        if (exists)
        {
            string date = DateTime.TryParse(data.savedAt, out var parsed) ? parsed.ToLocalTime().ToString("dd.MM.yyyy HH:mm") : "";
            _saveText.text = $"Автосохранение · ход {data.turn}\n{date}";
        }
        else _saveText.text = error == null ? "Ваше путешествие начинается здесь" : "Сохранение недоступно. Можно начать новую игру.";
        if (_lastSaveError != null) _saveText.text = _lastSaveError;
    }
    public void RequestNewGame()
    {
        if (IsBusy) return;
        if (GameSaveService.TryRead(out _, out _))
            ShowDialog("Начать новое путешествие?\nТекущее автосохранение будет заменено.", "Новая игра", "Отмена",
                () => StartCoroutine(LoadGame(false)), () => Select(_newGame));
        else StartCoroutine(LoadGame(false));
    }
    public void ContinueGame() { if (!IsBusy && GameSaveService.TryRead(out _, out _)) StartCoroutine(LoadGame(true)); }

    private IEnumerator LoadGame(bool resume)
    {
        CaravanSaveData saved = null;
        if (resume && !GameSaveService.TryRead(out saved, out var readError))
        { _lastSaveError = readError; RefreshContinue(); yield break; }
        IsRestoring = true; BeginLoading(resume ? "Возвращаемся к каравану…" : "Собираем караван…");
        _events.SetActive(false);
        yield return LoadScene("MapPrototipe");
        yield return null; yield return null;
        float deadline = Time.realtimeSinceStartup + 30;
        while ((GameManager.Instance == null || AITurnManager.Instance != null && !AITurnManager.Instance.IsReady) && Time.realtimeSinceStartup < deadline)
            yield return null;
        string error = null;
        try
        {
            if (GameManager.Instance == null) throw new InvalidOperationException("Сцена игры не готова.");
            if (resume) { GameSaveService.Validate(saved, _catalog); GameManager.Instance.RestoreSave(saved, _catalog); }
        }
        catch (Exception exception) { error = "Не удалось продолжить партию: " + exception.Message; Debug.LogWarning(error); }
        if (error != null)
        {
            // Не оставляем частично восстановленную партию доступной для игры.
            yield return LoadScene("MainMenu");
            _lastSaveError = error; ShowMainMenu(); yield break;
        }
        IsRestoring = false;
        if (!resume) NotifySaveResult(GameSaveService.TrySave(out error), error);
        _progress.fillAmount = 1; _loadingText.text = "Караван готов";
        yield return null;
        IsBusy = false; _canvasRoot.SetActive(false);
        if (_lastSaveError != null) ShowSaveFailure(false);
    }
    private IEnumerator LoadScene(string name)
    {
        float started = Time.realtimeSinceStartup;
        var operation = SceneManager.LoadSceneAsync(name);
        operation.allowSceneActivation = false;
        while (operation.progress < .9f || Time.realtimeSinceStartup - started < .65f)
        { _progress.fillAmount = operation.progress; yield return null; }
        _progress.fillAmount = .9f; operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
    }

    public void OpenSettings()
    {
        if (IsBusy) return;
        _settingsFromGame = SceneManager.GetActiveScene().name == "MapPrototipe";
        _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        _canvasRoot.SetActive(true); _main.SetActive(false); _loading.SetActive(false); _dialog.SetActive(false);
        _art.gameObject.SetActive(!_settingsFromGame);
        _settings.Open(() =>
        {
            if (_settingsFromGame) { _canvasRoot.SetActive(false); EventSystem.current?.SetSelectedGameObject(_previousSelection); }
            else { _main.SetActive(true); Select(_newGame); }
        });
    }
    public void LeaveGame(bool quit)
    { if (!IsBusy) StartCoroutine(SaveAndLeave(quit)); }
    private IEnumerator SaveAndLeave(bool quit)
    {
        BeginLoading("Сохраняем путешествие…");
        // Середина хода ИИ не является снимком целого мира. Заканчиваем очередь под экраном загрузки.
        PauseMenuController.Instance?.Resume();
        EventSystem.current?.SetSelectedGameObject(null);
        float deadline = Time.realtimeSinceStartup + 30;
        while (GameManager.Instance != null && !GameManager.Instance.CanSave && Time.realtimeSinceStartup < deadline) yield return null;
        bool saved = GameSaveService.TrySave(out var error); NotifySaveResult(saved, error);
        if (!saved) { IsBusy = false; ShowSaveFailure(quit); yield break; }
        if (quit) { QuitApplication(); yield break; }
        yield return LoadScene("MainMenu"); ShowMainMenu();
    }
    private void ShowSaveFailure(bool quit)
    {
        _canvasRoot.SetActive(true); _main.SetActive(false); _loading.SetActive(false); _art.gameObject.SetActive(false);
        PauseMenuController.Instance?.Pause();
        ShowDialog(_lastSaveError + "\nПартия остаётся открытой.", "Повторить", "Остаться",
            () => LeaveGame(quit), () => { _canvasRoot.SetActive(false); PauseMenuController.Instance?.Pause(); });
    }
    public void NotifySaveResult(bool success, string error) => _lastSaveError = success ? null : error;
    public void QuitFromMenu() { if (!IsBusy) QuitApplication(); }
    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void BeginLoading(string text)
    {
        IsBusy = true; _canvasRoot.SetActive(true); _main.SetActive(false); _dialog.SetActive(false);
        _settings.Root.SetActive(false); _art.gameObject.SetActive(true); _loading.SetActive(true);
        _loadingText.text = text; _progress.fillAmount = 0;
        EventSystem.current?.SetSelectedGameObject(null);
    }
    private void ShowDialog(string text, string accept, string cancel, Action action, Action canceled)
    {
        _message.text = text; _dialogAccept.GetComponentInChildren<TMP_Text>().text = accept;
        _dialogCancel.GetComponentInChildren<TMP_Text>().text = cancel;
        _accept = action; _cancel = canceled; _dialog.SetActive(true); Select(_dialogCancel);
        _dialogCancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _dialogAccept, selectOnRight = _dialogAccept };
        _dialogAccept.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _dialogCancel, selectOnRight = _dialogCancel };
    }
    private void CancelDialog() { _dialog.SetActive(false); var action = _cancel; _cancel = null; _accept = null; action?.Invoke(); }
    private static void Select(Button button) => EventSystem.current?.SetSelectedGameObject(button.gameObject);

    private void BuildUI()
    {
        _events = new GameObject("Ввод главного меню");
        _events.SetActive(false); _events.transform.SetParent(transform, false);
        _events.AddComponent<EventSystem>(); _events.AddComponent<InputSystemUIInputModule>();
        _canvasRoot = new GameObject("Главное меню — Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvasRoot.transform.SetParent(transform, false);
        var canvas = _canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 3000;
        var scaler = _canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        _ui = new MenuUIBuilder(_font, _buttonSprite);
        var artRect = MenuUIBuilder.Rect("Караван на закате", _canvasRoot.transform, Vector2.zero, Vector2.zero);
        MenuUIBuilder.Stretch(artRect); _art = artRect.gameObject.AddComponent<RawImage>(); _art.texture = _background;
        var shade = MenuUIBuilder.Image("Затемнение", _canvasRoot.transform, Vector2.zero, Vector2.zero, new Color(.03f, .02f, .01f, .3f));
        MenuUIBuilder.Stretch(shade.rectTransform);
        _main = MenuUIBuilder.Rect("Главное меню", _canvasRoot.transform, Vector2.zero, Vector2.zero).gameObject;
        MenuUIBuilder.Stretch((RectTransform)_main.transform);
        Title(_main.transform);
        var panel = _ui.Panel("Дорога зовёт", _main.transform, new Vector2(0, -105), new Vector2(490, 465));
        _continue = _ui.Button(panel, "Продолжить", new Vector2(0, 135), new Vector2(410, 66), ContinueGame);
        _newGame = _ui.Button(panel, "Новая игра", new Vector2(0, 52), new Vector2(410, 66), RequestNewGame);
        _ui.Button(panel, "Настройки", new Vector2(0, -31), new Vector2(410, 66), OpenSettings);
        _ui.Button(panel, "Выйти", new Vector2(0, -114), new Vector2(410, 66), QuitFromMenu);
        _saveText = _ui.Label(panel, "", new Vector2(0, -193), new Vector2(450, 65), 18, gold: true);
        _ui.Label(_main.transform, "КАРАВАНЫ · ТОРГОВЛЯ · ПРИКЛЮЧЕНИЯ", new Vector2(0, -470), new Vector2(1400, 45), 21, gold: true);
        _loading = MenuUIBuilder.Rect("Загрузка", _canvasRoot.transform, Vector2.zero, Vector2.zero).gameObject;
        MenuUIBuilder.Stretch((RectTransform)_loading.transform); Title(_loading.transform);
        var loadingPanel = _ui.Panel("Сборы в дорогу", _loading.transform, new Vector2(0, -370), new Vector2(980, 200));
        _loadingText = _ui.Label(loadingPanel, "Собираем караван…", new Vector2(0, 55), new Vector2(900, 45), 27);
        var track = MenuUIBuilder.Image("Прогресс", loadingPanel, Vector2.zero, new Vector2(860, 12), new Color(.3f, .24f, .16f));
        _progress = MenuUIBuilder.Image("Загрузка сцены", track.transform, Vector2.zero, Vector2.zero, MenuUIBuilder.Gold);
        _progressSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f));
        _progress.sprite = _progressSprite;
        MenuUIBuilder.Stretch(_progress.rectTransform); _progress.type = Image.Type.Filled; _progress.fillMethod = Image.FillMethod.Horizontal; _progress.fillAmount = 0;
        _ui.Label(loadingPanel, "Каждая дорога — новая история.", new Vector2(0, -55), new Vector2(900, 45), 20, gold: true);
        _settings = new SettingsMenu(_canvasRoot.transform, _ui);
        _dialog = MenuUIBuilder.Rect("Подтверждение", _canvasRoot.transform, Vector2.zero, Vector2.zero).gameObject;
        MenuUIBuilder.Stretch((RectTransform)_dialog.transform);
        var blocker = _dialog.AddComponent<Image>(); blocker.color = new Color(0, 0, 0, .7f);
        var dialogPanel = _ui.Panel("Решение", _dialog.transform, Vector2.zero, new Vector2(760, 340));
        _message = _ui.Label(dialogPanel, "", new Vector2(0, 60), new Vector2(690, 155), 27);
        _dialogCancel = _ui.Button(dialogPanel, "Отмена", new Vector2(-175, -85), new Vector2(300, 65), CancelDialog);
        _dialogAccept = _ui.Button(dialogPanel, "Подтвердить", new Vector2(175, -85), new Vector2(300, 65),
            () => { _dialog.SetActive(false); var action = _accept; _accept = null; _cancel = null; action?.Invoke(); });
        _dialog.SetActive(false); _loading.SetActive(false);
    }
    private void Title(Transform parent)
    {
        var title = _ui.Label(parent, "1000 ДОРОГ", new Vector2(0, 350), new Vector2(1450, 140), 110, gold: true);
        title.characterSpacing = 9;
        title.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .9f);
        _ui.Label(parent, "ПУТЬ КАРАВАНА", new Vector2(0, 258), new Vector2(1000, 55), 29);
    }
}
