using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image icon; // Добавлено поле для иконки
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text cityStock;
    [SerializeField] private TMP_Text cityBuyPrice;
    [SerializeField] private TMP_Text citySellPrice;
    [SerializeField] private TMP_Text playerStock;
    [SerializeField] private TMP_Text playerAveragePriceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private GameObject arrowUp;   // зелёная стрелка ↑ (Image/Sprite)
    [SerializeField] private GameObject arrowDown; // красная стрелка ↓

    // Свойство для доступа к данным (делаем его сериализованным, если нужно для отладки)
    public CityData.CityItem CityItem { get; private set; }

    /// <summary>
    /// Инициализирует UI элемент и привязывает кнопки к TradeSystem.
    /// </summary>
    public void Initialize(
        CityData.CityItem cityItem,
        int currentPlayerStock,
        float averagePrice,
        TradeSystem tradeSystem) // Теперь принимаем TradeSystem
    {
        this.CityItem = cityItem;
        
        // --- Обновление визуальных данных ---
        
        // Отображение иконки
        if (icon != null)
        {
            icon.sprite = cityItem.item.icon;
            // Включаем компонент Image только если иконка существует, 
            // чтобы на экране не было пустого белого квадрата
            icon.enabled = (cityItem.item.icon != null);
        }

        itemName.text = cityItem.item.itemName ?? "Unknown Item";

        // --- Привязка кнопок к TradeSystem ---
        
        // Очищаем предыдущие слушатели (важно при переиспользовании UI-элементов)
        buyButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();
        
        // Привязываем кнопки к методам TradeSystem, используя лямбда-выражения
        // (предполагаем покупку/продажу 1 единицы за раз)
        buyButton.onClick.AddListener(() => tradeSystem.BuyItem(cityItem, 1));
        sellButton.onClick.AddListener(() => tradeSystem.SellItem(cityItem, 1));
        
        RefreshData(currentPlayerStock, averagePrice);
        
        // NOTE: Если вы хотите, чтобы кнопки отключались при нулевом запасе, 
        // эту логику нужно добавить здесь или в UpdatePlayerStock/TradeSystem.
    }

    public void RefreshData(int currentPlayerStock, float averagePrice)
    {
        // Обновляем запасы
        cityStock.text = CityItem.stock.ToString();
        playerStock.text = currentPlayerStock.ToString();

        // Обновляем цены (обращаемся к нашим новым свойствам buyPrice и sellPrice)
        cityBuyPrice.text = CityItem.buyPrice.ToString();
        citySellPrice.text = CityItem.sellPrice.ToString();
        
        // Обновляем текст средней цены (если он привязан в инспекторе)
        if (playerAveragePriceText != null)
        {
            // Форматируем до 2 знаков после запятой ("F1" - это 1 знак, "F2" - 2 знака). 
            // Если товара нет (цена 0), пишем прочерк.
            playerAveragePriceText.text = averagePrice > 0 ? averagePrice.ToString("F1") : "-";
        }
        
        // ─── Спрос ────────────────────────────────────────────────────────────
        if (arrowUp != null)   arrowUp.SetActive(CityItem.demand == DemandLevel.High);
        if (arrowDown != null) arrowDown.SetActive(CityItem.demand == DemandLevel.Low);
        // ────
    }

    /// <summary>
    /// Обновляет количество этого предмета у игрока.
    /// </summary>
    public void UpdatePlayerStock(int newStock)
    {
        playerStock.text = newStock.ToString();
    }
}