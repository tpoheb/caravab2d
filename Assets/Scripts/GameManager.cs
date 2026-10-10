using UnityEngine;
using UnityEngine.UI;

public partial class GameManager : MonoBehaviour
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
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Кнопка вытянуть карту")]
    [SerializeField] private Button drawCardButton;

    [Header("UI выбора кубика (Старая Карта)")]
    [Tooltip("Панель с кнопками 1–6 для выбора значения кубика атаки")]
    [SerializeField] private GameObject diceChoicePanel;

    [Header("UI выбора товара (Мистический Узел)")]
    [Tooltip("Панель выбора товара для удвоения")]
    [SerializeField] private GameObject doubleGoodsPanelPrefab;
    [SerializeField] private Transform uiRoot;

    public GameState State { get; private set; } = GameState.Idle;

    /// <summary>Текущий личный ход игрока. Увеличивается после завершения ходов ИИ.</summary>
    public int TurnNumber { get; private set; }

    private bool _pendingBattle;
    private int? _selectedDiceValue;
    private bool _awaitingDiceChoice;

    public HandCardData.PlayTiming CurrentHandTiming => State switch
    {
        GameState.InCity => HandCardData.PlayTiming.InCity,
        GameState.PreparingEncounter when cardManager.CurrentCard is BattleCardData
            => HandCardData.PlayTiming.BeforeBattle,
        GameState.PreparingEncounter when cardManager.CurrentCard is ShadowCardData
            => HandCardData.PlayTiming.BeforeEvent,
        GameState.InBattle => HandCardData.PlayTiming.DuringBattle,
        GameState.ResolvingEvent => HandCardData.PlayTiming.DuringEvent,
        GameState.PostEncounter when cardManager.CurrentCard is BattleCardData
            => HandCardData.PlayTiming.AfterBattle,
        GameState.PostEncounter when cardManager.CurrentCard is ShadowCardData
            => HandCardData.PlayTiming.AfterEvent,
        _ => HandCardData.PlayTiming.None,
    };

    public bool CanAdvancePhase => !PauseMenuController.IsPaused && !_awaitingDiceChoice &&
        (State == GameState.PreparingEncounter || State == GameState.ResolvingEvent ||
         State == GameState.PostEncounter ||
         (State == GameState.InBattle && battleManager.HasDiceRoll));

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
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
        if (diceSystem    != null) diceSystem.OnDiceRolled     += OnDiceRolled;
        if (playerToken   != null) playerToken.OnArrivedAtCity += OnArrivedAtCity;
        if (drawCardButton != null) drawCardButton.onClick.AddListener(OnDrawCardButtonPressed);
    }

    private void Unsubscribe()
    {
        if (diceSystem    != null) diceSystem.OnDiceRolled     -= OnDiceRolled;
        if (playerToken   != null) playerToken.OnArrivedAtCity -= OnArrivedAtCity;
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
        UpdatePhaseUI();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Ход
    // ─────────────────────────────────────────────────────────────────────

    private void StartTurn()
    {
        TurnNumber++;
        _pendingBattle = false;
        SetState(GameState.InCity);
    }

    private void OnDrawCardButtonPressed()
    {
        if (PauseMenuController.IsPaused) return;
        if (State != GameState.DrawingCard)
        {
            Debug.LogWarning($"[GameManager] DrawCard проигнорирован, состояние {State}");
            return;
        }

        // Блокируем повторное вытягивание до получения сигнала раскрытия.
        SetState(GameState.Idle);
        cardManager.DrawCard();
    }

    public void OnShadowCardRevealed()
    {
        _pendingBattle = false;
        BeginPreparation();
    }

    public void OnBattleCardRevealed()
    {
        _pendingBattle = true;
        BeginPreparation();
    }

    private void BeginPreparation()
    {
        _selectedDiceValue = null;
        _awaitingDiceChoice = false;
        SetState(GameState.PreparingEncounter);
    }

    public void OnBattleForceEnded()
    {
        _pendingBattle = false;
        CloseDiceChoice();
        SetState(GameState.PostEncounter);
        Debug.Log("[GameManager] Бой пропущен через EscapeBattle.");
    }

    public void OnCardCancelled()
    {
        _pendingBattle = false;
        CloseDiceChoice();
        SetState(GameState.PostEncounter);
        Debug.Log("[GameManager] Карта отменена или колода пуста.");
    }

    /// <summary>Общая кнопка продолжает текущую фазу; ход заканчивается только после постфазы.</summary>
    public void RequestEndTurn() => AdvancePhase();

    public void AdvancePhase()
    {
        if (!CanAdvancePhase)
        {
            Debug.LogWarning($"[GameManager] Продолжение фазы недоступно: {State}.");
            return;
        }

        switch (State)
        {
            case GameState.PreparingEncounter:
                if (_pendingBattle)
                {
                    SetState(GameState.InBattle);
                    if (_selectedDiceValue.HasValue) OnDiceRolled(_selectedDiceValue.Value);
                    else diceSystem.RollDice();
                }
                else SetState(GameState.ResolvingEvent);
                break;

            case GameState.InBattle:
                battleManager.FinalizeBattle();
                SetState(GameState.PostEncounter);
                break;

            case GameState.ResolvingEvent:
                cardManager.ResolveCurrentEvent();
                SetState(GameState.PostEncounter);
                break;

            case GameState.PostEncounter:
                EndPlayerTurn();
                break;
        }
    }

    private void EndPlayerTurn()
    {
        CompleteEncounter();
        FinishEndTurn();
    }

    private void CompleteEncounter()
    {
        // Блокировка действий до выплаты штрафа, тика эффектов и запуска ИИ.
        SetState(GameState.EndingTurn);
        if (_pendingBattle) battleManager.SettleBattle();
        _pendingBattle = false;
        handManager?.ClearEncounterEffects();
        CloseDiceChoice();
        shadowEffectManager?.ProcessTurn();
    }

    private void CloseDiceChoice()
    {
        _awaitingDiceChoice = false;
        _selectedDiceValue = null;
        if (diceChoicePanel != null) diceChoicePanel.SetActive(false);
    }

    private void FinishEndTurn()
    {
        cardManager.HideEventCard();

        var ui = GetUIManager();
        ui.ClearEventText();
        ui.ShowDiceButton(false);

        SetState(GameState.WaitingForAI);
        StartCoroutine(WaitForAIAndBeginPlayerTurn());
    }

    private System.Collections.IEnumerator WaitForAIAndBeginPlayerTurn()
    {
        if (AITurnManager.Instance != null)
            yield return AITurnManager.Instance.ProcessAITurnAndWait();

        while (PauseMenuController.IsPaused) yield return null;

        // Начинается следующий личный ход. Вход в город не завершает его:
        // после городских действий OnPathSelected приведёт на первую клетку.
        TurnNumber++;
        SetState(GameState.Moving);
        ContinueMovement();
        GameSaveService.TryAutosave();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Кубик
    // ─────────────────────────────────────────────────────────────────────

    public void OnDiceRolled(int value)
    {
        if (PauseMenuController.IsPaused) return;
        if (State != GameState.InBattle || !_pendingBattle || _awaitingDiceChoice) return;
        battleManager.ExecuteBattle(value);
        UpdatePhaseUI();
        handManager?.RefreshUI();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Карты руки — UI промпты
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Показывает панель выбора числа кубика (1–6).
    /// Кнопки в панели должны вызывать OnDiceChoiceSelected(int value).
    /// Активируется картой "Старая Карта".
    /// </summary>
    public void PromptDiceChoice()
    {
        if (!_pendingBattle || (State != GameState.PreparingEncounter && State != GameState.InBattle)) return;
        _awaitingDiceChoice = true;
        UpdatePhaseUI();
        handManager?.RefreshUI();
        if (diceChoicePanel == null)
        {
            Debug.LogWarning("[GameManager] diceChoicePanel не назначен в инспекторе. " +
                             "Автоматически выбираем максимум (6).");
            ApplyDiceChoice(6);
            return;
        }

        diceChoicePanel.SetActive(true);
        // Кнопки в diceChoicePanel настраиваются в инспекторе:
        // каждая кнопка вызывает GameManager.Instance.OnDiceChoiceSelected(N)
    }

    /// <summary>
    /// Вызывается кнопкой в diceChoicePanel.
    /// </summary>
    public void OnDiceChoiceSelected(int value)
    {
        if (PauseMenuController.IsPaused) return;
        if (!_awaitingDiceChoice) return;
        value = Mathf.Clamp(value, 1, 6);
        if (diceChoicePanel != null) diceChoicePanel.SetActive(false);
        ApplyDiceChoice(value);
    }

    private void ApplyDiceChoice(int value)
    {
        _awaitingDiceChoice = false;
        Debug.Log($"[GameManager] ChooseDice: выбрано {value}.");
        if (State == GameState.PreparingEncounter) _selectedDiceValue = value;
        else OnDiceRolled(value);
        UpdatePhaseUI();
        handManager?.RefreshUI();
    }

    public bool IsChoosingDice => _awaitingDiceChoice;

    /// <summary>
    /// Показывает UI выбора товара для удвоения (Мистический Узел).
    /// После выбора игрок вызывает OnDoubleGoodsSelected(Item item).
    /// </summary>
    public void PromptDoubleGoods()
    {
        if (playerInventory == null || playerInventory.Items.Count == 0)
        {
            Debug.LogWarning("[GameManager] PromptDoubleGoods: инвентарь пуст, удваивать нечего.");
            return;
        }

        if (doubleGoodsPanelPrefab == null)
        {
            // Заглушка: удваиваем первый попавшийся товар
            Debug.LogWarning("[GameManager] doubleGoodsPanelPrefab не назначен. " +
                             "Удваиваем первый товар в инвентаре.");
            playerInventory.DoubleGoods(playerInventory.Items[0].item);
            return;
        }

        // TODO: заменить на полноценный UI выбора товара (GoodsSelectorUI)
        // Пока удваиваем случайный товар из инвентаря
        var items = playerInventory.Items;
        var chosen = items[Random.Range(0, items.Count)].item;
        playerInventory.DoubleGoods(chosen);
        Debug.Log($"[GameManager] DoubleGoods: удвоен {chosen.name} (UI выбора не назначен).");
    }

    /// <summary>
    /// Коллбэк для будущего UI выбора товара.
    /// Вызвать из GoodsSelectorUI когда он будет готов.
    /// </summary>
    public void OnDoubleGoodsSelected(Item item)
    {
        if (PauseMenuController.IsPaused) return;
        playerInventory.DoubleGoods(item);
        Debug.Log($"[GameManager] DoubleGoods: выбран товар {item.name}.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Движение
    // ─────────────────────────────────────────────────────────────────────

    public void OnPathSelected(PathCellInitializer path)
    {
        if (PauseMenuController.IsPaused) return;
        if (State != GameState.InCity)
        {
            Debug.LogWarning($"[GameManager] Путь проигнорирован, состояние {State}");
            return;
        }
        if (path == null) { Debug.LogError("[GameManager] Путь null!"); return; }

        playerToken.StartPath(path);
        SetState(GameState.Moving);
        ContinueMovement();
    }

    public void ContinueMovement()
    {
        if (PauseMenuController.IsPaused) return;
        if (State != GameState.Moving)
        {
            Debug.LogWarning($"[GameManager] ContinueMovement прерван, состояние {State}");
            return;
        }

        bool reachedCity = playerToken.AdvanceToken();

        if (reachedCity)
            SetState(GameState.InCity);
        else
            SetState(GameState.DrawingCard);
    }

    private void OnArrivedAtCity(City city)
    {
        Debug.Log($"[GameManager] Прибыли в {city.CityName}.");
        SetState(GameState.InCity);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Обратная совместимость
    // ─────────────────────────────────────────────────────────────────────

    public void CompleteEventPhase()
    {
        if (State == GameState.ResolvingEvent) AdvancePhase();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateDrawCardButton()
    {
        if (drawCardButton != null)
            drawCardButton.gameObject.SetActive(State == GameState.DrawingCard);
    }

    private void UpdatePhaseUI()
    {
        var ui = battleManager != null ? GetUIManager() : null;
        ui?.ShowPhase(State, cardManager != null && cardManager.CurrentCard is BattleCardData, CanAdvancePhase);
    }

    private BattleUIManager GetUIManager() => battleManager.GetUIManager();

    private void ValidateReferences()
    {
        if (playerToken    == null) Debug.LogError("[GameManager] PlayerToken не назначен");
        if (diceSystem     == null) Debug.LogError("[GameManager] DiceSystem не назначен");
        if (battleManager  == null) Debug.LogError("[GameManager] BattleManager не назначен");
        if (cardManager    == null) Debug.LogError("[GameManager] CardManager не назначен");
        if (drawCardButton == null) Debug.LogWarning("[GameManager] DrawCardButton не назначен");
        if (playerInventory == null) Debug.LogWarning("[GameManager] PlayerInventory не назначен (нужен для DoubleGoods)");
    }
}
