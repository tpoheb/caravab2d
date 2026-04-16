using System.Collections.Generic;
using UnityEngine;
using TMPro; // Добавьте для работы с TMP_Text
using UnityEngine.UI; // Добавьте для работы с Button

// TradeUIManager теперь является компонентом MonoBehaviour
public class TradeUIManager : MonoBehaviour
{
    // --- UI Elements (Сериализуются в Инспекторе этого скрипта) ---
    [Header("UI Elements")]
    [SerializeField] private TMP_Text playerMoneyText;
    [SerializeField] private TMP_Text cityMoneyText;
    [SerializeField] private TMP_Text cityNameText;
    [SerializeField] private GameObject tradePanel; // Панель, которую нужно включать/выключать
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private Button closeTradeButton; // НОВОЕ ПОЛЕ: Кнопка "Закрыть"
    
    // --- Системная ссылка (Требуется для вызова логики Buy/Sell) ---
    // Используем TradeSystem (рефакторинг TradeItemSystem)
    [Header("System References")]
    [SerializeField] private TradeSystem tradeSystem; 

    private readonly List<ItemUI> _activeItemUIs = new List<ItemUI>();

    // Свойство для внешнего доступа (ActiveItemUIs)
    public List<ItemUI> ActiveItemUIs => _activeItemUIs; 
    
    public static event System.Action OnTradeClosedRequest;

    private void Awake()
    {
        // Проверка необходимых ссылок
        ValidateReferences();
        if (closeTradeButton != null)
        {
            // Привязываем кнопку "Закрыть" к методу, который закрывает UI и издает событие
            closeTradeButton.onClick.RemoveAllListeners();
            closeTradeButton.onClick.AddListener(OnCloseTradeButtonClicked);
        }
        
        tradePanel.SetActive(false); 
    
        // Панель должна быть закрыта по умолчанию
        tradePanel.SetActive(false); 
        
    }
    
    // Новая функция для проверки ссылок (лучшая практика)
    private void ValidateReferences()
    {
        if (tradeSystem == null) Debug.LogError($"{nameof(TradeSystem)} не назначен в {nameof(TradeUIManager)}!");
        if (tradePanel == null) Debug.LogError($"{nameof(tradePanel)} не назначен!");
        if (itemsContainer == null) Debug.LogError($"{nameof(itemsContainer)} не назначен!");
        if (itemUIPrefab == null) Debug.LogError($"{nameof(itemUIPrefab)} не назначен!");
        if (closeTradeButton == null) Debug.LogError($"{nameof(closeTradeButton)} не назначен в {nameof(TradeUIManager)}!");
    }
    
    // --- Методы управления панелью ---

    public void OpenTradePanel(CityData cityData, PlayerInventory inventory)
    {
        if (cityData == null || inventory == null) return;

        tradePanel.SetActive(true);
        
        RefreshTradeUI(cityData, inventory);
    }
    
    public void CloseTradePanel()
    {
        ClearItemUIs();
        tradePanel.SetActive(false);
    }
    private void OnCloseTradeButtonClicked()
    {
        // 1. Закрываем саму панель торговли
        CloseTradePanel();
        
        // 2. ИЗДАЕМ СОБЫТИЕ: сообщаем всем, что нужно открыть панель города.
        OnTradeClosedRequest?.Invoke(); 
        
        Debug.Log("TradeUIManager: Запрос на открытие панели города.");
    }

    // --- Центральный метод обновления ---

    /// <summary>
    /// Обновляет весь UI для нового города.
    /// </summary>
    public void RefreshTradeUI(CityData city, PlayerInventory playerInventory)
    {
        ClearItemUIs();
        
        UpdateCityInfo(city);
        UpdateMoneyUI(playerInventory, city);
        
        CreateItemUIs(city, playerInventory);
    }
    
    // --- Обновление данных ---

    private void UpdateCityInfo(CityData city)
    {
        if (cityNameText != null)
            cityNameText.text = city.cityName ?? "Unknown City";
    }

    public void UpdateMoneyUI(PlayerInventory playerInventory, CityData city)
    {
        if (playerMoneyText != null)
            playerMoneyText.text = $"{playerInventory.Money}";
            
        // Примечание: предполагается, что CityData имеет поле cityGold
        if (cityMoneyText != null && city != null) 
            cityMoneyText.text = $"{city.cityGold}"; 
    }

    public void RefreshItemStocks(PlayerInventory playerInventory)
    {
        Debug.Log("Refreshing item UIs stocks and prices");
        foreach (var itemUI in _activeItemUIs)
        {
            if (itemUI == null) continue;
            
            // Получаем запасы игрока
            int playerStock = playerInventory.GetItemStock(itemUI.CityItem.item); 
            
            // НОВОЕ: Получаем среднюю цену из инвентаря
            float avgPrice = playerInventory.GetItemAveragePrice(itemUI.CityItem.item);
            
            // Передаем оба значения в UI
            itemUI.RefreshData(playerStock, avgPrice); 
        }
    }

    // --- Построение UI ---

    private void CreateItemUIs(CityData city, PlayerInventory playerInventory)
    {
        if (city.items == null || city.items.Count == 0)
        {
            Debug.LogWarning($"City {city.cityName} has no items to trade!");
            return;
        }

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
        if (cityItem == null || cityItem.item == null)
        {
            Debug.LogError("Found null CityItem or null Item reference!");
            return false;
        }
        return true;
    }

    private void CreateItemUI(CityData.CityItem cityItem, PlayerInventory playerInventory)
    {
        // Проверка ссылок уже сделана в Awake
        
        int playerStock = playerInventory.GetItemStock(cityItem.item);
        float avgPrice = playerInventory.GetItemAveragePrice(cityItem.item);

        var itemUIObject = Instantiate(itemUIPrefab, itemsContainer);
        var itemUI = itemUIObject.GetComponent<ItemUI>();
        
        if (itemUI == null)
        {
            Debug.LogError($"Instantiated item UI prefab '{itemUIPrefab.name}' is missing the ItemUI component!");
            Destroy(itemUIObject);
            return;
        }
        
        // --- Прямая связь с TradeSystem ---
        // ItemUI вызывает BuyItem/SellItem через TradeSystem, который обрабатывает транзакцию.
        // ЭТОТ МЕТОД ЛУЧШЕ, ЧЕМ ПЕРЕДАЧА ЛЯМБДЫ (TRADE SYSTEM должен иметь ссылку)
        
        // Предполагается, что ItemUI.Initialize принимает TradeSystem
        itemUI.Initialize(
            cityItem,
            playerStock,
            avgPrice,
            tradeSystem // Передаем TradeSystem напрямую
        ); 
        
        _activeItemUIs.Add(itemUI);
        // Debug.Log($"Successfully created UI for item: {cityItem.item.name}");
    }

    public void ClearItemUIs()
    {
        Debug.Log($"Clearing {_activeItemUIs.Count} item UIs");
        
        // Использование цикла for-backwards для безопасного удаления
        for (int i = _activeItemUIs.Count - 1; i >= 0; i--)
        {
            var ui = _activeItemUIs[i];
            if(ui != null && ui.gameObject != null) 
                Destroy(ui.gameObject);
        }
        _activeItemUIs.Clear();
    }
    public void SetSystem(TradeSystem system)
    {
        this.tradeSystem = system;
    }
    
}