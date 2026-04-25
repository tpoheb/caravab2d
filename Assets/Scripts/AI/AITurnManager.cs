using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет ИИ-торговцами. Не имеет своего цикла —
/// запускается из GameManager.RequestEndTurn() одновременно с ходом игрока.
/// </summary>
public class AITurnManager : MonoBehaviour
{
    public static AITurnManager Instance { get; private set; }

    [Header("ИИ торговцы")]
    [SerializeField] private TraderProfile[] aiProfiles;
    [SerializeField] private City[]          aiStartCities;

    [Header("Экономика")]
    [SerializeField] private WorldEconomy worldEconomy;

    [Header("Настройки")]
    [Tooltip("Пауза между анимацией шага ИИ и следующим действием")]
    [SerializeField] private float stepDelay = 0.3f;

    private TurnQueue      _turnQueue;
    private List<AITrader> _aiTraders = new List<AITrader>();
    private bool           _isProcessing;

    public float StepDelay => stepDelay;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitAiTraders();
        InitTurnQueue();
    }

    // ------------------------------------------------------------------
    // Инициализация
    // ------------------------------------------------------------------

    private void InitAiTraders()
    {
        for (int i = 0; i < aiProfiles.Length; i++)
        {
            var profile   = aiProfiles[i];
            var startCity = i < aiStartCities.Length ? aiStartCities[i] : null;

            if (profile.tokenPrefab == null)
            {
                Debug.LogError($"[AITurnManager] У профиля '{profile.displayName}' не задан tokenPrefab.");
                continue;
            }

            var go     = Instantiate(profile.tokenPrefab);
            go.name    = $"AITrader_{profile.displayName}";
            var trader = go.GetComponent<AITrader>();

            if (trader == null)
            {
                Debug.LogError($"[AITurnManager] Префаб '{profile.tokenPrefab.name}' не содержит AITrader.");
                continue;
            }

            trader.Initialize(profile, startCity, worldEconomy);
            _aiTraders.Add(trader);

            Debug.Log($"[AITurnManager] Создан ИИ: {profile.displayName} в городе {startCity?.CityName ?? "NULL"}");
        }
    }

    private void InitTurnQueue()
    {
        var traders = new List<ITrader>(_aiTraders);
        _turnQueue  = new TurnQueue(traders, worldEconomy);

        _turnQueue.OnTurnStarted  += t => Debug.Log($"[TurnQueue] Ход {t} начался");
        _turnQueue.OnTurnResolved += t => Debug.Log($"[TurnQueue] Ход {t} завершён");
    }

    // ------------------------------------------------------------------
    // Вызывается из GameManager.RequestEndTurn()
    // ------------------------------------------------------------------

    /// <summary>
    /// Запускает один ход всех ИИ.
    /// GameManager вызывает это сразу после того как обработал ход игрока.
    /// </summary>
    public void ProcessAITurn()
    {
        if (_isProcessing)
        {
            Debug.LogWarning("[AITurnManager] Ход уже обрабатывается — пропускаем.");
            return;
        }

        StartCoroutine(ProcessAITurnCoroutine());
    }

    private IEnumerator ProcessAITurnCoroutine()
    {
        _isProcessing = true;
        yield return _turnQueue.ProcessTurnCoroutine();
        _isProcessing = false;
    }

    // ------------------------------------------------------------------
    // Параллельное выполнение корутин (для одновременного движения)
    // ------------------------------------------------------------------

    /// <summary>
    /// Запускает список корутин параллельно и ждёт пока все завершатся.
    /// Используется TurnQueue для одновременного движения всех ИИ.
    /// </summary>
    public IEnumerator RunParallel(List<IEnumerator> coroutines)
    {
        int remaining = coroutines.Count;
        if (remaining == 0) yield break;

        foreach (var coroutine in coroutines)
            StartCoroutine(WrapCoroutine(coroutine, () => remaining--));

        while (remaining > 0)
            yield return null;
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, System.Action onComplete)
    {
        yield return coroutine;
        onComplete();
    }

    // ------------------------------------------------------------------
    // Доступ
    // ------------------------------------------------------------------

    public IReadOnlyList<AITrader> AiTraders => _aiTraders;
}