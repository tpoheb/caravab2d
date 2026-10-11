using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Полный маршрут меню → игра → автосохранение → меню → продолжение в реальной сцене.</summary>
[InitializeOnLoad]
public static class GameMenuPlayModeChecks
{
    [Serializable] private sealed class SavedSceneSetup { public SceneSetup[] scenes; }
    private const string EditorSetup = "GameMenuChecks.EditorSetup";
    private const string EditorQuality = "GameMenuChecks.EditorQuality";
    private const string Running = "GameMenuChecks.Running", Stopping = "GameMenuChecks.Stopping", Deadline = "GameMenuChecks.Deadline", ExitCode = "GameMenuChecks.ExitCode";
    private const string Preferences = "Caravan.Checks.Settings";
    private static string CheckPath => Path.GetFullPath("Library/GameMenuPreview/check-autosave.json");
    private static int _stage, _nextFrame;
    private static CaravanSaveData _expected;
    private static bool _stopping;
    private static int _exitCode;
    private static int _loggedStage = -1;
    private static Keyboard _keyboard;
    private static InputSettings _originalInput, _checkInput;
    static GameMenuPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (!SessionState.GetBool(Running, false)) return;
        _stopping = SessionState.GetBool(Stopping, false); _exitCode = SessionState.GetInt(ExitCode, 1);
        EditorApplication.update += Tick;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void IsolateData()
    {
        if (!SessionState.GetBool(Running, false)) return;
        Application.runInBackground = true;
        GameSaveService.OverridePath = CheckPath; GameSettings.OverrideKey = Preferences;
    }
    // Без -quit: завершает редактор после проверки и выхода из Play Mode.
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Запустите в отдельном batchmode Editor.");
        RunChecks();
    }
    [MenuItem("Tools/Проверки/Главное меню и сохранения в Play Mode")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount)
            .Any(index => SceneManager.GetSceneAt(index).isDirty))
            throw new InvalidOperationException("Запустите вне Play Mode, сохранив открытые сцены.");
        SessionState.SetString(EditorSetup, JsonUtility.ToJson(new SavedSceneSetup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        SessionState.SetInt(EditorQuality, QualitySettings.GetQualityLevel());
        SessionState.SetBool("GameMenuChecks.RunInBackground", Application.runInBackground);
        SessionState.SetFloat("GameMenuChecks.AudioVolume", AudioListener.volume);
        RunChecks();
    }
    private static void RunChecks()
    {
        _stage = 0; _nextFrame = 0; _stopping = false; _loggedStage = -1;
        PauseMenuChecks.RunAll();
        Directory.CreateDirectory(Path.GetDirectoryName(CheckPath));
        if (File.Exists(CheckPath)) File.Delete(CheckPath);
        PlayerPrefs.DeleteKey(Preferences);
        EditorSceneManager.OpenScene(GameMenuSetup.MainMenuPath);
        SessionState.SetBool(Running, true); SessionState.SetBool(Stopping, false);
        SessionState.SetFloat(Deadline, (float)EditorApplication.timeSinceStartup + 480);
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        try
        {
            if (_stopping)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(Running, false); PlayerPrefs.DeleteKey(Preferences);
                    EditorApplication.update -= Tick;
                    File.WriteAllText("Library/GameMenuPreview/check-result.txt", _exitCode == 0 ? "GAME_MENU_PLAY_MODE_CHECKS_PASSED" : "GAME_MENU_PLAY_MODE_CHECKS_FAILED");
                    if (Application.isBatchMode) EditorApplication.Exit(_exitCode);
                    else
                    {
                        QualitySettings.SetQualityLevel(SessionState.GetInt(EditorQuality, 2), true);
                        Application.runInBackground = SessionState.GetBool("GameMenuChecks.RunInBackground", false);
                        AudioListener.volume = SessionState.GetFloat("GameMenuChecks.AudioVolume", 1);
                        var saved = JsonUtility.FromJson<SavedSceneSetup>(SessionState.GetString(EditorSetup, ""));
                        if (saved != null) EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
                        SessionState.EraseString(EditorSetup);
                    }
                }
                return;
            }
            Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Deadline, 0), "Таймаут проверки меню.");
            if (Application.isPlaying) EditorApplication.QueuePlayerLoopUpdate();
            if (!Application.isPlaying || Time.frameCount < Math.Max(5, _nextFrame)) return;
            if (_loggedStage != _stage) { _loggedStage = _stage; Debug.Log("GAME_MENU_CHECK_STAGE_" + _stage); }
            var flow = ApplicationFlow.Instance;
            switch (_stage)
            {
                case 0:
                    _originalInput = InputSystem.settings; _checkInput = UnityEngine.Object.Instantiate(_originalInput);
                    _checkInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    _checkInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    _checkInput.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                    InputSystem.settings = _checkInput; _keyboard = InputSystem.AddDevice<Keyboard>();
                    Require(flow != null && !Get<Button>(flow, "_continue").interactable, "Без сохранения продолжение недоступно.");
                    Capture(flow, "MainMenu.png"); flow.OpenSettings();
                    break;
                case 1:
                    var settings = Get<SettingsMenu>(flow, "_settings");
                    CheckChoiceNavigation(settings);
                    Capture(flow, "Settings.png");
                    Get<Slider>(settings, "_sound").value = .35f; Get<Slider>(settings, "_music").value = .2f;
                    settings.Root.GetComponentsInChildren<Button>().Single(button => button.name == "Качество").onClick.Invoke();
                    settings.Root.GetComponentsInChildren<Button>().Single(button => button.name == "Полный экран").onClick.Invoke();
                    Get<Button>(settings, "_apply").onClick.Invoke();
                    Require(Mathf.Approximately(AudioListener.volume, .35f) && Mathf.Approximately(GameSettings.Current.music, .2f), "Настройки звука применяются.");
                    Require(GameSettings.Current.quality == QualitySettings.GetQualityLevel() && !GameSettings.Current.fullscreen,
                        "Сохраняются качество и режим экрана.");
                    flow.RequestNewGame(); Capture(flow, "Loading.png");
                    break;
                case 2:
                    if (flow.IsBusy) return;
                    Require(SceneManager.GetActiveScene().name == ApplicationFlow.GameSceneName && GameManager.Instance.State == GameState.InCity,
                        "Новая игра должна открыть стартовый город.");
                    Require(GameSaveService.TryRead(out var first, out _) && first.turn == 1, "Новая партия создаёт автосохранение.");
                    CheckCityHand(PauseMenuController.Instance);
                    PrepareCaravan();
                    var city = GameSaveService.Find<CityManager>().PlayerCurrentCity;
                    GameManager.Instance.OnPathSelected(city.Paths.First(path => path != null && path.CellCount >= 3));
                    DrawType<ShadowCardData>();
                    GameManager.Instance.AdvancePhase(); GameManager.Instance.AdvancePhase(); GameManager.Instance.AdvancePhase();
                    break;
                case 3:
                    if (!GameManager.Instance.CanSave || GameManager.Instance.TurnNumber < 2) return;
                    Require(GameSaveService.TryRead(out var next, out _) && next.turn == 2 && next.aiTurn == 1, "После хода сохраняются игрок и очередь ИИ.");
                    DrawType<BattleCardData>(); GameManager.Instance.AdvancePhase();
                    Require(GameManager.Instance.State == GameState.InBattle, "Проверяем сохранение незавершённого боя.");
                    Require(Get<Transform>(HandManager.Instance, "handTransform").gameObject.activeSelf,
                        "В дорожной встрече рука показывается автоматически.");
                    _expected = GameManager.Instance.CaptureSave(flow.Catalog);
                    EventSystem.current.SetSelectedGameObject(Get<Button>(GameSaveService.Find<BattleManager>().GetUIManager(), "endTurnButton").gameObject);
                    var pause = PauseMenuController.Instance; pause.Pause();
                    MenuPreviewCapture.Save(Get<GameObject>(pause, "_canvasRoot").GetComponent<Canvas>(), "Pause.png");
                    flow.OpenSettings();
                    break;
                case 4:
                    Require(PauseMenuController.IsPaused && Time.timeScale == 0, "Настройки из паузы не запускают игровой ход.");
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape)); _stage = 39;
                    break;
                case 40:
                    Require(!Get<SettingsMenu>(flow, "_settings").Root.activeSelf && PauseMenuController.IsPaused,
                        "Esc закрывает настройки без одновременного снятия паузы.");
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                    PauseMenuController.Instance.RequestMainMenu(); PauseMenuController.Instance.ConfirmQuit();
                    Require(EventSystem.current.currentSelectedGameObject == null, "При загрузке клавиатурный фокус уходит с игровых кнопок.");
                    _stage = 4;
                    break;
                case 5:
                    if (flow.IsBusy) return;
                    Require(SceneManager.GetActiveScene().name == "MainMenu" && Get<Button>(flow, "_continue").interactable, "После выхода появляется Продолжить.");
                    Capture(flow, "MainMenuWithSave.png");
                    Require(GameSaveService.TryRead(out _expected, out _), "Сохранение доступно после выхода.");
                    flow.RequestNewGame(); Get<Button>(flow, "_dialogCancel").onClick.Invoke();
                    Require(GameSaveService.TryRead(out var unchanged, out _) && unchanged.money == _expected.money, "Отмена новой игры сохраняет прежнюю партию.");
                    // Идентификаторы старой сцены те же, но её мировые координаты выходят за пределы доски.
                    var legacy = JsonUtility.FromJson<CaravanSaveData>(JsonUtility.ToJson(_expected));
                    legacy.playerPosition += new Vector3(1000, 500, 600);
                    foreach (var trader in legacy.traders) trader.position += new Vector3(1000, 500, 600);
                    GameSaveService.Write(legacy);
                    flow.ContinueGame();
                    break;
                case 6:
                    if (flow.IsBusy) return;
                    Require(SceneManager.GetActiveScene().name == ApplicationFlow.GameSceneName, "Продолжить загружает сцену игры.");
                    var restored = GameManager.Instance.CaptureSave(flow.Catalog); restored.savedAt = _expected.savedAt;
                    File.WriteAllText(Path.GetFullPath("Library/GameMenuPreview/expected.json"), JsonUtility.ToJson(_expected, true));
                    File.WriteAllText(Path.GetFullPath("Library/GameMenuPreview/restored.json"), JsonUtility.ToJson(restored, true));
                    Require(JsonUtility.ToJson(restored) == JsonUtility.ToJson(_expected), "Полный снимок боя, каравана, рынков, карт, ИИ и истории должен совпадать после загрузки.");
                    Require(Get<Button>(GameSaveService.Find<BattleManager>().GetUIManager(), "endTurnButton").interactable,
                        "После восстановления броска доступна экранная кнопка завершения боя.");
                    GameManager.Instance.AdvancePhase();
                    _expected = GameManager.Instance.CaptureSave(flow.Catalog);
                    PauseMenuController.Instance.Pause(); PauseMenuController.Instance.RequestMainMenu(); PauseMenuController.Instance.ConfirmQuit();
                    break;
                case 7:
                    if (flow.IsBusy) return;
                    flow.ContinueGame();
                    break;
                case 8:
                    if (flow.IsBusy) return;
                    var post = GameManager.Instance.CaptureSave(flow.Catalog);
                    Require(post.phase == GameState.PostEncounter && post.money == _expected.money && post.hand.SequenceEqual(_expected.hand),
                        "Загрузка результата боя не должна повторно выдавать деньги или карту награды.");
                    PauseMenuController.Instance.Pause(); PauseMenuController.Instance.RequestMainMenu(); PauseMenuController.Instance.ConfirmQuit();
                    break;
                case 9:
                    if (flow.IsBusy) return;
                    flow.ContinueGame();
                    break;
                case 10:
                    if (flow.IsBusy) return;
                    GameManager.Instance.AdvancePhase();
                    Require(GameManager.Instance.State == GameState.WaitingForAI, "Началась очередь ИИ.");
                    PauseMenuController.Instance.Pause(); PauseMenuController.Instance.RequestMainMenu(); PauseMenuController.Instance.ConfirmQuit();
                    break;
                case 11:
                    if (flow.IsBusy) return;
                    Require(SceneManager.GetActiveScene().name == "MainMenu" && GameSaveService.TryRead(out var afterAI, out _) &&
                        afterAI.turn == 3 && afterAI.aiTurn == 2, "Выход во время ИИ дожидается целой очереди и сохраняет следующий ход.");
                    File.WriteAllText(CheckPath, "{broken save}"); flow.ShowMainMenu();
                    Require(!Get<Button>(flow, "_continue").interactable, "Повреждённое сохранение не разрешает продолжение.");
                    Debug.Log("GAME_MENU_PLAY_MODE_CHECKS_PASSED"); Stop(0); return;
            }
            _stage++; _nextFrame = Time.frameCount + 8;
        }
        catch (Exception exception) { Debug.LogException(exception); Stop(1); }
    }
    public static void CheckChoiceNavigation(SettingsMenu settings)
    {
        var before = JsonUtility.ToJson(Get<GameSettingsData>(settings, "_draft"));
        foreach (string name in new[] { "Полный экран", "Разрешение", "Качество" })
        {
            settings.Root.GetComponentsInChildren<Button>().Single(button => button.name == name + " — назад").onClick.Invoke();
            settings.Root.GetComponentsInChildren<Button>().Single(button => button.name == name + " — вперёд").onClick.Invoke();
            Require(JsonUtility.ToJson(Get<GameSettingsData>(settings, "_draft")) == before,
                "Выбор назад и вперёд должен возвращать исходное значение: " + name);
        }
        var resolutions = Get<Vector2Int[]>(settings, "_resolutions");
        foreach (var choice in new[] { (name: "Разрешение", count: resolutions.Length),
            (name: "Качество", count: QualitySettings.names.Length) })
        {
            // Полный круг в обратную сторону проверяет также переход через начало списка.
            for (int i = 0; i < choice.count; i++)
                settings.Root.GetComponentsInChildren<Button>().Single(button => button.name == choice.name + " — назад").onClick.Invoke();
            Require(JsonUtility.ToJson(Get<GameSettingsData>(settings, "_draft")) == before,
                "Обратное переключение должно замыкаться в круг: " + choice.name);
        }
    }

    public static void CheckCityHand(PauseMenuController pause)
    {
        var hand = HandManager.Instance;
        var panel = Get<Transform>(hand, "handTransform").gameObject;
        var pauseRect = (RectTransform)Get<Button>(pause, "_openButton").transform;
        Require(pauseRect.anchorMin == new Vector2(0, 1) && pauseRect.pivot == new Vector2(0, 1) &&
            pauseRect.anchoredPosition.x > 0 && pauseRect.anchoredPosition.y < 0,
            "Пауза должна находиться у левого верхнего края.");
        Require(!panel.activeSelf && !hand.IsCityHandOpen, "При въезде в город рука закрыта.");
        int count = hand.CardCount;
        var button = Get<Button>(pause, "_cityHandButton");
        button.onClick.Invoke();
        Require(panel.activeSelf && hand.IsCityHandOpen, "Кнопка карт должна открывать руку в городе.");
        hand.RefreshUI();
        Require(panel.activeSelf && hand.CardCount == count, "Обновление руки не закрывает её и не расходует карты.");
        pause.Pause();
        hand.ToggleCityHand();
        Require(!button.gameObject.activeSelf && panel.activeSelf && hand.IsCityHandOpen,
            "Пауза скрывает кнопку карт и блокирует переключение руки.");
        pause.Resume(); button.onClick.Invoke();
        Require(!panel.activeSelf && !hand.IsCityHandOpen && hand.CardCount == count,
            "Повторное нажатие скрывает руку, сохраняя карты.");
    }

    private static void PrepareCaravan()
    {
        var inventory = GameSaveService.Find<PlayerInventory>(); inventory.AddMoney(10000);
        var item = GameSaveService.Find<CityManager>().AllCities.SelectMany(city => city.RuntimeData.items).First(entry => entry.item != null).item;
        inventory.AddItem(item, 2, 73);
        var unit = AssetDatabase.FindAssets("t:UnitData").Select(guid => AssetDatabase.LoadAssetAtPath<UnitData>(AssetDatabase.GUIDToAssetPath(guid))).First();
        GameSaveService.Find<TeamSystem>().TryHireUnit(unit);
        var guild = AssetDatabase.FindAssets("t:GuildData").Select(guid => AssetDatabase.LoadAssetAtPath<GuildData>(AssetDatabase.GUIDToAssetPath(guid))).First();
        GameSaveService.Find<GuildSystem>().TryJoinGuild(guild);
        GameSaveService.Find<ShadowEffectManager>().ApplyTransientCard(ShadowEffectType.Attack, 4, 3);
        GameSaveService.Find<ShadowEffectManager>().ApplyTransientCard(ShadowEffectType.TeamStats, -20, 3);
        var market = GameSaveService.Find<CityManager>().AllCities[0].RuntimeData.items[0];
        market.stock += 17; market.currentBuyPrice += 12; market.UpdateDemand();
    }
    private static void DrawType<T>() where T : class, ICard
    {
        var flow = ApplicationFlow.Instance;
        var state = GameManager.Instance.CaptureSave(flow.Catalog);
        if (GameManager.Instance.State == GameState.InCity)
            GameManager.Instance.OnPathSelected(GameSaveService.Find<CityManager>().PlayerCurrentCity.Paths.First());
        for (int seed = 1; seed < 1000; seed++)
        {
            UnityEngine.Random.InitState(seed);
            var deck = new CardDeck(); deck.AddRange(state.deck.Select(flow.Catalog.Get<ICard>));
            if (!(deck.Draw() is T)) continue;
            UnityEngine.Random.InitState(seed); CardManager.Instance.DrawCard();
            Require(CardManager.Instance.CurrentCard is T, "Раскрыт ожидаемый тип карты."); return;
        }
        throw new InvalidOperationException("Не найден тип карты для проверки.");
    }
    private static void Capture(ApplicationFlow flow, string filename) =>
        MenuPreviewCapture.Save(Get<GameObject>(flow, "_canvasRoot").GetComponent<Canvas>(), filename);
    private static void RestoreInput()
    {
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        if (_originalInput != null) InputSystem.settings = _originalInput;
        if (_checkInput != null) UnityEngine.Object.DestroyImmediate(_checkInput);
        _keyboard = null; _originalInput = null; _checkInput = null;
    }
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode || !SessionState.GetBool(Running, false)) return;
        RestoreInput();
        if (!_stopping)
        {
            _stopping = true; _exitCode = 1;
            SessionState.SetBool(Stopping, true); SessionState.SetInt(ExitCode, 1);
        }
    }
    private static void Stop(int code)
    {
        RestoreInput();
        PauseMenuController.Instance?.Resume(); _stopping = true; _exitCode = code;
        SessionState.SetBool(Stopping, true); SessionState.SetInt(ExitCode, code); EditorApplication.isPlaying = false;
    }
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
