using System.Collections.Generic;
using UnityEngine;

public class TradeUIManager
{
    private TradeItemSystem tradeSystem;
    private List<ItemUI> activeItemUIs = new List<ItemUI>();

    public List<ItemUI> ActiveItemUIs => activeItemUIs;

    public TradeUIManager(TradeItemSystem system)
    {
        tradeSystem = system;
    }

    public void RefreshTradeUI(CityData city, PlayerInventory playerInventory)
    {
        ClearItemUIs();
        
        UpdateCityInfo(city);
        UpdateMoneyUI(playerInventory, city);
        
        CreateItemUIs(city, playerInventory);
    }

    private void UpdateCityInfo(CityData city)
    {
        if (tradeSystem.cityNameText != null)
            tradeSystem.cityNameText.text = city.cityName ?? "Unknown City";
    }

    public void UpdateMoneyUI(PlayerInventory playerInventory, CityData city)
    {
        if (tradeSystem.playerMoneyText != null)
            tradeSystem.playerMoneyText.text = $"{playerInventory.Money}";
            
        if (tradeSystem.cityMoneyText != null && city != null)
            tradeSystem.cityMoneyText.text = $"{city.cityGold}";
    }

    private void CreateItemUIs(CityData city, PlayerInventory playerInventory)
    {
        if (city.items == null || city.items.Count == 0)
        {
            Debug.LogWarning($"City {city.cityName} has no items to trade!");
            return;
        }

        Debug.Log($"Creating UI for {city.items.Count} items");

        foreach (var cityItem in city.items)
        {
            if (ValidateCityItem(cityItem))
            {
                CreateItemUI(cityItem, playerInventory);
            }
        }
    }

    private bool ValidateCityItem(CityData.CityItem cityItem)
    {
        if (cityItem == null)
        {
            Debug.LogError("Found null cityItem in city items list!");
            return false;
        }

        if (cityItem.item == null)
        {
            Debug.LogError("Found cityItem with null item reference!");
            return false;
        }

        return true;
    }

    private void CreateItemUI(CityData.CityItem cityItem, PlayerInventory playerInventory)
    {
        if (tradeSystem.itemUIPrefab == null || tradeSystem.itemsContainer == null)
        {
            Debug.LogError("Item UI prefab or container not set!");
            return;
        }

        int playerStock = playerInventory.GetItemStock(cityItem.item);
        Debug.Log($"Player stock for {cityItem.item.name}: {playerStock}");

        var itemUIObject = Object.Instantiate(tradeSystem.itemUIPrefab, tradeSystem.itemsContainer);
        var itemUI = itemUIObject.GetComponent<ItemUI>();
        
        if (itemUI == null)
        {
            Debug.LogError("Instantiated item doesn't have ItemUI component!");
            Object.Destroy(itemUIObject);
            return;
        }
        
        itemUI.Initialize(
            cityItem,
            playerStock,
            () => tradeSystem.BuyItem(cityItem, 1),
            () => tradeSystem.SellItem(cityItem, 1)
        );
        
        activeItemUIs.Add(itemUI);
        Debug.Log($"Successfully created UI for item: {cityItem.item.name}");
    }

    public void RefreshItemStocks(PlayerInventory playerInventory)
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

    public void ClearItemUIs()
    {
        Debug.Log($"Clearing {activeItemUIs.Count} item UIs");
        foreach (var itemUI in activeItemUIs)
        {
            if(itemUI != null && itemUI.gameObject != null) 
                Object.Destroy(itemUI.gameObject);
        }
        activeItemUIs.Clear();
    }
}