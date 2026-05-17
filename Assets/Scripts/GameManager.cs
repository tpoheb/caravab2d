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
    [SerializeField] private ShadowEffectManager shadowEffectManager;

    [Header("Кнопка вытянуть карту")]
    [SerializeField] private Button drawCardButton;

    public GameState State { get; private set; } = GameState.Idle;

    private bool _pendingBattle = false;

    // ─────────────────────────────────────────────────────────────────────
    // Жизненный цикл
    // ─────────────────────────────────────────────────────────────────────

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
        GetUIManager().ShowEndTurnButton(false);
        playerStats.Initialize();
        StartTurn();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Подписки
    // ─────────────────────────────────────────────────────────────────────

    private void Subscribe()
    {
        if (diceSystem   != null) diceSystem.OnDiceRolled     += OnDiceRolled;
        if (playerToken  != null) playerToken.OnArrivedAtCity += OnArrivedAtCity;
        if (drawCardButton != null) drawCardButton.onClick.AddListener(OnDrawCardButtonPressed);
    }

    private void Unsubscribe()
    {
        if (diceSystem   != null) diceSystem.OnDiceRolled     -= OnDiceRolled;
        if (playerToken  != null) playerToken.OnArrivedAtCity -= OnArrivedAtCity;
        if (drawCardButton != null) drawCardButton.onClick.RemoveListener(OnDrawCardButtonPressed);
    }

    // ─────────────────────────────────────────────────────────────────────
    // FSM
    // ─────────────────────────────────────────────────────────────────────

    private void SetState(GameState newState)
    {
        Debug.Log($"FSM: {State} -> {newState}");
        State = newState;
        HandManager.Instance?.RefreshUI();
        UpdateDrawCardButton();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Ход
    // ─────────────────────────────────────────────────────────────────────

    private void StartTurn()
    {
        _pendingBattle = false;
        SetState(GameState.InCity);
    }

    /// <summary>
    /// Нажатие кнопки "Вытянуть карту".
    /// Доступно только в состоянии DrawingCard.
    /// </summary>
    private void OnDrawCardButtonPressed()
    {
        if (State != GameState.DrawingCard)
        {
            Debug.LogWarning($"GameManager: DrawCard проигнорирован, состояние {State}");
            return;
        }

        SetState(GameState.ResolvingEvent);   // блокируем повторное нажатие
        cardManager.DrawCard();
    }

    /// <summary>
    /// Вызывается из CardManager после открытия карты тени.
    /// </summary>
    public void OnShadowCardRevealed()
    {
        GetUIManager().ShowEndTurnButton(true);
        GetUIManager().ShowDiceButton(false);
    }

    /// <summary>
    /// Вызывается из CardManager после открытия карты битвы.
    /// BattleManager.PrepareBattle уже вызван к этому моменту.
    /// Запускаем автобросок кубика.
    /// </summary>
    public void OnBattleCardRevealed()
    {
        _pendingBattle = true;
        SetState(GameState.InBattle);
        diceSystem.RollDice();
    }

    /// <summary>
    /// Завершение хода — единственная обязательная кнопка.
    /// </summary>
    public void RequestEndTurn()
    {
        if (State != GameState.ResolvingEvent && State != GameState.InBattle)
        {
            Debug.LogWarning($"GameManager: RequestEndTurn проигнорирован, состояние {State}");
            return;
        }

        if (_pendingBattle)
        {
            battleManager.FinalizeBattle();
            _pendingBattle = false;
        }

        shadowEffectManager?.ProcessTurn();
        cardManager.HideEventCard();

        var ui = GetUIManager();
        ui.ShowEndTurnButton(false);
        ui.ShowDiceButton(true);

        SetState(GameState.Moving);
        ContinueMovement();

        AITurnManager.Instance?.ProcessAITurn();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Кубик (только для битвы)
    // ─────────────────────────────────────────────────────────────────────

    public void OnDiceRolled(int value, DiceEventType type)
    {
        if (State == GameState.InBattle || State == GameState.ResolvingEvent)
        {
            battleManager.ExecuteBattle(value);
            SetState(GameState.ResolvingEvent);
            GetUIManager().ShowEndTurnButton(true);
            GetUIManager().ShowDiceButton(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Движение
    // ─────────────────────────────────────────────────────────────────────

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

    public void ContinueMovement()
    {
        if (State != GameState.Moving)
        {
            Debug.LogWarning($"GameManager: ContinueMovement прерван, состояние {State}");
            return;
        }

        bool reachedCity = playerToken.AdvanceToken();

        if (reachedCity)
            SetState(GameState.InCity);
        else
            SetState(GameState.DrawingCard);    // игрок должен вытянуть карту
    }

    private void OnArrivedAtCity(City city)
    {
        Debug.Log($"GameManager: прибыли в {city.CityName}");
        SetState(GameState.InCity);
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI кнопки вытянуть карту
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateDrawCardButton()
    {
        if (drawCardButton != null)
            drawCardButton.gameObject.SetActive(State == GameState.DrawingCard);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Обратная совместимость
    // ─────────────────────────────────────────────────────────────────────

    public void CompleteEventPhase()
    {
        if (State != GameState.ResolvingEvent) return;
        SetState(GameState.Moving);
        ContinueMovement();
    }

    private BattleUIManager GetUIManager() => battleManager.GetUIManager();

    // ─────────────────────────────────────────────────────────────────────
    // Валидация
    // ─────────────────────────────────────────────────────────────────────

    private void ValidateReferences()
    {
        if (playerToken    == null) Debug.LogError("GameManager: PlayerToken не назначен");
        if (diceSystem     == null) Debug.LogError("GameManager: DiceSystem не назначен");
        if (battleManager  == null) Debug.LogError("GameManager: BattleManager не назначен");
        if (cardManager    == null) Debug.LogError("GameManager: CardManager не назначен");
        if (drawCardButton == null) Debug.LogWarning("GameManager: DrawCardButton не назначен");
    }
}