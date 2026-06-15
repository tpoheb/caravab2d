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
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Кнопка вытянуть карту")]
    [SerializeField] private Button drawCardButton;

    [Header("UI выбора кубика (Старая Карта)")]
    [Tooltip("Панель с кнопками 1–6 для выбора значения кубика пути")]
    [SerializeField] private GameObject diceChoicePanel;

    [Header("UI выбора товара (Мистический Узел)")]
    [Tooltip("Панель выбора товара для удвоения")]
    [SerializeField] private GameObject doubleGoodsPanelPrefab;
    [SerializeField] private Transform uiRoot;

    public GameState State { get; private set; } = GameState.Idle;

    private bool _pendingBattle = false;

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
    }

    // ─────────────────────────────────────────────────────────────────────
    // Ход
    // ─────────────────────────────────────────────────────────────────────

    private void StartTurn()
    {
        _pendingBattle = false;
        SetState(GameState.InCity);
    }

    private void OnDrawCardButtonPressed()
    {
        if (State != GameState.DrawingCard)
        {
            Debug.LogWarning($"[GameManager] DrawCard проигнорирован, состояние {State}");
            return;
        }

        SetState(GameState.ResolvingEvent);
        cardManager.DrawCard();
    }

    public void OnShadowCardRevealed()
    {
        GetUIManager().ShowEndTurnButton(true);
        GetUIManager().ShowDiceButton(false);
    }

    public void OnBattleCardRevealed()
    {
        _pendingBattle = true;
        SetState(GameState.InBattle);
        diceSystem.RollDice();
    }

    /// <summary>
    /// Вызывается BattleManager когда бой завершён принудительно (Дымовая завеса).
    /// Переводим в ResolvingEvent — игрок нажимает "Завершить ход" как обычно.
    /// </summary>
    public void OnBattleForceEnded()
    {
        _pendingBattle = false;
        SetState(GameState.ResolvingEvent);
        GetUIManager().ShowEndTurnButton(true);
        GetUIManager().ShowDiceButton(false);
        Debug.Log("[GameManager] Бой пропущен через EscapeBattle.");
    }

    /// <summary>
    /// Вызывается CardManager когда карта была отменена Странным Амулетом.
    /// Переходим сразу к завершению хода.
    /// </summary>
    public void OnCardCancelled()
    {
        SetState(GameState.ResolvingEvent);
        GetUIManager().ShowEndTurnButton(true);
        GetUIManager().ShowDiceButton(false);
        Debug.Log("[GameManager] Карта отменена амулетом.");
    }

    public void RequestEndTurn()
    {
        if (State != GameState.ResolvingEvent && State != GameState.InBattle)
        {
            Debug.LogWarning($"[GameManager] RequestEndTurn проигнорирован, состояние {State}");
            return;
        }

        GetUIManager().ShowEndTurnButton(false);

        if (_pendingBattle)
        {
            battleManager.FinalizeBattle();
            _pendingBattle = false;
            StartCoroutine(EndTurnAfterDelay(1.5f));
            return;
        }

        shadowEffectManager?.ProcessTurn();
        FinishEndTurn();
    }

    private System.Collections.IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        shadowEffectManager?.ProcessTurn();
        FinishEndTurn();
    }

    private void FinishEndTurn()
    {
        cardManager.HideEventCard();

        var ui = GetUIManager();
        ui.ClearEventText();
        ui.ShowDiceButton(true);

        SetState(GameState.Moving);
        ContinueMovement();

        AITurnManager.Instance?.ProcessAITurn();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Кубик
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
    // Карты руки — UI промпты
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Показывает панель выбора числа кубика (1–6).
    /// Кнопки в панели должны вызывать OnDiceChoiceSelected(int value).
    /// Активируется картой "Старая Карта".
    /// </summary>
    public void PromptDiceChoice()
    {
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
        value = Mathf.Clamp(value, 1, 6);
        diceChoicePanel?.SetActive(false);
        ApplyDiceChoice(value);
    }

    private void ApplyDiceChoice(int value)
    {
        Debug.Log($"[GameManager] ChooseDice: выбрано {value}.");
        // Передаём в BattleManager напрямую (кубик пути — отдельная логика)
        battleManager.ExecuteBattle(value);
        SetState(GameState.ResolvingEvent);
        GetUIManager().ShowEndTurnButton(true);
        GetUIManager().ShowDiceButton(false);
    }

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
        playerInventory.DoubleGoods(item);
        Debug.Log($"[GameManager] DoubleGoods: выбран товар {item.name}.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Движение
    // ─────────────────────────────────────────────────────────────────────

    public void OnPathSelected(PathCellInitializer path)
    {
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
        if (State != GameState.ResolvingEvent) return;
        SetState(GameState.Moving);
        ContinueMovement();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateDrawCardButton()
    {
        if (drawCardButton != null)
            drawCardButton.gameObject.SetActive(State == GameState.DrawingCard);
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