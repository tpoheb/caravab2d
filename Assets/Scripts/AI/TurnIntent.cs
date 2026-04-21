using System.Collections.Generic;

/// <summary>
/// Намерение торговца на один ход.
/// Формируется в PlanTurn(), применяется в ExecuteTurn().
/// Использует City и PathCellInitializer напрямую — без координат.
/// </summary>
public class TurnIntent
{
    public ITrader Trader { get; set; }

    /// <summary>Путь по которому торговец хочет двинуться из текущего города.</summary>
    public PathCellInitializer SelectedPath { get; set; }

    /// <summary>Список товаров для покупки в текущем городе.</summary>
    public List<TradeOrder> BuyOrders { get; set; } = new List<TradeOrder>();

    /// <summary>Список товаров для продажи в текущем городе.</summary>
    public List<TradeOrder> SellOrders { get; set; } = new List<TradeOrder>();
}

/// <summary>Одна торговая заявка: товар и количество.</summary>
public class TradeOrder
{
    public string GoodId  { get; set; }
    public int    Amount  { get; set; }
}
