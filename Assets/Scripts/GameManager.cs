using UnityEngine;
using UnityEngine.UI; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private Button rollForAttackButton;
    
    [Header("Системы")]
    [SerializeField] private PlayerToken playerToken;
    [SerializeField] private DiceSystem diceSystem;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private HandManager handManager;
    

    public GameState State { get; private set; } = GameState.Idle;
    private int lastDiceValue;

    // --------------------
    // ЖИЗНЕННЫЙ ЦИКЛ
    // --------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateReferences();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        StartTurn();
        var ui = battleManager.GetUIManager();
        ui.ShowBattleRollButton(false); // Вне боя не видна
        ui.ShowEndTurnButton(false);   // Скрыта, пока фишка не встанет на клетку
    }

    // --------------------
    // ПОДПИСКИ
    // --------------------

    private void Subscribe()
    {
        if (diceSystem != null)
            diceSystem.OnDiceRolled += OnDiceRolled;

        if (playerToken != null)
            playerToken.OnArrivedAtCity += OnArrivedAtCity;
    }

    private void Unsubscribe()
    {
        if (diceSystem != null)
            diceSystem.OnDiceRolled -= OnDiceRolled;

        if (playerToken != null)
            playerToken.OnArrivedAtCity -= OnArrivedAtCity;
    }

    // --------------------
    // FSM
    // --------------------

    private void SetState(GameState newState)
    {
        Debug.Log($"FSM: переход состояния {State} → {newState}");
        State = newState;
    }

    // --------------------
    // ХОД ИГРОКА
    // --------------------

    private void StartTurn()
    {
        Debug.Log("GameManager: Начат новый ход игрока");
        SetState(GameState.InCity);
    }

    public void RequestEndTurn()
    {
        Debug.Log($"[LOG] GameManager: Запрос завершения хода в состоянии {State}");
        
        if (State == GameState.ResolvingEvent)
        {
            // Скрываем интерфейс битвы/события
            var ui = battleManager.GetUIManager();
            ui.ShowEndTurnButton(false);
            ui.ClearEventText();
            
            
            // Возвращаемся в движение
            SetState(GameState.Moving);
            ContinueMovement();
        }
    }

    // --------------------
    // ГОРОД / ВЫБОР ПУТИ
    // --------------------

    private void OnArrivedAtCity(City city)
    {
        Debug.Log($"GameManager: Игрок прибыл в город {city.CityName}");
        SetState(GameState.InCity);
    }

    public void OnPathSelected(PathCellInitializer path)
    {
        Debug.Log($"[LOG] GameManager: Получен вызов OnPathSelected. Текущее состояние: {State}");

        if (State != GameState.InCity)
        {
            Debug.LogWarning($"[LOG] GameManager: Путь проигнорирован, так как состояние {State}, а не InCity");
            return;
        }

        if (path == null)
        {
            Debug.LogError("[LOG] GameManager: Передан пустой путь (null)!");
            return;
        }

        Debug.Log($"[LOG] GameManager: Инициализируем путь к {path.FinishCity?.CityName}. Переходим в Moving.");
        
        // 1. Устанавливаем данные в токене
        playerToken.StartPath(path);
        
        // 2. Меняем состояние
        SetState(GameState.Moving);

        // 3. ВАЖНО: Принудительно вызываем первый шаг, чтобы фишка ушла на клетку 0
        Debug.Log("[LOG] GameManager: Вызываем ContinueMovement для первого шага.");
        ContinueMovement();
    }

    // --------------------
    // ДВИЖЕНИЕ
    // --------------------

    public void ContinueMovement()
    {
        Debug.Log($"[LOG] GameManager: Вызов ContinueMovement. Состояние: {State}");

        if (State != GameState.Moving)
        {
            Debug.LogWarning("[LOG] GameManager: ContinueMovement прерван, состояние не Moving!");
            return;
        }

        // Пытаемся подвинуть фишку
        Debug.Log("[LOG] GameManager: Запрос к PlayerToken.AdvanceToken()...");
        bool reachedCity = playerToken.AdvanceToken();

        if (reachedCity)
        {
            Debug.Log("[LOG] GameManager: PlayerToken сообщил, что достиг города.");
            SetState(GameState.InCity);
        }
        else
        {
            Debug.Log("[LOG] GameManager: Шаг выполнен успешно. Переходим к броску кубика.");
            SetState(GameState.RollingDice);
            diceSystem.RollDice();
        }
    }

    // --------------------
    // КУБИК
    // --------------------

    private void RollDice()
    {
        Debug.Log("GameManager: Бросок кубика");
        SetState(GameState.RollingDice);
        diceSystem.RollDice();
    }

    public void OnDiceRolled(int value, DiceEventType type)
    {
        lastDiceValue = value;

        if (State == GameState.InBattle)
        {
            Debug.Log($"[LOG] GameManager: Кубик в бою показал {value}.");
            battleManager.ExecuteBattle(value);
            OnEventResolved(); // Используем наш новый метод для включения кнопки
            return; 
        }

        // Передаем и тип события, и само значение кубика
        ResolveDiceEvent(type, value);
    }
    // Метод, который вызывается при нажатии на кнопку "Бросить кубик в бою"
    public void RequestBattleDiceRoll()
    {
        battleManager.GetUIManager().ShowBattleRollButton(false);
        diceSystem.RollDice();
    }

    // --------------------
    // СОБЫТИЯ
    // --------------------

    private void ResolveDiceEvent(DiceEventType type, int diceValue)
    {
        // 1. Показываем текст события
        battleManager.GetUIManager().DisplayEventInfo(type, diceValue);

        // 2. Логика самих событий
        switch (type)
        {
            case DiceEventType.Battle:
                StartBattlePhase(); // Здесь кнопка хода НЕ появится, пока бой не кончится
                break;
        
            case DiceEventType.ShadowInfluence:
                cardManager.DrawCard();
                OnEventResolved(); // Тень применилась, показываем кнопку "Конец хода"
                break;

            case DiceEventType.PeacefulPass:
                OnEventResolved(); // Пустой проход, показываем кнопку "Конец хода"
                break;
        }
    }
    private BattleCardData GetRandomBattleCard()
    {
        // Обращаемся к менеджеру карт, чтобы он выдал нам случайную битву
        return cardManager.GetRandomBattleCard(); 
    }
    private void StartBattlePhase()
    {
        SetState(GameState.InBattle);
    
        // 1. Получаем случайную карту из твоего списка/базы карт
        BattleCardData randomCard = GetRandomBattleCard(); 

        // 2. Передаем её в BattleManager
        battleManager.PrepareBattle(randomCard);
    }
    private void OnEventResolved()
    {
        Debug.Log("GameManager: Событие завершено. Ждем нажатия кнопки 'Завершить ход'.");
    
        // МЫ БОЛЬШЕ НЕ ВЫЗЫВАЕМ ContinueMovement() АВТОМАТИЧЕСКИ!
        // Вместо этого мы остаемся в состоянии ResolvingEvent или вводим WaitEndTurn
        SetState(GameState.ResolvingEvent); 
    
        var ui = battleManager.GetUIManager();
        if (ui != null)
        {
            ui.ShowEndTurnButton(true);
            ui.ShowBattleRollButton(false); // На всякий случай гасим кнопку атаки
        }
    }

    public void CompleteEventPhase()
    {
        if (State != GameState.ResolvingEvent)
        {
            Debug.LogWarning($"GameManager: CompleteEventPhase вызван в состоянии {State}");
            return;
        }

        Debug.Log("GameManager: Событие завершено, продолжаем движение");

        SetState(GameState.Moving);
        ContinueMovement();
    }
    // --------------------
    // ВАЛИДАЦИЯ
    // --------------------

    private void ValidateReferences()
    {
        if (playerToken == null)
            Debug.LogError("GameManager: PlayerToken не назначен");

        if (diceSystem == null)
            Debug.LogError("GameManager: DiceSystem не назначен");

        if (battleManager == null)
            Debug.LogError("GameManager: BattleManager не назначен");
        
        if (handManager == null)
            Debug.LogError("GameManager: HandManager не назначен");
    }
}
