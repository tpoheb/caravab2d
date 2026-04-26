using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    //public Image icon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text cityStock;
    [SerializeField] private TMP_Text cityBuyPrice;
    [SerializeField] private TMP_Text citySellPrice;
    [SerializeField] private TMP_Text playerStock;
    [SerializeField] private TMP_Text playerAveragePriceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;

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
        
        //icon.sprite = cityItem.item.icon;
        itemName.text = cityItem.item.itemName ?? "Unknown Item";
        /*cityStock.text = cityItem.stock.ToString();
        cityBuyPrice.text = cityItem.buyPrice.ToString();
        citySellPrice.text = cityItem.sellPrice.ToString();
        playerStock.text = currentPlayerStock.ToString();*/

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
            // Форматируем до 2 знаков после запятой ("F1"). 
            // Если товара нет (цена 0), пишем прочерк.
            playerAveragePriceText.text = averagePrice > 0 ? averagePrice.ToString("F1") : "-";
        }
    }
    /// <summary>
    /// Обновляет количество этого предмета у игрока.
    /// </summary>
    public void UpdatePlayerStock(int newStock)
    {
        playerStock.text = newStock.ToString();
    }
}