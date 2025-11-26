using UnityEngine;

// Класс-утилита, отвечающий исключительно за проверку условий и выполнение транзакций.
// Не должен быть MonoBehaviour.
public static class TradeTransactionHandler // Сделаем класс статическим
{
    // УДАЛЕН: Конструктор TradeTransactionHandler(TradeSystem system)
    // УДАЛЕН: Приватное поле TradeSystem tradeSystem;

    // Сделаем метод статическим, так как нет зависимостей от экземпляра класса
    public static void ProcessBuyTransaction(CityData.CityItem cityItem, int quantity, 
                                    CityData city, PlayerInventory playerInventory, 
                                    PlayerStats playerStats)
    {
        Debug.Log($"Attempting to buy {quantity} of {cityItem.item.name}");
        
        // ... (логика транзакции остается прежней)
        
        int basePrice = cityItem.buyPrice;
        int finalPrice = CalculateFinalPrice(basePrice, true, playerStats); // Вызываем статический метод
        int totalCost = finalPrice * quantity;
        int totalWeight = cityItem.item.weight * quantity;

        Debug.Log($"Buy details - Base: {basePrice}, Final: {finalPrice}, TotalCost: {totalCost}, Weight: {totalWeight}");

        if (!CanBuy(cityItem, quantity, totalCost, totalWeight, city, playerInventory)) // Вызываем статический метод
            return;

        ExecuteBuyTransaction(cityItem, quantity, totalCost, city, playerInventory); // Вызываем статический метод
    }

    // Сделаем метод статическим
    public static void ProcessSellTransaction(CityData.CityItem cityItem, int quantity, 
                                     CityData city, PlayerInventory playerInventory, 
                                     PlayerStats playerStats)
    {
        Debug.Log($"Attempting to sell {quantity} of {cityItem.item.name}");
        
        // ... (логика транзакции остается прежней)
        
        int basePrice = cityItem.sellPrice;
        int finalPrice = CalculateFinalPrice(basePrice, false, playerStats); // Вызываем статический метод
        int totalValue = finalPrice * quantity;

        Debug.Log($"Sell details - Base: {basePrice}, Final: {finalPrice}, TotalValue: {totalValue}");

        if (!CanSell(cityItem, quantity, totalValue, city, playerInventory)) // Вызываем статический метод
            return;

        ExecuteSellTransaction(cityItem, quantity, totalValue, city, playerInventory); // Вызываем статический метод
    }

    // Сделаем все вспомогательные методы приватными и статическими
    private static int CalculateFinalPrice(int basePrice, bool isBuying, PlayerStats playerStats)
    {
        // ... (логика остается прежней)
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStats is null, using default bargain value");
            return basePrice;
        }

        float bargainEffect = playerStats.Bargain * 0.01f;
        int finalPrice = isBuying 
            ? Mathf.RoundToInt(basePrice * (1f - bargainEffect))
            : Mathf.RoundToInt(basePrice * (1f + bargainEffect));
        
        return Mathf.Max(1, finalPrice); 
    }

    private static bool CanBuy(CityData.CityItem cityItem, int quantity, int totalCost, 
                       int totalWeight, CityData city, PlayerInventory playerInventory)
    {
        // ... (логика остается прежней)
        bool canCarry = playerInventory.CanCarryMore(totalWeight);
        bool playerHasMoney = playerInventory.Money >= totalCost;
        bool cityHasStock = cityItem.stock >= quantity;

        LogTradeValidation("Buy", canCarry, playerHasMoney, cityHasStock, 
                          "Не хватает места в инвентаре!",
                          "Недостаточно денег!",
                          "Недостаточно товара в городе!");

        return canCarry && playerHasMoney && cityHasStock;
    }

    private static bool CanSell(CityData.CityItem cityItem, int quantity, int totalValue, 
                        CityData city, PlayerInventory playerInventory)
    {
        // ... (логика остается прежней)
        bool playerHasItems = playerInventory.GetItemStock(cityItem.item) >= quantity;
        bool cityHasMoney = city.cityGold >= totalValue;

        LogTradeValidation("Sell", playerHasItems, cityHasMoney, true,
                          "Недостаточно товара!",
                          "У города нет денег!",
                          "");

        return playerHasItems && cityHasMoney;
    }

    private static void LogTradeValidation(string tradeType, bool condition1, bool condition2, bool condition3,
                                  string warning1, string warning2, string warning3)
    {
        // ... (логика остается прежней)
        Debug.Log($"CanTrade ({tradeType}) - Condition1: {condition1}, Condition2: {condition2}, Condition3: {condition3}");

        if (!condition1 && !string.IsNullOrEmpty(warning1)) Debug.LogWarning(warning1);
        if (!condition2 && !string.IsNullOrEmpty(warning2)) Debug.LogWarning(warning2);
        if (!condition3 && !string.IsNullOrEmpty(warning3)) Debug.LogWarning(warning3);
    }

    private static void ExecuteBuyTransaction(CityData.CityItem cityItem, int quantity, 
                                     int totalCost, CityData city, PlayerInventory playerInventory)
    {
        // ... (логика остается прежней)
        playerInventory.Money -= totalCost;
        city.cityGold += totalCost;
        cityItem.stock -= quantity;
        playerInventory.AddItem(cityItem.item, quantity);

        Debug.Log($"Bought {quantity} {cityItem.item.name}. Player money: {playerInventory.Money}, City money: {city.cityGold}, Item stock: {cityItem.stock}");
    }

    private static void ExecuteSellTransaction(CityData.CityItem cityItem, int quantity, 
                                      int totalValue, CityData city, PlayerInventory playerInventory)
    {
        // ... (логика остается прежней)
        playerInventory.Money += totalValue;
        city.cityGold -= totalValue;
        cityItem.stock += quantity;
        playerInventory.RemoveItem(cityItem.item, quantity);

        Debug.Log($"Sold {quantity} {cityItem.item.name}. Player money: {playerInventory.Money}, City money: {city.cityGold}, Item stock: {cityItem.stock}");
    }
}