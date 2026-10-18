using UnityEngine;

public static class TradeTransactionHandler
{
    public static bool ProcessBuyTransaction(CityData.CityItem cityItem, int quantity,
                                    CityData city, PlayerInventory playerInventory,
                                    PlayerStats playerStats, out int totalCost)
    {
        totalCost = 0;
        if (!ValidRequest(cityItem, quantity, city, playerInventory)) return false;

        // Вычисление не расходует модификаторы до проверки сделки.
        totalCost = CalculateTransactionCost(cityItem, quantity, true, playerStats);
        int totalWeight = cityItem.item.weight * quantity;

        if (!CanBuy(cityItem, quantity, totalCost, totalWeight, city, playerInventory))
        {
            totalCost = 0;
            return false;
        }

        TradeCardModifiers.Instance?.ConsumePurchaseDiscount();
        ExecuteBuyTransaction(cityItem, quantity, totalCost, city, playerInventory);
        return true;
    }

    public static bool ProcessSellTransaction(CityData.CityItem cityItem, int quantity,
                                     CityData city, PlayerInventory playerInventory,
                                     PlayerStats playerStats, out int totalValue)
    {
        totalValue = 0;
        if (!ValidRequest(cityItem, quantity, city, playerInventory)) return false;

        // Полная выручка, включая фиксированный бонус, должна быть у города.
        totalValue = CalculateTransactionCost(cityItem, quantity, false, playerStats)
            + (TradeCardModifiers.Instance?.PendingSaleBonus ?? 0);

        if (!CanSell(cityItem, quantity, totalValue, city, playerInventory))
        {
            totalValue = 0;
            return false;
        }

        TradeCardModifiers.Instance?.ConsumeSalePriceBoost();
        TradeCardModifiers.Instance?.ConsumeSaleBonus();
        ExecuteSellTransaction(cityItem, quantity, totalValue, city, playerInventory);
        return true;
    }

    // Расчёт стоимости не меняет рынок и не расходует одноразовые бонусы.
    private static int CalculateTransactionCost(CityData.CityItem cityItem, int quantity,
                                            bool isBuying, PlayerStats playerStats)
    {
        float tempPrice = isBuying ? cityItem.currentBuyPrice : cityItem.currentSellPrice;
        float total = 0f;
        float bargainEffect = (playerStats != null) ? playerStats.Bargain * 0.01f : 0f;

        // Модификатор скидки на покупку (Купец купцу друг)
        float purchaseDiscount = isBuying
            ? (TradeCardModifiers.Instance?.PendingPurchaseDiscount ?? 0f)
            : 0f;

        // Модификатор буста цены продажи (Слух из первых уст)
        float salePriceBoost = !isBuying
            ? (TradeCardModifiers.Instance?.PendingSalePriceBoost ?? 0f)
            : 0f;

        for (int i = 0; i < quantity; i++)
        {
            float price = tempPrice;

            if (isBuying)
                price = price * (1f - bargainEffect) * (1f - purchaseDiscount);
            else
                price = price * (1f + bargainEffect) * (1f + salePriceBoost);

            total += Mathf.Max(1f, Mathf.Round(price));

            if (isBuying)
                tempPrice = Mathf.Min(cityItem.maxBuyPrice, tempPrice * (1f + cityItem.volatility));
            else
                tempPrice = Mathf.Max(cityItem.minSellPrice, tempPrice * (1f - cityItem.volatility));
        }

        return Mathf.RoundToInt(total);
    }

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

        cityItem.UpdateDemand();

        Debug.Log($"Bought {quantity}. New Buy: {cityItem.currentBuyPrice}, Sell: {cityItem.currentSellPrice}");
    }

    private static void ExecuteSellTransaction(CityData.CityItem cityItem, int quantity,
        int totalValue, CityData city, PlayerInventory playerInventory)
    {

        playerInventory.Money += totalValue;
        city.cityGold -= totalValue;
        cityItem.stock += quantity;
        playerInventory.RemoveItem(cityItem.item, quantity);

        for (int i = 0; i < quantity; i++)
        {
            cityItem.currentBuyPrice  = Mathf.Max(cityItem.minBuyPrice,  cityItem.currentBuyPrice  * (1f - cityItem.volatility));
            cityItem.currentSellPrice = Mathf.Max(cityItem.minSellPrice, cityItem.currentSellPrice * (1f - cityItem.volatility));
        }

        cityItem.UpdateDemand();

        Debug.Log($"Sold {quantity} for {totalValue}. " +
                  $"New Buy: {cityItem.currentBuyPrice}, Sell: {cityItem.currentSellPrice}");
    }

    private static bool ValidRequest(CityData.CityItem cityItem, int quantity, CityData city, PlayerInventory inventory)
        => quantity > 0 && cityItem?.item != null && city != null && inventory != null
            && city.items != null && city.items.Contains(cityItem);

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
