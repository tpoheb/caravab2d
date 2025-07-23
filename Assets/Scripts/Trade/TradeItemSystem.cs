using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Для использования ToList() при логировании

public class TradeItemSystem : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;
    public PlayerStats playerStats;
    public List<CityData> allCities = new List<CityData>();
    
    [Header("UI Elements")]
    public Text playerMoneyText;
    public Text cityMoneyText;
    public Text cityNameText;
    public GameObject tradePanel;
    public Transform itemsContainer;
    public GameObject itemUIPrefab;

    private CityData currentCity;
    private List<ItemUI> activeItemUIs = new List<ItemUI>();

    private void Start()
    {
        // Логирование при старте для проверки инициализации
        Debug.Log($"TradeItemSystem started. Cities count: {allCities.Count}");
        if (allCities.Count > 0)
        {
            Debug.Log($"First city items: {string.Join(", ", allCities[0].items.Select(i => i.item.name))}");
        }
    }

    public void OpenCityTrade(CityData city)
    {
        if (city == null)
        {
            Debug.LogError("Attempted to open trade with null city!");
            return;
        }

        Debug.Log($"Opening trade with city: {city.cityName}. Items count: {city.items.Count}");
        if (city.items.Count == 0)
        {
            Debug.LogWarning($"City {city.cityName} has no items to trade!");
        }

        currentCity = city;
        tradePanel.SetActive(true);
        RefreshTradeUI();
    }

    private void RefreshTradeUI()
    {
        ClearItemUIs();
        
        if (currentCity == null)
        {
            Debug.LogError("Current city is null in RefreshTradeUI!");
            return;
        }

        cityNameText.text = currentCity.cityName;
        UpdateMoneyUI();

        Debug.Log($"Refreshing UI for city: {currentCity.cityName}. Items count: {currentCity.items.Count}");
        
        foreach (var cityItem in currentCity.items)
        {
            if (cityItem == null || cityItem.item == null)
            {
                Debug.LogError("Found null cityItem or cityItem.item in city items list!");
                continue;
            }
            
            Debug.Log($"Creating UI for item: {cityItem.item.name}, stock: {cityItem.stock}, buyPrice: {cityItem.buyPrice}, sellPrice: {cityItem.sellPrice}");
            CreateItemUI(cityItem);
        }
    }

    private void CreateItemUI(CityData.CityItem cityItem)
    {
        if (itemUIPrefab == null || itemsContainer == null)
        {
            Debug.LogError("Item UI prefab or container not set!");
            return;
        }

        int playerStock = playerInventory.GetItemStock(cityItem.item);
        Debug.Log($"Player stock for {cityItem.item.name}: {playerStock}");

        var itemUI = Instantiate(itemUIPrefab, itemsContainer).GetComponent<ItemUI>();
        if (itemUI == null)
        {
            Debug.LogError("Instantiated item doesn't have ItemUI component!");
            return;
        }
        
        itemUI.Initialize(
            cityItem,
            playerStock,
            () => BuyItem(cityItem, 1),
            () => SellItem(cityItem, 1)
        );
        
        activeItemUIs.Add(itemUI);
        Debug.Log($"Successfully created UI for item: {cityItem.item.name}");
    }

    private void ClearItemUIs()
    {
        Debug.Log($"Clearing {activeItemUIs.Count} item UIs");
        foreach (var itemUI in activeItemUIs)
        {
            if(itemUI != null && itemUI.gameObject != null) 
                Destroy(itemUI.gameObject);
        }
        activeItemUIs.Clear();
    }

    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        Debug.Log($"Attempting to buy {quantity} of {cityItem.item.name}");
        
        int basePrice = cityItem.buyPrice;
        int finalPrice = CalculateFinalPrice(basePrice, true);
        int totalCost = finalPrice * quantity;
        int totalWeight = cityItem.item.weight * quantity;

        Debug.Log($"Buy details - Base: {basePrice}, Final: {finalPrice}, TotalCost: {totalCost}, Weight: {totalWeight}");

        if (!CanTrade(cityItem, quantity, totalCost, totalWeight, true)) 
            return;

        // Совершаем сделку
        playerInventory.Money -= totalCost;
        currentCity.cityGold += totalCost;
        cityItem.stock -= quantity;
        playerInventory.AddItem(cityItem.item, quantity);

        Debug.Log($"Bought {quantity} {cityItem.item.name}. Player money: {playerInventory.Money}, City money: {currentCity.cityGold}, Item stock: {cityItem.stock}");

        UpdateAfterTrade();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        Debug.Log($"Attempting to sell {quantity} of {cityItem.item.name}");
        
        int basePrice = cityItem.sellPrice;
        int finalPrice = CalculateFinalPrice(basePrice, false);
        int totalValue = finalPrice * quantity;

        Debug.Log($"Sell details - Base: {basePrice}, Final: {finalPrice}, TotalValue: {totalValue}");

        if (!CanTrade(cityItem, quantity, totalValue, 0, false)) 
            return;

        // Совершаем сделку
        playerInventory.Money += totalValue;
        currentCity.cityGold -= totalValue;
        cityItem.stock += quantity;
        playerInventory.RemoveItem(cityItem.item, quantity);

        Debug.Log($"Sold {quantity} {cityItem.item.name}. Player money: {playerInventory.Money}, City money: {currentCity.cityGold}, Item stock: {cityItem.stock}");

        UpdateAfterTrade();
    }

    private int CalculateFinalPrice(int basePrice, bool isBuying)
    {
        float bargainEffect = playerStats.Bargain * 0.01f;
        int finalPrice = isBuying 
            ? Mathf.RoundToInt(basePrice * (1f - bargainEffect))
            : Mathf.RoundToInt(basePrice * (1f + bargainEffect));
        
        Debug.Log($"Price calculation - Base: {basePrice}, Bargain: {bargainEffect}, Final: {finalPrice}, IsBuying: {isBuying}");
        return finalPrice;
    }

    private bool CanTrade(CityData.CityItem cityItem, int quantity, int totalMoney, int totalWeight, bool isBuying)
    {
        if (isBuying)
        {
            bool canCarry = playerInventory.CanCarryMore(totalWeight);
            bool playerHasMoney = playerInventory.Money >= totalMoney;
            bool cityHasStock = cityItem.stock >= quantity;

            Debug.Log($"CanTrade (Buy) - CanCarry: {canCarry}, HasMoney: {playerHasMoney}, HasStock: {cityHasStock}");

            if (!canCarry) Debug.LogWarning("Не хватает места в инвентаре!");
            if (!playerHasMoney) Debug.LogWarning("Недостаточно денег!");
            if (!cityHasStock) Debug.LogWarning("Недостаточно товара в городе!");

            return canCarry && playerHasMoney && cityHasStock;
        }
        else
        {
            bool playerHasItems = playerInventory.GetItemStock(cityItem.item) >= quantity;
            bool cityHasMoney = currentCity.cityGold >= totalMoney;

            Debug.Log($"CanTrade (Sell) - HasItems: {playerHasItems}, CityHasMoney: {cityHasMoney}");

            if (!playerHasItems) Debug.LogWarning("Недостаточно товара!");
            if (!cityHasMoney) Debug.LogWarning("У города нет денег!");

            return playerHasItems && cityHasMoney;
        }
    }

    private void UpdateAfterTrade()
    {
        UpdateMoneyUI();
        RefreshItemUIs();
    }

    private void UpdateMoneyUI()
    {
        playerMoneyText.text = $"Деньги: {playerInventory.Money}";
        cityMoneyText.text = $"Город: {currentCity.cityGold}";
    }

    private void RefreshItemUIs()
    {
        Debug.Log("Refreshing item UIs");
        foreach (var itemUI in activeItemUIs)
        {
            if (itemUI == null) continue;
            
            int playerStock = playerInventory.GetItemStock(itemUI.CityItem.item);
            itemUI.UpdatePlayerStock(playerStock);
            Debug.Log($"Refreshed UI for {itemUI.CityItem.item.name}, stock: {playerStock}");
        }
    }

    public void CloseTrade()
    {
        Debug.Log("Closing trade panel");
        tradePanel.SetActive(false);
        ClearItemUIs();
    }
}