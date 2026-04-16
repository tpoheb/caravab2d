using UnityEngine;
using System;
using System.Collections.Generic;

public class TradeSystem : MonoBehaviour
{
    // --- ИЗДАТЕЛЬ (События) ---
    public static event Action<City> OnTradeRequest; 

    // --- Зависимости (Системы) ---
    [Header("Системы и Зависимости")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TradeUIManager tradeUIManager; 
    
    // --- НОВОЕ: Глобальная экономика ---
    [Header("Глобальная экономика")]
    [SerializeField] private PlayerToken playerToken; // Ссылка на фишку для отслеживания шагов
    [SerializeField] private List<CityData> allCities = new List<CityData>(); // Список всех городов в игре

    // --- Данные ---
    private City _currentCity; 

    private void Awake()
    {
        // Подписываемся на собственное статическое событие
        OnTradeRequest += OpenTrade; 
        
        // НОВОЕ: Подписываемся на шаги игрока
        if (playerToken != null)
        {
            playerToken.OnStepCompleted += RegenerateGlobalEconomy;
        }
        
        ValidateReferences();
    }

    private void OnDestroy()
    {
        // Обязательная отписка
        OnTradeRequest -= OpenTrade;

        // НОВОЕ: Отписка от шагов
        if (playerToken != null)
        {
            playerToken.OnStepCompleted -= RegenerateGlobalEconomy;
        }
    }

    // --- Главный вход (Вызывается через событие OnTradeRequest) ---
    private void OpenTrade(City city)
    {
        if (city == null || city.CityData == null) 
        {
            Debug.LogError("Попытка открыть торговлю с пустым городом или данными города!");
            return;
        }

        _currentCity = city;
        tradeUIManager.OpenTradePanel(_currentCity.CityData, playerInventory); 
        Debug.Log($"Открытие торговли с городом: {_currentCity.CityName}");
    }

    // --- Методы, вызываемые UI-кнопками ---
    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        if (_currentCity?.CityData == null) return;
        
        TradeTransactionHandler.ProcessBuyTransaction(cityItem, quantity, 
            _currentCity.CityData, playerInventory, playerStats);

        UpdateTradeUI();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        if (_currentCity?.CityData == null) return;

        TradeTransactionHandler.ProcessSellTransaction(cityItem, quantity, 
            _currentCity.CityData, playerInventory, playerStats);
    
        UpdateTradeUI();
    }

    private void UpdateTradeUI()
    {
        tradeUIManager.UpdateMoneyUI(playerInventory, _currentCity.CityData);
        tradeUIManager.RefreshItemStocks(playerInventory);
    }

    public void CloseTrade()
    {
        tradeUIManager.CloseTradePanel();
    }

    // --- НОВОЕ: Метод регенерации экономики ---
    /// <summary>
    /// Вызывается автоматически после каждого шага PlayerToken.
    /// </summary>
    private void RegenerateGlobalEconomy()
    {
        if (allCities == null || allCities.Count == 0) return;

        foreach (var city in allCities)
        {
            if (city == null || city.items == null) continue;

            foreach (var cityItem in city.items)
            {
                cityItem.RegeneratePrice();
            }
        }
        
        // Опционально: можно добавить лог для проверки, что цены обновляются
        // Debug.Log("TradeSystem: Экономика сделала шаг, цены обновлены.");
    }

    private void ValidateReferences()
    {
        if (playerInventory == null) Debug.LogError($"{nameof(PlayerInventory)} не назначен!");
        if (playerStats == null) Debug.LogError($"{nameof(PlayerStats)} не назначен!");
        if (tradeUIManager == null) Debug.LogError($"{nameof(TradeUIManager)} не назначен!");
        // Добавили проверку для новых полей
        if (playerToken == null) Debug.LogWarning("TradeSystem: PlayerToken не назначен, цены не будут обновляться по ходам.");
        if (allCities.Count == 0) Debug.LogWarning("TradeSystem: Список городов пуст, глобальная экономика не будет работать.");
    }

    public static void RequestTrade(City city)
    {
        OnTradeRequest?.Invoke(city);
    }
    
    public CityData CurrentCityData => _currentCity?.CityData;
}