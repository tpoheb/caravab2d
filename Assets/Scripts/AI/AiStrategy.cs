using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Стратегия ИИ-торговца.
///
/// Арбитраж с раздельными ценами (как в CityData):
///   buyPrice  — сколько ИИ платит чтобы купить товар в городе A
///   sellPrice — сколько ИИ получает если продаёт товар в городе B
///
/// Реальная маржа = sellPrice(B) - buyPrice(A)
/// Именно эту разницу мы ищем.
/// </summary>
public class AiStrategy
{
    private readonly TraderProfile _profile;
    private readonly WorldEconomy  _economy;

    public AiStrategy(TraderProfile profile, WorldEconomy economy)
    {
        _profile = profile;
        _economy = economy;
    }

    // ------------------------------------------------------------------
    // Главный метод — вызывается из AITrader.PlanTurn()
    // ------------------------------------------------------------------

    public TurnIntent Evaluate(GameSnapshot snapshot, AITrader trader)
    {
        var intent = new TurnIntent { Trader = trader };

        var currentSnap = GetCitySnapshot(snapshot, trader.CurrentCity);

        // 1. Продаём что везём если цена выгодная
        if (currentSnap != null)
            AddSellOrders(intent, trader, currentSnap);

        // 2. Ищем лучший арбитраж
        var opportunity = FindBestArbitrage(snapshot, trader);

        if (opportunity == null)
        {
            intent.SelectedPath = null;
            return intent;
        }

        // 3. Покупаем если уже в городе покупки
        if (trader.CurrentCity == opportunity.BuyCity && currentSnap != null)
            AddBuyOrders(intent, trader, opportunity, currentSnap);

        // 4. Идём к следующей цели
        City target = trader.CurrentCity == opportunity.BuyCity
            ? opportunity.SellCity
            : opportunity.BuyCity;

        intent.SelectedPath = GetNextPath(trader.CurrentCity, target);

        return intent;
    }

    // ------------------------------------------------------------------
    // Поиск арбитража — buyPrice(A) vs sellPrice(B)
    // ------------------------------------------------------------------

    private ArbitrageOpportunity FindBestArbitrage(
        GameSnapshot snapshot, AITrader trader)
    {
        ArbitrageOpportunity best      = null;
        float                bestScore = _profile.minProfitThreshold;

        foreach (var buySnap in snapshot.Cities)
        foreach (var sellSnap in snapshot.Cities)
        {
            if (buySnap.City == sellSnap.City) continue;

            foreach (var goodId in snapshot.AllGoods)
            {
                // buyPrice — сколько платим в buySnap (снимок хранит currentBuyPrice)
                float buyPrice = buySnap.GetPrice(goodId);
                int   stock    = buySnap.GetStock(goodId);

                if (stock == 0 || buyPrice <= 0) continue;

                // sellPrice — сколько получим в sellSnap
                // WorldEconomy.GetSellPrice возвращает currentSellPrice
                float sellPrice = _economy.GetSellPrice(sellSnap.City, goodId);

                if (sellPrice <= 0) continue;

                float margin = sellPrice - buyPrice;
                if (margin <= 0) continue;

                // Расстояние: от текущей позиции до города покупки + до продажи
                int distToBuy  = GetCityDistance(trader.CurrentCity, buySnap.City);
                int distToSell = GetCityDistance(buySnap.City, sellSnap.City);
                int totalDist  = distToBuy + distToSell;

                if (totalDist == int.MaxValue) continue;

                float score = (margin / Mathf.Max(1, totalDist))
                              * _profile.greedWeight;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = new ArbitrageOpportunity
                    {
                        GoodId    = goodId,
                        BuyCity   = buySnap.City,
                        SellCity  = sellSnap.City,
                        BuyPrice  = buyPrice,
                        SellPrice = sellPrice,
                        Stock     = stock,
                        Score     = score
                    };
                }
            }
        }

        return best;
    }

    // ------------------------------------------------------------------
    // Формирование заявок
    // ------------------------------------------------------------------

    private void AddBuyOrders(TurnIntent intent, AITrader trader,
        ArbitrageOpportunity opp, CitySnapshot snap)
    {
        float buyPrice = snap.GetPrice(opp.GoodId);
        if (buyPrice <= 0) return;

        int affordable = trader.Gold / Mathf.Max(1, Mathf.RoundToInt(buyPrice));
        int amount     = Mathf.Min(affordable, opp.Stock);

        if (amount > 0)
            intent.BuyOrders.Add(new TradeOrder
                { GoodId = opp.GoodId, Amount = amount });
    }

    private void AddSellOrders(TurnIntent intent, AITrader trader,
        CitySnapshot snap)
    {
        foreach (var kvp in trader.Inventory.GetAll())
        {
            string goodId = kvp.Key;
            int    held   = kvp.Value;

            if (held <= 0) continue;

            // Продаём только если sellPrice > 0
            float sellPrice = _economy.GetSellPrice(snap.City, goodId);
            if (sellPrice > 0)
                intent.SellOrders.Add(new TradeOrder
                    { GoodId = goodId, Amount = held });
        }
    }

    // ------------------------------------------------------------------
    // BFS по графу City → PathCellInitializer → FinishCity
    // ------------------------------------------------------------------

    private int GetCityDistance(City from, City target)
    {
        if (from == target) return 0;

        var visited = new HashSet<City> { from };
        var queue   = new Queue<(City city, int dist)>();
        queue.Enqueue((from, 0));

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();

            foreach (var path in current.Paths)
            {
                if (path?.FinishCity == null) continue;
                var neighbour = path.FinishCity;

                if (neighbour == target)    return dist + 1;
                if (visited.Contains(neighbour)) continue;

                visited.Add(neighbour);
                queue.Enqueue((neighbour, dist + 1));
            }
        }

        return int.MaxValue;
    }

    private PathCellInitializer GetNextPath(City from, City target)
    {
        if (from == target || target == null) return null;

        var cameFrom = new Dictionary<City, (City prev, PathCellInitializer path)>();
        var queue    = new Queue<City>();

        cameFrom[from] = (null, null);
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var path in current.Paths)
            {
                if (path?.FinishCity == null) continue;
                var neighbour = path.FinishCity;

                if (cameFrom.ContainsKey(neighbour)) continue;

                cameFrom[neighbour] = (current, path);
                queue.Enqueue(neighbour);

                if (neighbour == target)
                    return ReconstructFirstStep(cameFrom, from, target);
            }
        }

        return null;
    }

    private PathCellInitializer ReconstructFirstStep(
        Dictionary<City, (City prev, PathCellInitializer path)> cameFrom,
        City from, City target)
    {
        var current = target;
        PathCellInitializer firstPath = null;

        while (cameFrom[current].prev != null)
        {
            firstPath = cameFrom[current].path;
            current   = cameFrom[current].prev;
            if (current == from) break;
        }

        return firstPath;
    }

    // ------------------------------------------------------------------
    // Вспомогательный поиск снапшота
    // ------------------------------------------------------------------

    private CitySnapshot GetCitySnapshot(GameSnapshot snapshot, City city)
    {
        foreach (var s in snapshot.Cities)
            if (s.City == city) return s;
        return null;
    }
}

// ------------------------------------------------------------------
// Данные арбитражной возможности
// ------------------------------------------------------------------

public class ArbitrageOpportunity
{
    public string GoodId;
    public City   BuyCity;
    public City   SellCity;
    public float  BuyPrice;
    public float  SellPrice;
    public int    Stock;
    public float  Score;
}