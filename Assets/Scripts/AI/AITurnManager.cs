using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour-менеджер ИИ-торговцев и TurnQueue.
///
/// Намеренно отделён от существующего GameManager —
/// не ломает твою текущую архитектуру, просто добавляет слой сверху.
///
/// Размести в сцене:
///   AITurnManager (пустой GameObject)
///     ← этот компонент
///     ← PlayerTraderAdapter
/// </summary>
public class AITurnManager : MonoBehaviour
{
    public static AITurnManager Instance { get; private set; }

    [Header("Игрок")]
    [SerializeField] private PlayerTraderAdapter playerAdapter;

    [Header("ИИ торговцы")]
    [SerializeField] private TraderProfile[] aiProfiles;
    [SerializeField] private City[]          aiStartCities;

    [Header("Экономика")]
    [SerializeField] private WorldEconomy worldEconomy;

    [Header("Настройки")]
    [SerializeField] private float delayBetweenTurns = 0.5f;

    private TurnQueue         _turnQueue;
    private List<AITrader>    _aiTraders = new List<AITrader>();

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
        StartCoroutine(TurnLoop());
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
                Debug.LogError($"[AITurnManager] У профиля {profile.displayName} " +
                               $"не задан tokenPrefab.");
                continue;
            }

            var go     = Instantiate(profile.tokenPrefab);
            go.name    = $"AITrader_{profile.displayName}";
            var trader = go.GetComponent<AITrader>();

            if (trader == null)
            {
                Debug.LogError($"[AITurnManager] Префаб {profile.tokenPrefab.name} " +
                               $"не содержит компонент AITrader.");
                continue;
            }

            trader.Initialize(profile, startCity, worldEconomy);
            _aiTraders.Add(trader);

            Debug.Log($"[AITurnManager] Создан ИИ: {profile.displayName} " +
                      $"в городе {startCity?.CityName}");
        }
    }

    private void InitTurnQueue()
    {
        var allTraders = new List<ITrader> { playerAdapter };
        allTraders.AddRange(_aiTraders);

        _turnQueue = new TurnQueue(allTraders, worldEconomy);

        _turnQueue.OnTurnStarted  += t => Debug.Log($"[TurnQueue] Ход {t} начался");
        _turnQueue.OnTurnResolved += t => Debug.Log($"[TurnQueue] Ход {t} завершён");
    }

    // ------------------------------------------------------------------
    // Игровой цикл
    // ------------------------------------------------------------------

    private IEnumerator TurnLoop()
    {
        while (true)
        {
            yield return _turnQueue.ProcessTurnCoroutine(playerAdapter);
            yield return new WaitForSeconds(delayBetweenTurns);
        }
    }

    // ------------------------------------------------------------------
    // Публичный доступ для UI
    // ------------------------------------------------------------------

    public IReadOnlyList<AITrader> AiTraders => _aiTraders;
}