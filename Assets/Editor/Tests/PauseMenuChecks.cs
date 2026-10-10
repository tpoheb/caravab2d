using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Проверяет паузу и исполнение ИИ настоящими игровыми классами.</summary>
public static class PauseMenuChecks
{
    [MenuItem("Tools/Проверки/Меню паузы")]
    public static void Run()
    {
        if (Application.isPlaying || PauseMenuController.Instance != null)
            throw new InvalidOperationException("Запустите проверку вне Play Mode, без активного меню паузы.");

        var objects = new List<UnityEngine.Object>();
        float timeScale = Time.timeScale;
        bool audioPaused = AudioListener.pause;
        var oldEventSystem = EventSystem.current;
        EventSystem checkEventSystem = null;
        try
        {
            var eventRoot = new GameObject("PauseMenuChecks — EventSystem", typeof(EventSystem));
            objects.Add(eventRoot);
            checkEventSystem = eventRoot.GetComponent<EventSystem>();
            Invoke(checkEventSystem, "OnEnable");
            EventSystem.current = checkEventSystem;
            var previousButton = new GameObject("Игровая кнопка", typeof(RectTransform), typeof(Button));
            objects.Add(previousButton);
            EventSystem.current.SetSelectedGameObject(previousButton);

            var pauseRoot = new GameObject("PauseMenuChecks");
            objects.Add(pauseRoot);
            var pause = pauseRoot.AddComponent<PauseMenuController>();
            Set(pause, "_font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Bricemont SDF.asset"));
            // Awake не вызывается автоматически в Edit Mode.
            Invoke(pause, "Awake");
            Time.timeScale = 0.65f;
            AudioListener.pause = false;
            pause.Pause();
            pause.Pause();
            Require(PauseMenuController.IsPaused && Time.timeScale == 0f && AudioListener.pause,
                "Пауза должна остановить время и звук, повторный вызов не заменяет сохранённые значения.");
            Require(EventSystem.current.currentSelectedGameObject != previousButton,
                "Фокус должен уйти с игровой кнопки в меню.");
            Require(Get<GameObject>(pause, "_overlay").activeSelf &&
                Get<GameObject>(pause, "_menuPanel").activeSelf, "Пауза должна показать меню.");
            Require(Get<Button>(pause, "_resumeButton").navigation.mode == Navigation.Mode.Explicit,
                "Навигация меню не должна переходить к игровым кнопкам.");

            pause.RequestQuit();
            Require(Get<GameObject>(pause, "_quitPanel").activeSelf && PauseMenuController.IsPaused,
                "Выход должен требовать подтверждения, сохраняя паузу.");
            pause.TogglePause();
            Require(!Get<GameObject>(pause, "_quitPanel").activeSelf && PauseMenuController.IsPaused,
                "Esc в подтверждении отменяет выход, не снимая паузу.");
            pause.TogglePause();
            Require(!PauseMenuController.IsPaused && Time.timeScale == 0.65f && !AudioListener.pause &&
                EventSystem.current.currentSelectedGameObject == previousButton,
                "Продолжение должно восстановить скорость, звук и прежний фокус.");

            var gameRoot = new GameObject("PauseMenuChecks — GameManager");
            gameRoot.SetActive(false);
            objects.Add(gameRoot);
            var game = gameRoot.AddComponent<GameManager>();
            Set(game, "<State>k__BackingField", GameState.ResolvingEvent);
            pause.Pause();
            Require(!game.CanAdvancePhase, "На паузе нельзя подтвердить событие.");
            game.AdvancePhase();
            Require(game.State == GameState.ResolvingEvent, "Пауза не должна менять фазу встречи.");
            var playerTurn = (IEnumerator)Invoke(game, "WaitForAIAndBeginPlayerTurn");
            Require(playerTurn.MoveNext() && game.TurnNumber == 0,
                "Ожидание ИИ не должно начинать следующий личный ход на паузе.");
            (playerTurn as IDisposable)?.Dispose();
            pause.Resume();

            CheckAIWait(pause, objects);

            // Уже выключенный звук также должен восстановиться без изменений.
            AudioListener.pause = true;
            pause.Pause();
            Invoke(pause, "OnDisable");
            Require(!PauseMenuController.IsPaused && Time.timeScale == 0.65f && AudioListener.pause,
                "Отключение компонента должно снять собственную паузу и восстановить исходный звук.");
            Debug.Log("PAUSE_MENU_CHECKS_PASSED");
        }
        finally
        {
            if (checkEventSystem != null) Invoke(checkEventSystem, "OnDisable");
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            if (oldEventSystem != null) EventSystem.current = oldEventSystem;
            Time.timeScale = timeScale;
            AudioListener.pause = audioPaused;
        }
    }

    public static void RunAll()
    {
        Run();
        CardSystemChecks.Run();
    }

    private static void CheckAIWait(PauseMenuController pause, List<UnityEngine.Object> objects)
    {
        var root = new GameObject("PauseMenuChecks — WorldEconomy");
        root.SetActive(false);
        objects.Add(root);
        var city = root.AddComponent<City>();
        var source = ScriptableObject.CreateInstance<CityData>();
        objects.Add(source);
        Set(city, "sourceData", source);
        city.InitRuntimeData();
        objects.Add(city.RuntimeData);
        var economy = root.AddComponent<WorldEconomy>();
        Set(economy, "cityBindings", new List<WorldEconomy.CityBinding>
        {
            new WorldEconomy.CityBinding { city = city, cityData = source }
        });

        using var trader = new WaitingTrader(city);
        var queue = new TurnQueue(new List<ITrader> { trader }, economy);
        var routine = queue.ProcessTurnCoroutine();
        pause.Pause();
        Require(routine.MoveNext() && queue.TurnNumber == 0,
            "Новая очередь ИИ должна ждать снятия паузы.");
        pause.Resume();
        Require(routine.MoveNext(), "Очередь должна запустить фоновое планирование.");
        Require(trader.Started.Wait(2000), "Планирование должно начаться в фоновом потоке.");
        pause.Pause();
        trader.Release.Set();
        Require(trader.Completed.Wait(2000), "Фоновое планирование может завершиться во время паузы.");
        for (int i = 0; i < 5; i++)
            Require(routine.MoveNext() && !trader.Traded && !trader.Moved,
                "Готовый план не должен менять рынок или положение каравана на паузе.");
        pause.Resume();
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
                while (nested.MoveNext()) { }
            Require(DateTime.UtcNow < deadline, "Очередь не завершилась после снятия паузы.");
            Thread.Sleep(1);
        }
        Require(trader.Traded && trader.Moved && queue.TurnNumber == 1,
            "После продолжения готовый план должен исполниться в том же ходе.");
    }

    private sealed class WaitingTrader : ITrader, IDisposable
    {
        public readonly ManualResetEventSlim Started = new ManualResetEventSlim();
        public readonly ManualResetEventSlim Release = new ManualResetEventSlim();
        public readonly ManualResetEventSlim Completed = new ManualResetEventSlim();
        public bool Traded;
        public bool Moved;
        public string DisplayName => "Проверочный караван";
        public int Initiative => 0;
        public int Gold => 100;
        public City CurrentCity { get; }
        public event Action<ITrader, City> OnArrivedAtCity { add { } remove { } }
        public event Action<ITrader, PathCellInitializer> OnPathBlocked { add { } remove { } }

        public WaitingTrader(City city) => CurrentCity = city;
        public TurnIntent PlanTurn(GameSnapshot snapshot)
        {
            Started.Set();
            Release.Wait(2000);
            Completed.Set();
            return new TurnIntent { Trader = this };
        }
        public void ExecuteTrade(TurnIntent intent, WorldEconomy economy) => Traded = true;
        public IEnumerator ExecuteMovement(TurnIntent intent)
        {
            Moved = true;
            yield break;
        }
        public void Dispose()
        {
            Release.Set();
            // При ошибке проверки дожидаемся выхода рабочего потока перед освобождением событий.
            if (Started.IsSet) Completed.Wait(2000);
            Started.Dispose();
            Release.Dispose();
            Completed.Dispose();
        }
    }

    private static object Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
