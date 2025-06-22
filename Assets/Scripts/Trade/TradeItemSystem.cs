using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public void OpenCityTrade(CityData city)
    {
        currentCity = city;
        tradePanel.SetActive(true);
        RefreshTradeUI();
    }

    private void RefreshTradeUI()
    {
        ClearItemUIs();
        
        cityNameText.text = currentCity.cityName;
        UpdateMoneyUI();

        foreach (var cityItem in currentCity.items)
        {
            CreateItemUI(cityItem);
        }
    }

    private void CreateItemUI(CityData.CityItem cityItem)
    {
        var itemUI = Instantiate(itemUIPrefab, itemsContainer).GetComponent<ItemUI>();
        int playerStock = playerInventory.GetItemStock(cityItem.item);
        
        itemUI.Initialize(
            cityItem,
            playerStock,
            () => BuyItem(cityItem, 1),  // Купить 1
            () => SellItem(cityItem, 1)   // Продать 1
        );
        
        activeItemUIs.Add(itemUI);
    }

    private void ClearItemUIs()
    {
        foreach (var itemUI in activeItemUIs)
        {
            if(itemUI != null) Destroy(itemUI.gameObject);
        }
        activeItemUIs.Clear();
    }

    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        int basePrice = cityItem.buyPrice;
        int finalPrice = CalculateFinalPrice(basePrice, true);
        int totalCost = finalPrice * quantity;
        int totalWeight = cityItem.item.weight * quantity;

        if (!CanTrade(cityItem, quantity, totalCost, totalWeight, true)) 
            return;

        // Совершаем сделку
        playerInventory.Money -= totalCost;
        currentCity.cityGold += totalCost;
        cityItem.stock -= quantity;
        playerInventory.AddItem(cityItem.item, quantity);

        UpdateAfterTrade();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        int basePrice = cityItem.sellPrice;
        int finalPrice = CalculateFinalPrice(basePrice, false);
        int totalValue = finalPrice * quantity;

        if (!CanTrade(cityItem, quantity, totalValue, 0, false)) 
            return;

        // Совершаем сделку
        playerInventory.Money += totalValue;
        currentCity.cityGold -= totalValue;
        cityItem.stock += quantity;
        playerInventory.RemoveItem(cityItem.item, quantity);

        UpdateAfterTrade();
    }

    private int CalculateFinalPrice(int basePrice, bool isBuying)
    {
        float bargainEffect = playerStats.Bargain * 0.01f;
        return isBuying 
            ? Mathf.RoundToInt(basePrice * (1f - bargainEffect)) // Скидка при покупке
            : Mathf.RoundToInt(basePrice * (1f + bargainEffect)); // Наценка при продаже
    }

    private bool CanTrade(CityData.CityItem cityItem, int quantity, int totalMoney, int totalWeight, bool isBuying)
    {
        if (isBuying)
        {
            bool canCarry = playerInventory.CanCarryMore(totalWeight);
            bool playerHasMoney = playerInventory.Money >= totalMoney;
            bool cityHasStock = cityItem.stock >= quantity;

            if (!canCarry) Debug.Log("Не хватает места в инвентаре!");
            if (!playerHasMoney) Debug.Log("Недостаточно денег!");
            if (!cityHasStock) Debug.Log("Недостаточно товара в городе!");

            return canCarry && playerHasMoney && cityHasStock;
        }
        else
        {
            bool playerHasItems = playerInventory.GetItemStock(cityItem.item) >= quantity;
            bool cityHasMoney = currentCity.cityGold >= totalMoney;

            if (!playerHasItems) Debug.Log("Недостаточно товара!");
            if (!cityHasMoney) Debug.Log("У города нет денег!");

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
        foreach (var itemUI in activeItemUIs)
        {
            int playerStock = playerInventory.GetItemStock(itemUI.CityItem.item);
            itemUI.UpdatePlayerStock(playerStock);
        }
    }

    public void CloseTrade()
    {
        tradePanel.SetActive(false);
        ClearItemUIs();
    }

    // Для кнопок +/-
    //public void BuyMax(CityData.CityItem cityItem) => BuyItem(cityItem, GetMaxTradable(cityItem, true));
    // public void SellMax(CityData.CityItem cityItem) => SellItem(cityItem, GetMaxTradable(cityItem, false));
    //
    // private int GetMaxTradable(CityData.CityItem cityItem, bool isBuying)
    // {
    //     if (isBuying)
    //     {
    //         int byMoney = playerInventory.Money / CalculateFinalPrice(cityItem.buyPrice, true);
    //         int byStock = cityItem.stock;
    //         float carryCapacity = playerInventory.RemainingCarryCapacity() / cityItem.item.weight;
    //         return Mathf.Min(byMoney, byStock, Mathf.FloorToInt(carryCapacity));
    //     }
    //     else
    //     {
    //         int byPlayerStock = playerInventory.GetItemStock(cityItem.item);
    //         int byCityMoney = currentCity.cityGold / CalculateFinalPrice(cityItem.sellPrice, false);
    //         return Mathf.Min(byPlayerStock, byCityMoney);
    //     }
    // }
}