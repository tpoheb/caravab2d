using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Оркестрирует ходы всех участников.
///
/// Последовательность каждого хода:
/// 1. TakeSnapshot()     — снимок мира
/// 2. PlanTurn() × все  — ИИ думают в Task.Run, ждём игрока
/// 3. PathClaims         — раздаём пути по инициативе
/// 4. ExecuteTrade()     — торговля по инициативе
/// 5. ExecuteMovement()  — движение (корутина) по инициативе
/// </summary>
public class TurnQueue
{
    private readonly List<ITrader>  _traders;
    private readonly WorldEconomy   _economy;

    public int TurnNumber { get; private set; }

    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnResolved;

    public TurnQueue(List<ITrader> traders, WorldEconomy economy)
    {
        _traders = traders.OrderByDescending(t => t.Initiative).ToList();
        _economy = economy;
    }

    // ------------------------------------------------------------------
    // Главная корутина — вызывается из GameManager каждый ход
    // ------------------------------------------------------------------

    public IEnumerator ProcessTurnCoroutine(PlayerTraderAdapter playerAdapter)
    {
        TurnNumber++;
        OnTurnStarted?.Invoke(TurnNumber);

        var snapshot = _economy.TakeSnapshot();

        // 1. ИИ планируют в фоне
        var aiTraders = _traders.OfType<AITrader>().ToList();
        var aiTasks   = aiTraders
            .Select(t => Task.Run(() => t.PlanTurn(snapshot)))
            .ToArray();

        // 2. Ждём решения игрока
        while (!playerAdapter.IsReady)
            yield return null;

        TurnIntent playerIntent = playerAdapter.ConsumeIntent();

        // 3. Ждём ИИ (обычно уже готовы)
        while (!Task.WhenAll(aiTasks).IsCompleted)
            yield return null;

        // 4. Собираем все интенты
        var intents = new List<TurnIntent> { playerIntent };
        intents.AddRange(aiTasks.Select(t => t.Result));

        // Сортируем по инициативе — высокая идёт первой везде
        var sorted = intents
            .OrderByDescending(i => i.Trader.Initiative)
            .ToList();

        // 5. Раздаём пути по инициативе (PathClaims)
        AssignPaths(sorted);

        // 6. Торговля по инициативе
        foreach (var intent in sorted)
            intent.Trader.ExecuteTrade(intent, _economy);

        // 7. Движение по инициативе (пошагово, корутина)
        foreach (var intent in sorted)
            yield return intent.Trader.ExecuteMovement(intent);

        OnTurnResolved?.Invoke(TurnNumber);
    }

    // ------------------------------------------------------------------
    // PathClaims — раздача путей по инициативе
    // ------------------------------------------------------------------

    private void AssignPaths(List<TurnIntent> sortedByInitiative)
    {
        var claims = new PathClaims();

        foreach (var intent in sortedByInitiative)
        {
            if (intent.SelectedPath == null) continue;

            if (claims.TryClaim(intent.SelectedPath))
            {
                // Путь получен — движение разрешено
            }
            else
            {
                // Путь занят — обнуляем выбор
                intent.SelectedPath = null;

                if (intent.Trader is AITrader ai)
                    ai.NotifyPathBlocked(intent.SelectedPath);
            }
        }
    }
}
