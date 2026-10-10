using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет ИИ-торговцами. Не имеет своего цикла —
/// GameManager ждёт завершения всех ИИ перед следующим ходом игрока.
/// </summary>
public partial class AITurnManager : MonoBehaviour
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

        StartCoroutine(ProcessAITurnAndWait());
    }

    public IEnumerator ProcessAITurnAndWait()
    {
        while (_isProcessing) yield return null;
        if (_turnQueue == null) yield break;
        _isProcessing = true;
        try
        {
            yield return _turnQueue.ProcessTurnCoroutine();
        }
        finally
        {
            _isProcessing = false;
        }
    }
    // ------------------------------------------------------------------
    // Доступ
    // ------------------------------------------------------------------

    public IReadOnlyList<AITrader> AiTraders => _aiTraders;
}