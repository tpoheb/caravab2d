using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Стратегия ИИ:
/// 1. Оценивает возможную прибыль от продажи (продаёт, если текущая цена выше средней цены закупки).
/// 2. Рассчитывает бюджет на покупку (не более 15% от золота с учетом потенциальной выручки от продаж).
/// 3. Покупает случайный доступный товар в рамках бюджета.
/// 4. Путешествует: всегда выбирает случайный путь для следующего шага.
/// </summary>
public class AiStrategy
{
    private TraderProfile _profile;
    private WorldEconomy  _economy;
    
    // Используем System.Random вместо UnityEngine.Random для потокобезопасности в Task.Run
    private System.Random _random;

    public AiStrategy(TraderProfile profile, WorldEconomy economy)
    {
        _profile = profile;
        _economy = economy;
        
        // Инициализируем генератор случайных чисел уникальным сидом для каждого торговца
        _random = new System.Random(System.Guid.NewGuid().GetHashCode());
    }

    public TurnIntent Evaluate(GameSnapshot snapshot, AITrader trader)
    {
        var intent = new TurnIntent { Trader = trader };
        intent.SellOrders = new List<TradeOrder>();
        intent.BuyOrders  = new List<TradeOrder>();

        City currentCity = trader.CurrentCity;
        if (currentCity == null) return intent;

        int projectedGold = trader.Gold;

        // ------------------------------------------------------------------
        // 1. ПРОДАЖА (Только если выгодно)
        // ------------------------------------------------------------------
        foreach (string goodId in snapshot.AllGoods) // Используем AllGoods (с большой буквы, как исправили ранее)
        {
            int amountInInventory = trader.Inventory.GetAmount(goodId);
            if (amountInInventory > 0)
            {
                float avgBuyPrice = trader.GetAveragePurchasePrice(goodId);
                float currentSellPrice = _economy.GetSellPrice(currentCity, goodId);

                if (currentSellPrice > avgBuyPrice)
                {
                    intent.SellOrders.Add(new TradeOrder 
                    { 
                        GoodId = goodId, 
                        Amount = amountInInventory 
                    });

                    projectedGold += Mathf.RoundToInt(currentSellPrice * amountInInventory);
                }
            }
        }

        // ------------------------------------------------------------------
        // 2. ПОКУПКА (Не более 15% от прогнозируемого золота)
        // ------------------------------------------------------------------
        int budget = Mathf.FloorToInt(projectedGold * 0.15f);
        CityData cityData = _economy.GetCityData(currentCity);

        if (cityData != null && budget > 0)
        {
            List<CityData.CityItem> affordableItems = new List<CityData.CityItem>();
            foreach (var cItem in cityData.items)
            {
                if (cItem.stock > 0 && cItem.currentBuyPrice <= budget)
                {
                    affordableItems.Add(cItem);
                }
            }

            if (affordableItems.Count > 0)
            {
                // ИСПОЛЬЗУЕМ System.Random.Next вместо UnityEngine.Random.Range
                int randomItemIndex = _random.Next(0, affordableItems.Count);
                var chosenItem = affordableItems[randomItemIndex];
                
                int maxAmountByBudget = Mathf.FloorToInt(budget / chosenItem.currentBuyPrice);
                int amountToBuy = Mathf.Min(maxAmountByBudget, chosenItem.stock);

                if (amountToBuy > 0)
                {
                    intent.BuyOrders.Add(new TradeOrder 
                    { 
                        GoodId = chosenItem.item.itemName, 
                        Amount = amountToBuy 
                    });
                }
            }
        }

        // ------------------------------------------------------------------
        // 3. РАНДОМНЫЙ ПУТЬ (Непрерывное путешествие)
        // ------------------------------------------------------------------
        if (currentCity.Paths != null && currentCity.Paths.Count > 0)
        {
            // ИСПОЛЬЗУЕМ System.Random.Next вместо UnityEngine.Random.Range
            int randomIndex = _random.Next(0, currentCity.Paths.Count);
            intent.SelectedPath = currentCity.Paths[randomIndex];
        }
        else
        {
            Debug.LogWarning($"[AiStrategy] {trader.DisplayName}: У города {currentCity.CityName} нет доступных путей!");
        }

        return intent;
    }
}