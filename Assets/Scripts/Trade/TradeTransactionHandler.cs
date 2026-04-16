using UnityEngine;

public static class TradeTransactionHandler
{
    public static void ProcessBuyTransaction(CityData.CityItem cityItem, int quantity, 
                                    CityData city, PlayerInventory playerInventory, 
                                    PlayerStats playerStats)
    {
        // 1. Вычисляем динамическую стоимость чека
        int totalCost = CalculateTransactionCost(cityItem, quantity, true, playerStats);
        int totalWeight = cityItem.item.weight * quantity;

        if (!CanBuy(cityItem, quantity, totalCost, totalWeight, city, playerInventory)) 
            return;

        ExecuteBuyTransaction(cityItem, quantity, totalCost, city, playerInventory); 
    }

    public static void ProcessSellTransaction(CityData.CityItem cityItem, int quantity, 
                                     CityData city, PlayerInventory playerInventory, 
                                     PlayerStats playerStats)
    {
        // 1. Вычисляем динамическую выручку с чека
        int totalValue = CalculateTransactionCost(cityItem, quantity, false, playerStats);

        if (!CanSell(cityItem, quantity, totalValue, city, playerInventory)) 
            return;

        ExecuteSellTransaction(cityItem, quantity, totalValue, city, playerInventory); 
    }

    // --- НОВАЯ ЛОГИКА КАЛЬКУЛЯТОРА ---
    private static int CalculateTransactionCost(CityData.CityItem cityItem, int quantity, bool isBuying, PlayerStats playerStats)
    {
        // Берем нужную стартовую цену
        float tempPrice = isBuying ? cityItem.currentBuyPrice : cityItem.currentSellPrice;
        float total = 0f;
        float bargainEffect = (playerStats != null) ? playerStats.Bargain * 0.01f : 0f;

        for (int i = 0; i < quantity; i++)
        {
            float priceWithBargain = isBuying 
                ? tempPrice * (1f - bargainEffect)
                : tempPrice * (1f + bargainEffect);
            
            total += Mathf.Max(1f, Mathf.Round(priceWithBargain));

            // Симулируем сдвиг цены
            if (isBuying)
                tempPrice = Mathf.Min(cityItem.maxBuyPrice, tempPrice * (1f + cityItem.volatility));
            else
                tempPrice = Mathf.Max(cityItem.minSellPrice, tempPrice * (1f - cityItem.volatility));
        }

        return Mathf.RoundToInt(total);
    }
    
    // --- ОБНОВЛЕННЫЕ ИСПОЛНИТЕЛИ ---
    private static void ExecuteBuyTransaction(CityData.CityItem cityItem, int quantity, 
        int totalCost, CityData city, PlayerInventory playerInventory)
    {
        playerInventory.Money -= totalCost;
        city.cityGold += totalCost;
        cityItem.stock -= quantity;
        playerInventory.AddItem(cityItem.item, quantity, totalCost);

        // После покупки игроком (запас падает) -> ОБЕ цены растут
        for (int i = 0; i < quantity; i++)
        {
            cityItem.currentBuyPrice = Mathf.Min(cityItem.maxBuyPrice, cityItem.currentBuyPrice * (1f + cityItem.volatility));
            cityItem.currentSellPrice = Mathf.Min(cityItem.maxSellPrice, cityItem.currentSellPrice * (1f + cityItem.volatility));
        }
        
        Debug.Log($"Bought {quantity}. New Buy: {cityItem.currentBuyPrice}, Sell: {cityItem.currentSellPrice}");
    }

    private static void ExecuteSellTransaction(CityData.CityItem cityItem, int quantity, 
        int totalValue, CityData city, PlayerInventory playerInventory)
    {
        playerInventory.Money += totalValue;
        city.cityGold -= totalValue;
        cityItem.stock += quantity;
        playerInventory.RemoveItem(cityItem.item, quantity);

        // После продажи игроком (запас растет) -> ОБЕ цены падают
        for (int i = 0; i < quantity; i++)
        {
            cityItem.currentBuyPrice = Mathf.Max(cityItem.minBuyPrice, cityItem.currentBuyPrice * (1f - cityItem.volatility));
            cityItem.currentSellPrice = Mathf.Max(cityItem.minSellPrice, cityItem.currentSellPrice * (1f - cityItem.volatility));
        }

        Debug.Log($"Sold {quantity}. New Buy: {cityItem.currentBuyPrice}, Sell: {cityItem.currentSellPrice}");
    }
    // (Методы CanBuy, CanSell и LogTradeValidation остаются без изменений)
    private static bool CanBuy(CityData.CityItem cityItem, int quantity, int totalCost, int totalWeight, CityData city, PlayerInventory playerInventory)
    {
        bool canCarry = playerInventory.CanCarryMore(totalWeight);
        bool playerHasMoney = playerInventory.Money >= totalCost;
        bool cityHasStock = cityItem.stock >= quantity;
        return canCarry && playerHasMoney && cityHasStock;
    }

    private static bool CanSell(CityData.CityItem cityItem, int quantity, int totalValue, CityData city, PlayerInventory playerInventory)
    {
        bool playerHasItems = playerInventory.GetItemStock(cityItem.item) >= quantity;
        bool cityHasMoney = city.cityGold >= totalValue;
        return playerHasItems && cityHasMoney;
    }
}