using System;
using System.Collections.Generic;

/// <summary>Прежняя стратегия торговли и случайного пути, использующая только копию рынка.</summary>
public class AiStrategy
{
    private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

    public AiStrategy(TraderProfile profile, WorldEconomy economy) { }

    public TurnIntent Evaluate(GameSnapshot snapshot, AITrader trader)
    {
        var intent = new TurnIntent { Trader = trader };
        var city = snapshot.GetCity(trader.CurrentCity);
        if (city == null) return intent;
        int projectedGold = trader.Gold;

        foreach (var goodId in snapshot.AllGoods)
        {
            int held = trader.Inventory.GetAmount(goodId);
            float sellPrice = city.GetSellPrice(goodId);
            if (held > 0 && sellPrice > trader.GetAveragePurchasePrice(goodId))
            {
                intent.SellOrders.Add(new TradeOrder { GoodId = goodId, Amount = held });
                projectedGold += (int)Math.Round(sellPrice * held);
            }
        }

        int budget = (int)Math.Floor(projectedGold * 0.15f);
        var affordableGoods = new List<string>();
        foreach (var goodId in snapshot.AllGoods)
        {
            float price = city.GetPrice(goodId);
            if (city.GetStock(goodId) > 0 && price > 0 && price <= budget)
                affordableGoods.Add(goodId);
        }
        if (affordableGoods.Count > 0)
        {
            string goodId = affordableGoods[_random.Next(affordableGoods.Count)];
            int amount = Math.Min((int)Math.Floor(budget / city.GetPrice(goodId)), city.GetStock(goodId));
            if (amount > 0) intent.BuyOrders.Add(new TradeOrder { GoodId = goodId, Amount = amount });
        }

        if (city.Paths.Count > 0) intent.SelectedPath = city.Paths[_random.Next(city.Paths.Count)];
        return intent;
    }
}