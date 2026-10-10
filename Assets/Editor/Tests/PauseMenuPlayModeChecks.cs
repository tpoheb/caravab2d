using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Batch-проверка настоящей сцены, ввода Esc, UI и выхода из Play Mode.</summary>
[InitializeOnLoad]
public static class PauseMenuPlayModeChecks
{
    private const string RunningKey = "PauseMenuPlayModeChecks.Running";
    private const string DeadlineKey = "PauseMenuPlayModeChecks.Deadline";
    private const string StoppingKey = "PauseMenuPlayModeChecks.Stopping";
    private const string ExitCodeKey = "PauseMenuPlayModeChecks.ExitCode";
    private static int _stage;
    private static int _nextFrame;
    private static Keyboard _keyboard;
    private static InputSettings _originalInputSettings;
    private static InputSettings _checkInputSettings;
    private static PauseMenuController _pause;
    private static GameManager _game;
    private static PlayerInventory _inventory;
    private static GameState _initialState;
    private static int _initialTurn;
    private static int _initialMoney;
    private static bool _stopping;
    private static int _exitCode;

    static PauseMenuPlayModeChecks()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        _stopping = SessionState.GetBool(StoppingKey, false);
        _exitCode = SessionState.GetInt(ExitCodeKey, 1);
        EditorApplication.update += Tick;
    }

    // Запускать без -quit: проверка сама завершает Editor после выхода из Play Mode.
    public static void RunBatch()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Эта проверка запускается в отдельном batchmode Editor.");
        EditorSceneManager.OpenScene("Assets/Scenes/MapPrototipe.unity");
        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(StoppingKey, false);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + 300f);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        try
        {
            if (_stopping)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(RunningKey, false);
                    EditorApplication.Exit(_exitCode);
                }
                return;
            }
            Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(DeadlineKey, 0f),
                "Превышено время ожидания Play Mode.");
            if (!Application.isPlaying || Time.frameCount < Math.Max(5, _nextFrame)) return;

            switch (_stage)
            {
                case 0:
                    _pause = PauseMenuController.Instance;
                    _game = GameManager.Instance;
                    Require(_pause != null && _game != null, "Сцена должна содержать меню и GameManager.");
                    _inventory = Get<PlayerInventory>(_game, "playerInventory");
                    _initialState = _game.State;
                    _initialTurn = _game.TurnNumber;
                    _initialMoney = _inventory.Money;
                    _originalInputSettings = InputSystem.settings;
                    _checkInputSettings = UnityEngine.Object.Instantiate(_originalInputSettings);
                    _checkInputSettings.editorInputBehaviorInPlayMode =
                        InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    _checkInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    _checkInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                    InputSystem.settings = _checkInputSettings;
                    _keyboard = InputSystem.AddDevice<Keyboard>();
                    Escape(true);
                    break;
                case 1:
                    Require(PauseMenuController.IsPaused && Time.timeScale == 0f && AudioListener.pause,
                        $"Ввод Esc должен открыть меню: пауза={PauseMenuController.IsPaused}, " +
                        $"клавиатура включена={_keyboard.enabled}, текущая={Keyboard.current == _keyboard}, " +
                        $"Esc={_keyboard.escapeKey.isPressed}.");
                    Require(_game.State == _initialState && _game.TurnNumber == _initialTurn &&
                        _inventory.Money == _initialMoney, "Пауза должна сохранить состояние каравана.");
                    CheckRaycastBlock();
                    Capture("PauseMenu.png");
                    Escape(false);
                    break;
                case 2:
                    Get<GameObject>(_pause, "_menuPanel").transform.Find("Выйти из игры")
                        .GetComponent<Button>().onClick.Invoke();
                    break;
                case 3:
                    Require(Get<GameObject>(_pause, "_quitPanel").activeSelf,
                        "Кнопка выхода должна открыть подтверждение.");
                    Capture("PauseMenuQuit.png");
                    Escape(true);
                    break;
                case 4:
                    Require(PauseMenuController.IsPaused && !Get<GameObject>(_pause, "_quitPanel").activeSelf,
                        "Esc должен отменить выход, сохраняя паузу.");
                    Escape(false);
                    break;
                case 5:
                    Escape(true);
                    break;
                case 6:
                    Require(!PauseMenuController.IsPaused && Time.timeScale == 1f && !AudioListener.pause,
                        "Повторный Esc должен продолжить игру.");
                    Require(_game.State == _initialState && _game.TurnNumber == _initialTurn &&
                        _inventory.Money == _initialMoney, "Продолжение не должно расходовать личный ход.");
                    Escape(false);
                    Get<Button>(_pause, "_openButton").onClick.Invoke();
                    Get<Button>(_pause, "_resumeButton").onClick.Invoke();
                    Require(!PauseMenuController.IsPaused, "Экранные кнопки должны открывать и закрывать паузу.");
                    Get<Button>(_pause, "_openButton").onClick.Invoke();
                    _pause.RequestQuit();
                    _pause.ConfirmQuit();
                    Require(!PauseMenuController.IsPaused && Time.timeScale == 1f,
                        "Подтверждённый выход должен восстановить время перед остановкой Play Mode.");
                    Debug.Log("PAUSE_MENU_PLAYMODE_CHECKS_PASSED");
                    Stop(0);
                    return;
            }
            _stage++;
            _nextFrame = Time.frameCount + 5;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Stop(1);
        }
    }

    private static void CheckRaycastBlock()
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width - 5f, Screen.height - 5f)
        }, hits);
        Require(hits.Count > 0 && hits[0].gameObject == Get<GameObject>(_pause, "_overlay"),
            "Затемнение должно перехватывать клики вне меню над игровыми окнами.");
    }

    private static void Escape(bool pressed) => InputSystem.QueueStateEvent(_keyboard,
        pressed ? new KeyboardState(Key.Escape) : new KeyboardState());

    private static void Capture(string filename)
    {
        string directory = Path.GetFullPath("Library/PauseMenuPreview");
        Directory.CreateDirectory(directory);
        // Batchmode не рисует Game View. Рендерим настоящий Canvas отдельной камерой URP.
        var canvas = Get<GameObject>(_pause, "_canvasRoot").GetComponent<Canvas>();
        var transforms = canvas.GetComponentsInChildren<Transform>(true);
        var layers = new int[transforms.Length];
        var cameraRoot = new GameObject("PauseMenuChecks — PreviewCamera", typeof(Camera));
        var camera = cameraRoot.GetComponent<Camera>();
        var target = new RenderTexture(1600, 900, 24);
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        var previousCamera = canvas.worldCamera;
        float previousPlaneDistance = canvas.planeDistance;
        var scaler = canvas.GetComponent<CanvasScaler>();
        bool scalerEnabled = scaler.enabled;
        float previousScale = canvas.scaleFactor;
        try
        {
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.10f, 0.08f);
            camera.cullingMask = 1 << 31;
            camera.transform.position = new Vector3(0f, 0f, -1000f);
            camera.targetTexture = target;
            for (int i = 0; i < transforms.Length; i++)
            {
                layers[i] = transforms[i].gameObject.layer;
                transforms[i].gameObject.layer = 31;
            }
            scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.scaleFactor = 1600f / 1920f;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest
            {
                destination = target
            });
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(directory, filename), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousTarget;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousPlaneDistance;
            canvas.scaleFactor = previousScale;
            scaler.enabled = scalerEnabled;
            for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
            UnityEngine.Object.DestroyImmediate(cameraRoot);
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void Stop(int exitCode)
    {
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        if (_originalInputSettings != null) InputSystem.settings = _originalInputSettings;
        if (_checkInputSettings != null) UnityEngine.Object.DestroyImmediate(_checkInputSettings);
        if (_pause != null) _pause.Resume();
        SessionState.SetBool(StoppingKey, true);
        SessionState.SetInt(ExitCodeKey, exitCode);
        _stopping = true;
        _exitCode = exitCode;
        EditorApplication.isPlaying = false;
    }

    private static T Get<T>(object target, string field) => (T)target.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
