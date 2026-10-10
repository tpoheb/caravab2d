using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>ИИ исполняют торговлю и движение по очереди, в порядке списка участников.</summary>
public class TurnQueue
{
    private readonly List<ITrader> _traders;
    private readonly WorldEconomy _economy;
    public int TurnNumber { get; private set; }
    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnResolved;

    public TurnQueue(List<ITrader> traders, WorldEconomy economy)
    {
        _traders = new List<ITrader>(traders);
        _economy = economy;
    }

    public IEnumerator ProcessTurnCoroutine()
    {
        while (PauseMenuController.IsPaused) yield return null;
        TurnNumber++;
        OnTurnStarted?.Invoke(TurnNumber);
        AIDebugLog.NewTurn(TurnNumber);
        if (_traders.Count == 0)
        {
            OnTurnResolved?.Invoke(TurnNumber);
            yield break;
        }

        foreach (var trader in _traders.OfType<AITrader>()) trader.PrepareTurn();
        var snapshot = _economy.TakeSnapshot();
        if (snapshot.Cities.Count == 0)
        {
            Debug.LogWarning("[TurnQueue] В снимке нет городов.");
            OnTurnResolved?.Invoke(TurnNumber);
            yield break;
        }

        // Один снимок для всех планов. Исполнение строго последовательное.
        var tasks = _traders.Select(trader => Task.Run(() => trader.PlanTurn(snapshot))).ToArray();
        var planning = Task.WhenAll(tasks);
        while (!planning.IsCompleted || PauseMenuController.IsPaused) yield return null;
        if (planning.IsFaulted) _ = planning.Exception;

        var claims = new PathClaims();
        for (int i = 0; i < tasks.Length; i++)
        {
            while (PauseMenuController.IsPaused) yield return null;
            if (tasks[i].IsFaulted)
            {
                Debug.LogException(tasks[i].Exception);
                continue;
            }
            if (tasks[i].IsCanceled) continue;
            var intent = tasks[i].Result;
            if (intent == null) continue;
            if (intent.SelectedPath != null && !claims.TryClaim(intent.SelectedPath))
            {
                var blocked = intent.SelectedPath;
                intent.SelectedPath = null;
                if (intent.Trader is AITrader blockedTrader) blockedTrader.NotifyPathBlocked(blocked);
            }

            int goldBefore = intent.Trader.Gold;
            intent.Trader.ExecuteTrade(intent, _economy);
            AIDebugLog.RecordFinance(intent.Trader.DisplayName, goldBefore, intent.Trader.Gold);
            AIDebugLog.RecordMove(intent.Trader.DisplayName, intent.SelectedPath?.name,
                intent.SelectedPath?.FinishCity?.CityName ?? "?");
            yield return intent.Trader.ExecuteMovement(intent);
        }
        while (PauseMenuController.IsPaused) yield return null;
        OnTurnResolved?.Invoke(TurnNumber);
    }
}
