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
    private TradeUIManager uiManager;
    private TradeTransactionHandler transactionHandler;

    private void Start()
    {
        InitializeSystem();
        LogInitializationData();
    }

    private void InitializeSystem()
    {
        uiManager = new TradeUIManager(this);
        transactionHandler = new TradeTransactionHandler(this);
    }

    private void LogInitializationData()
    {
        Debug.Log($"TradeItemSystem started. Cities count: {allCities.Count}");
        if (allCities.Count > 0 && allCities[0] != null)
        {
            Debug.Log($"First city: {allCities[0].cityName}, Items count: {allCities[0].items?.Count ?? 0}");
        }
    }

    public void OpenCityTrade(CityData city)
    {
        if (!ValidateCityData(city)) return;

        currentCity = city;
        tradePanel.SetActive(true);
        RefreshTradeUI();
    }

    private bool ValidateCityData(CityData city)
    {
        if (city == null)
        {
            Debug.LogError("Attempted to open trade with null city!");
            return false;
        }

        if (string.IsNullOrEmpty(city.cityName))
        {
            Debug.LogWarning("City name is empty!");
        }

        Debug.Log($"Opening trade with city: {city.cityName}. Items count: {city.items?.Count ?? 0}");
        return true;
    }

    private void RefreshTradeUI()
    {
        if (currentCity == null)
        {
            Debug.LogError("Current city is null in RefreshTradeUI!");
            return;
        }

        uiManager.RefreshTradeUI(currentCity, playerInventory);
    }

    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        transactionHandler.ProcessBuyTransaction(cityItem, quantity, currentCity, playerInventory, playerStats);
        UpdateAfterTrade();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        transactionHandler.ProcessSellTransaction(cityItem, quantity, currentCity, playerInventory, playerStats);
        UpdateAfterTrade();
    }

    private void UpdateAfterTrade()
    {
        uiManager.UpdateMoneyUI(playerInventory, currentCity);
        uiManager.RefreshItemStocks(playerInventory);
    }

    private void UpdateMoneyUI()
    {
        uiManager.UpdateMoneyUI(playerInventory, currentCity);
    }

    private void RefreshItemUIs()
    {
        uiManager.RefreshItemStocks(playerInventory);
    }

    public void CloseTrade()
    {
        uiManager.ClearItemUIs();
        tradePanel.SetActive(false);
    }

    // Свойства для доступа из других классов
    public List<ItemUI> ActiveItemUIs => uiManager.ActiveItemUIs;
    public CityData CurrentCity => currentCity;
}