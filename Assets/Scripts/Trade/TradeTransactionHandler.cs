using UnityEngine;

public class TradeTransactionHandler
{
    private TradeItemSystem tradeSystem;

    public TradeTransactionHandler(TradeItemSystem system)
    {
        tradeSystem = system;
    }

    public void ProcessBuyTransaction(CityData.CityItem cityItem, int quantity, 
                                    CityData city, PlayerInventory playerInventory, 
                                    PlayerStats playerStats)
    {
        Debug.Log($"Attempting to buy {quantity} of {cityItem.item.name}");
        
        int basePrice = cityItem.buyPrice;
        int finalPrice = CalculateFinalPrice(basePrice, true, playerStats);
        int totalCost = finalPrice * quantity;
        int totalWeight = cityItem.item.weight * quantity;

        Debug.Log($"Buy details - Base: {basePrice}, Final: {finalPrice}, TotalCost: {totalCost}, Weight: {totalWeight}");

        if (!CanBuy(cityItem, quantity, totalCost, totalWeight, city, playerInventory)) 
            return;

        ExecuteBuyTransaction(cityItem, quantity, totalCost, city, playerInventory);
    }

    public void ProcessSellTransaction(CityData.CityItem cityItem, int quantity, 
                                     CityData city, PlayerInventory playerInventory, 
                                     PlayerStats playerStats)
    {
        Debug.Log($"Attempting to sell {quantity} of {cityItem.item.name}");
        
        int basePrice = cityItem.sellPrice;
        int finalPrice = CalculateFinalPrice(basePrice, false, playerStats);
        int totalValue = finalPrice * quantity;

        Debug.Log($"Sell details - Base: {basePrice}, Final: {finalPrice}, TotalValue: {totalValue}");

        if (!CanSell(cityItem, quantity, totalValue, city, playerInventory)) 
            return;

        ExecuteSellTransaction(cityItem, quantity, totalValue, city, playerInventory);
    }

    private int CalculateFinalPrice(int basePrice, bool isBuying, PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStats is null, using default bargain value");
            return basePrice;
        }

        float bargainEffect = playerStats.Bargain * 0.01f;
        int finalPrice = isBuying 
            ? Mathf.RoundToInt(basePrice * (1f - bargainEffect))
            : Mathf.RoundToInt(basePrice * (1f + bargainEffect));
        
        Debug.Log($"Price calculation - Base: {basePrice}, Bargain: {bargainEffect}, Final: {finalPrice}, IsBuying: {isBuying}");
        return Mathf.Max(1, finalPrice); // Минимальная цена 1
    }

    private bool CanBuy(CityData.CityItem cityItem, int quantity, int totalCost, 
                       int totalWeight, CityData city, PlayerInventory playerInventory)
    {
        bool canCarry = playerInventory.CanCarryMore(totalWeight);
        bool playerHasMoney = playerInventory.Money >= totalCost;
        bool cityHasStock = cityItem.stock >= quantity;

        LogTradeValidation("Buy", canCarry, playerHasMoney, cityHasStock, 
                          "Не хватает места в инвентаре!",
                          "Недостаточно денег!",
                          "Недостаточно товара в городе!");

        return canCarry && playerHasMoney && cityHasStock;
    }

    private bool CanSell(CityData.CityItem cityItem, int quantity, int totalValue, 
                        CityData city, PlayerInventory playerInventory)
    {
        bool playerHasItems = playerInventory.GetItemStock(cityItem.item) >= quantity;
        bool cityHasMoney = city.cityGold >= totalValue;

        LogTradeValidation("Sell", playerHasItems, cityHasMoney, true,
                          "Недостаточно товара!",
                          "У города нет денег!",
                          "");

        return playerHasItems && cityHasMoney;
    }

    private void LogTradeValidation(string tradeType, bool condition1, bool condition2, bool condition3,
                                  string warning1, string warning2, string warning3)
    {
        Debug.Log($"CanTrade ({tradeType}) - Condition1: {condition1}, Condition2: {condition2}, Condition3: {condition3}");

        if (!condition1 && !string.IsNullOrEmpty(warning1)) Debug.LogWarning(warning1);
        if (!condition2 && !string.IsNullOrEmpty(warning2)) Debug.LogWarning(warning2);
        if (!condition3 && !string.IsNullOrEmpty(warning3)) Debug.LogWarning(warning3);
    }

    private void ExecuteBuyTransaction(CityData.CityItem cityItem, int quantity, 
                                     int totalCost, CityData city, PlayerInventory playerInventory)
    {
        playerInventory.Money -= totalCost;
        city.cityGold += totalCost;
        cityItem.stock -= quantity;
        playerInventory.AddItem(cityItem.item, quantity);

        Debug.Log($"Bought {quantity} {cityItem.item.name}. Player money: {playerInventory.Money}, City money: {city.cityGold}, Item stock: {cityItem.stock}");
    }

    private void ExecuteSellTransaction(CityData.CityItem cityItem, int quantity, 
                                      int totalValue, CityData city, PlayerInventory playerInventory)
    {
        playerInventory.Money += totalValue;
        city.cityGold -= totalValue;
        cityItem.stock += quantity;
        playerInventory.RemoveItem(cityItem.item, quantity);

        Debug.Log($"Sold {quantity} {cityItem.item.name}. Player money: {playerInventory.Money}, City money: {city.cityGold}, Item stock: {cityItem.stock}");
    }
}