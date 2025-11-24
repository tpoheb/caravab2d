using UnityEngine;
using System;
using System.Collections.Generic;

// Этот класс отвечает за управление состоянием торговли и обработку событий.
public class TradeSystem : MonoBehaviour
{
    // --- ИЗДАТЕЛЬ (События) ---
    // Событие, которое могут вызвать другие классы (например, CityPanel), чтобы открыть торговлю.
    // CityPanel должен вызывать это событие, когда игрок нажимает "Купить товары".
    public static event Action<City> OnTradeRequest; // ИСПОЛЬЗУЕМ КЛАСС CITY, А НЕ CityData

    // --- Зависимости (Системы) ---
    [Header("Системы и Зависимости")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TradeUIManager tradeUIManager; // Теперь отдельный компонент
    [SerializeField] private TradeTransactionHandler transactionHandler; // Теперь отдельный компонент
    
    // --- Данные ---
    // Используем City, так как это компонент на сцене, а CityData, вероятно, ScriptableObject для товаров.
    private City _currentCity; 

    private void Awake()
    {
        // Подписываемся на собственное статическое событие
        OnTradeRequest += OpenTrade; 
        
        ValidateReferences();
    }

    private void OnDestroy()
    {
        // Обязательная отписка
        OnTradeRequest -= OpenTrade;
    }

    // --- Главный вход (Вызывается через событие OnTradeRequest) ---
    /// <summary>
    /// Открывает торговую панель для указанного города.
    /// </summary>
    private void OpenTrade(City city)
    {
        // ВАЖНО: Предполагаем, что City.cs содержит ссылку на CityData
        if (city == null || city.CityData == null) // Требуется, чтобы в City.cs было поле public CityData CityData
        {
            Debug.LogError("Попытка открыть торговлю с пустым городом или данными города!");
            return;
        }

        _currentCity = city;
        
        // Делегируем открытие и построение UI менеджеру
        tradeUIManager.OpenTradePanel(_currentCity.CityData, playerInventory); 
        
        Debug.Log($"Открытие торговли с городом: {_currentCity.CityName}");
    }

    // --- Методы, вызываемые UI-кнопками ---
    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        if (_currentCity?.CityData == null) return;
        
        transactionHandler.ProcessBuyTransaction(cityItem, quantity, _currentCity.CityData, playerInventory, playerStats);
        
        UpdateTradeUI();
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        if (_currentCity?.CityData == null) return;

        transactionHandler.ProcessSellTransaction(cityItem, quantity, _currentCity.CityData, playerInventory, playerStats);
        
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

    // ... (Остальные методы, такие как ValidateReferences, остаются для проверки инспектора)
    
    private void ValidateReferences()
    {
        if (playerInventory == null) Debug.LogError($"{nameof(PlayerInventory)} не назначен!");
        if (playerStats == null) Debug.LogError($"{nameof(PlayerStats)} не назначен!");
        if (tradeUIManager == null) Debug.LogError($"{nameof(TradeUIManager)} не назначен!");
        if (transactionHandler == null) Debug.LogError($"{nameof(TradeTransactionHandler)} не назначен!");
    }
    public static void RequestTrade(City city)
    {
        // Безопасный вызов события ИЗНУТРИ класса TradeSystem.
        OnTradeRequest?.Invoke(city);
        Debug.Log($"TradeSystem: Получен запрос на торговлю в городе {city.CityName}");
    }
    
    // Свойства для доступа
    public CityData CurrentCityData => _currentCity?.CityData;
}