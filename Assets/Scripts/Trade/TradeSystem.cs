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
    
    // ИСПРАВЛЕНИЕ: Храним ссылки на объекты City на сцене, а не на ассеты CityData!
    [SerializeField] private List<City> allCities = new List<City>(); 

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
        // ИСПРАВЛЕНИЕ: Используем RuntimeData вместо CityData
        if (city == null || city.RuntimeData == null) 
        {
            Debug.LogError("Попытка открыть торговлю с пустым городом или данными города!");
            return;
        }

        _currentCity = city;
        tradeUIManager.OpenTradePanel(_currentCity.RuntimeData, playerInventory); 
        Debug.Log($"Открытие торговли с городом: {_currentCity.CityName}");
    }

    // --- Методы, вызываемые UI-кнопками ---
    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        // ИСПРАВЛЕНИЕ: Используем RuntimeData
        if (_currentCity?.RuntimeData == null) return;
        
        TradeTransactionHandler.ProcessBuyTransaction(cityItem, quantity, 
            _currentCity.RuntimeData, playerInventory, playerStats);

        UpdateTradeUI();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        // ИСПРАВЛЕНИЕ: Используем RuntimeData
        if (_currentCity?.RuntimeData == null) return;

        TradeTransactionHandler.ProcessSellTransaction(cityItem, quantity, 
            _currentCity.RuntimeData, playerInventory, playerStats);
    
        UpdateTradeUI();
    }

    private void UpdateTradeUI()
    {
        tradeUIManager.UpdateMoneyUI(playerInventory, _currentCity.RuntimeData);
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
            // ИСПРАВЛЕНИЕ: Проверяем наличие рантайм-копии и обращаемся к ней
            if (city == null || city.RuntimeData == null || city.RuntimeData.items == null) continue;

            foreach (var cityItem in city.RuntimeData.items)
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
        
        if (playerToken == null) Debug.LogWarning("TradeSystem: PlayerToken не назначен, цены не будут обновляться по ходам.");
        if (allCities.Count == 0) Debug.LogWarning("TradeSystem: Список городов пуст, глобальная экономика не будет работать.");
    }

    public static void RequestTrade(City city)
    {
        OnTradeRequest?.Invoke(city);
    }
    
    // ИСПРАВЛЕНИЕ: Возвращаем RuntimeData
    public CityData CurrentCityData => _currentCity?.RuntimeData;
}