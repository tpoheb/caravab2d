using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Системы")]
    [SerializeField] private PlayerToken playerToken;
    [SerializeField] private DiceSystem diceSystem;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private HandManager handManager;
    [SerializeField] private PlayerStats playerStats;

    public GameState State { get; private set; } = GameState.Idle;

    // Флаг: текущий ход содержал бой, который ещё не финализирован
    private bool _pendingBattle = false;

    // --------------------
    // ЖИЗНЕННЫЙ ЦИКЛ
    // --------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ValidateReferences();
    }

    private void OnEnable()  => Subscribe();
    private void OnDisable() => Unsubscribe();

    private void Start()
    {
        battleManager.GetUIManager().ShowEndTurnButton(false);
        playerStats.Initialize();
        StartTurn();
    }

    // --------------------
    // ПОДПИСКИ
    // --------------------

    private void Subscribe()
    {
        if (diceSystem != null)    diceSystem.OnDiceRolled    += OnDiceRolled;
        if (playerToken != null)   playerToken.OnArrivedAtCity += OnArrivedAtCity;
    }

    private void Unsubscribe()
    {
        if (diceSystem != null)    diceSystem.OnDiceRolled    -= OnDiceRolled;
        if (playerToken != null)   playerToken.OnArrivedAtCity -= OnArrivedAtCity;
    }

    // --------------------
    // FSM
    // --------------------

    private void SetState(GameState newState)
    {
        Debug.Log($"FSM: {State} -> {newState}");
        State = newState;
        HandManager.Instance?.RefreshUI();
    }

    // --------------------
    // ХОД ИГРОКА
    // --------------------

    private void StartTurn()
    {
        _pendingBattle = false;
        SetState(GameState.InCity);
    }

    /// <summary>
    /// Единственная обязательная кнопка за ход.
    /// Если был бой — финализирует его (деньги меняются здесь, один раз).
    /// Затем переходит к следующей клетке.
    /// </summary>
    public void RequestEndTurn()
    {
        if (State != GameState.ResolvingEvent)
        {
            Debug.LogWarning($"GameManager: RequestEndTurn проигнорирован, состояние {State}");
            return;
        }

        // Финализируем бой если он был в этом ходу
        if (_pendingBattle)
        {
            battleManager.FinalizeBattle();
            _pendingBattle = false;
        }

        var ui = battleManager.GetUIManager();
        ui.ShowEndTurnButton(false);
        ui.ShowDiceButton(true);  // Восстанавливаем кубик для следующей клетки
        ui.ClearEventText();

        SetState(GameState.Moving);
        ContinueMovement();
    }

    // --------------------
    // ГОРОД / ВЫБОР ПУТИ
    // --------------------

    private void OnArrivedAtCity(City city)
    {
        Debug.Log($"GameManager: прибыли в {city.CityName}");
        SetState(GameState.InCity);
    }

    public void OnPathSelected(PathCellInitializer path)
    {
        if (State != GameState.InCity)
        {
            Debug.LogWarning($"GameManager: путь проигнорирован, состояние {State}");
            return;
        }
        if (path == null) { Debug.LogError("GameManager: путь null!"); return; }

        playerToken.StartPath(path);
        SetState(GameState.Moving);
        ContinueMovement();
    }

    // --------------------
    // ДВИЖЕНИЕ
    // --------------------

    public void ContinueMovement()
    {
        if (State != GameState.Moving)
        {
            Debug.LogWarning($"GameManager: ContinueMovement прерван, состояние {State}");
            return;
        }

        bool reachedCity = playerToken.AdvanceToken();

        if (reachedCity)
        {
            SetState(GameState.InCity);
        }
        else
        {
            // Встали на клетку — автобросок
            SetState(GameState.RollingDice);
            diceSystem.RollDice();
        }
    }

    // --------------------
    // КУБИК
    // --------------------

    public void OnDiceRolled(int value, DiceEventType type)
    {
        // Бросок в бою (первичный или переброс картой)
        if (State == GameState.InBattle || State == GameState.ResolvingEvent)
        {
            battleManager.ExecuteBattle(value);
            // После любого броска в бою переходим в ResolvingEvent —
            // именно это состояние разрешает RequestEndTurn
            OnEventResolved();
            return;
        }

        ResolveDiceEvent(type, value);
    }

    // --------------------
    // СОБЫТИЯ
    // --------------------

    private void ResolveDiceEvent(DiceEventType type, int diceValue)
    {
        battleManager.GetUIManager().DisplayEventInfo(type, diceValue);

        switch (type)
        {
            case DiceEventType.Battle:
                StartBattlePhase();
                break;

            case DiceEventType.ShadowInfluence:
                cardManager.DrawCard();
                OnEventResolved();
                break;

            case DiceEventType.PeacefulPass:
                OnEventResolved();
                break;
        }
    }

    private void StartBattlePhase()
    {
        _pendingBattle = true;
        SetState(GameState.InBattle);

        BattleCardData card = cardManager.GetRandomBattleCard();
        battleManager.PrepareBattle(card);

        // Автобросок сразу после подготовки
        diceSystem.RollDice();
    }

    private void OnEventResolved()
    {
        SetState(GameState.ResolvingEvent);
        var ui = battleManager.GetUIManager();
        ui.ShowEndTurnButton(true);
        ui.ShowDiceButton(false); // Кубик недоступен пока ход не завершён
    }

    // Оставлен для обратной совместимости с CardManager
    public void CompleteEventPhase()
    {
        if (State != GameState.ResolvingEvent)
        {
            Debug.LogWarning($"GameManager: CompleteEventPhase в состоянии {State}");
            return;
        }
        SetState(GameState.Moving);
        ContinueMovement();
    }

    // --------------------
    // ВАЛИДАЦИЯ
    // --------------------

    private void ValidateReferences()
    {
        if (playerToken == null)   Debug.LogError("GameManager: PlayerToken не назначен");
        if (diceSystem == null)    Debug.LogError("GameManager: DiceSystem не назначен");
        if (battleManager == null) Debug.LogError("GameManager: BattleManager не назначен");
        if (handManager == null)   Debug.LogError("GameManager: HandManager не назначен");
    }
}