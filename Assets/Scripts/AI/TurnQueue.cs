using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Обрабатывает один ход всех ИИ-торговцев.
/// Вызывается из GameManager.RequestEndTurn() — один раз за нажатие кнопки.
///
/// Последовательность:
/// 1. Снимок мира
/// 2. ИИ планируют (Task.Run)
/// 3. Раздача путей по инициативе (PathClaims)
/// 4. Торговля по инициативе
/// 5. Один шаг движения каждого ИИ
/// </summary>
public class TurnQueue
{
    private readonly List<ITrader> _traders;
    private readonly WorldEconomy  _economy;

    public int TurnNumber { get; private set; }

    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnResolved;

    public TurnQueue(List<ITrader> traders, WorldEconomy economy)
    {
        _traders = traders.OrderByDescending(t => t.Initiative).ToList();
        _economy = economy;
    }

    /// <summary>
    /// Один ход всех ИИ. Вызывай через StartCoroutine из GameManager.
    /// </summary>
    public IEnumerator ProcessTurnCoroutine()
    {
        TurnNumber++;
        OnTurnStarted?.Invoke(TurnNumber);
        AIDebugLog.NewTurn(TurnNumber);

        var snapshot = _economy.TakeSnapshot();

        if (snapshot.Cities.Count == 0)
        {
            Debug.LogWarning("[TurnQueue] Snapshot пустой — проверь CityBindings в WorldEconomy.");
            OnTurnResolved?.Invoke(TurnNumber);
            yield break;
        }

        // 1. Все ИИ планируют параллельно
        var aiTraders = _traders.OfType<AITrader>().ToList();
        var aiTasks   = aiTraders
            .Select(t => Task.Run(() => t.PlanTurn(snapshot)))
            .ToArray();

        while (!Task.WhenAll(aiTasks).IsCompleted)
            yield return null;

        var intents = aiTasks.Select(t => t.Result).ToList();

        // Лог решений
        foreach (var intent in intents)
            Debug.Log($"[TurnQueue] {intent.Trader.DisplayName}: " +
                      $"путь={intent.SelectedPath?.name ?? "нет"} | " +
                      $"покупок={intent.BuyOrders.Count} | " +
                      $"продаж={intent.SellOrders.Count}");

        // 2. Раздача путей по инициативе
        var sorted = intents.OrderByDescending(i => i.Trader.Initiative).ToList();
        AssignPaths(sorted);

        var goldBefore = new Dictionary<string, int>();
        foreach (var intent in sorted)
            if (intent.Trader is AITrader aiSnap)
                goldBefore[aiSnap.DisplayName] = aiSnap.Gold;

        // 3. Торговля
        foreach (var intent in sorted)
            intent.Trader.ExecuteTrade(intent, _economy);

                // Логируем финансы и движение
        foreach (var intent in sorted)
        {
            string name = intent.Trader.DisplayName;
 
            // Финансы
            if (intent.Trader is AITrader aiLog && goldBefore.TryGetValue(name, out int before))
                AIDebugLog.RecordFinance(name, before, aiLog.Gold);
 
            // Движение
            string pathName = intent.SelectedPath?.name;
            string destName = intent.SelectedPath?.FinishCity?.CityName ?? "?";
            AIDebugLog.RecordMove(name, pathName, destName);
        }


        // 4. Один шаг движения для каждого ИИ — одновременно
        var moveCoroutines = sorted
            .Select(intent => intent.Trader.ExecuteMovement(intent))
            .ToList();

        // Запускаем все движения параллельно через AITurnManager
        yield return AITurnManager.Instance.RunParallel(moveCoroutines);

        OnTurnResolved?.Invoke(TurnNumber);
    }

    private void AssignPaths(List<TurnIntent> sorted)
    {
        var claims = new PathClaims();
        foreach (var intent in sorted)
        {
            if (intent.SelectedPath == null) continue;
            if (!claims.TryClaim(intent.SelectedPath))
            {
                var blocked = intent.SelectedPath;
                intent.SelectedPath = null;
                if (intent.Trader is AITrader ai)
                    ai.NotifyPathBlocked(blocked);
            }
        }
    }
}