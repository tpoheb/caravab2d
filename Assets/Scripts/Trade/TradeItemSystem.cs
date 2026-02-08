using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradeItemSystem : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;
    public PlayerStats playerStats;
    public List<CityData> allCities = new List<CityData>();
    
    [Header("UI Elements")]
    public TMP_Text playerMoneyText;
    public TMP_Text cityMoneyText;
    public TMP_Text cityNameText;
    public GameObject tradePanel;
    public Transform itemsContainer;
    public GameObject itemUIPrefab;

    private CityData currentCity;
    private TradeUIManager uiManager;

    // УДАЛЕНО: private TradeTransactionHandler transactionHandler; 
    // (Статический класс нельзя объявлять как переменную)

    private void Start()
    {
        InitializeSystem();
        LogInitializationData();
    }

    private void InitializeSystem()
    {
        //uiManager = new TradeUIManager(this);
        // УДАЛЕНО: transactionHandler = new TradeTransactionHandler(this);
        if (uiManager == null) 
            Debug.LogError("Пожалуйста, перетащите объект с TradeUIManager в поле UI Manager!");
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
        Debug.Log($"Opening trade with city: {city.cityName}. Items count: {city.items?.Count ?? 0}");
        return true;
    }

    private void RefreshTradeUI()
    {
        if (currentCity == null) return;
        uiManager.RefreshTradeUI(currentCity, playerInventory);
    }

    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        // ИЗМЕНЕНО: Обращаемся напрямую к статическому классу TradeTransactionHandler
        TradeTransactionHandler.ProcessBuyTransaction(
            cityItem, 
            quantity, 
            currentCity, 
            playerInventory, 
            playerStats
        );
        
        UpdateAfterTrade();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        // ИЗМЕНЕНО: Обращаемся напрямую к статическому классу TradeTransactionHandler
        TradeTransactionHandler.ProcessSellTransaction(
            cityItem, 
            quantity, 
            currentCity, 
            playerInventory, 
            playerStats
        );
        
        UpdateAfterTrade();
    }

    private void UpdateAfterTrade()
    {
        uiManager.UpdateMoneyUI(playerInventory, currentCity);
        uiManager.RefreshItemStocks(playerInventory);
    }

    public void CloseTrade()
    {
        uiManager.ClearItemUIs();
        tradePanel.SetActive(false);
    }

    public List<ItemUI> ActiveItemUIs => uiManager.ActiveItemUIs;
    public CityData CurrentCity => currentCity;
}