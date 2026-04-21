using System;
using System.Collections;

/// <summary>
/// Единый интерфейс для всех участников игры.
/// TurnQueue работает только через этот интерфейс —
/// не знает кто человек, а кто ИИ.
/// </summary>
public interface ITrader
{
    string DisplayName { get; }
    int    Initiative  { get; }
    int    Gold        { get; }

    /// <summary>Город в котором торговец находится сейчас.</summary>
    City CurrentCity { get; }

    /// <summary>
    /// Фаза планирования. Получает снимок мира, возвращает намерение.
    /// Ничего не меняет в игровом мире.
    /// </summary>
    TurnIntent PlanTurn(GameSnapshot snapshot);

    /// <summary>
    /// Фаза исполнения: торговля (покупка/продажа).
    /// Вызывается TurnQueue строго по инициативе.
    /// </summary>
    void ExecuteTrade(TurnIntent intent, WorldEconomy economy);

    /// <summary>
    /// Фаза движения: пошаговое перемещение по PathController.
    /// Возвращает корутину — TurnQueue yield return-ит её.
    /// </summary>
    IEnumerator ExecuteMovement(TurnIntent intent);

    /// <summary>Торговец прибыл в новый город.</summary>
    event Action<ITrader, City> OnArrivedAtCity;

    /// <summary>Путь заблокирован другим торговцем.</summary>
    event Action<ITrader, PathCellInitializer> OnPathBlocked;
}
