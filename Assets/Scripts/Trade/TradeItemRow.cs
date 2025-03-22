using UnityEngine;
using UnityEngine.UI;

public class TradeItemRow : MonoBehaviour
{
    // Поля для UI элементов
    private Text itemNameText;
    private Text cityQuantityText;
    private Text playerQuantityText;
    private InputField quantityInput;
    private Button buyButton;
    private Button sellButton;

    private Item item; // Данные товара

    // Инициализация компонентов
    void Awake()
    {
        // Находим компоненты в дочерних объектах
        itemNameText = transform.Find("ItemNameText").GetComponent<Text>();
        cityQuantityText = transform.Find("CityQuantityText").GetComponent<Text>();
        playerQuantityText = transform.Find("PlayerQuantityText").GetComponent<Text>();
        quantityInput = transform.Find("QuantityInput").GetComponent<InputField>();
        buyButton = transform.Find("BuyButton").GetComponent<Button>();
        sellButton = transform.Find("SellButton").GetComponent<Button>();

        // Привязываем методы к кнопкам
        buyButton.onClick.AddListener(BuyItem);
        sellButton.onClick.AddListener(SellItem);
    }

    // Инициализация строки товара
    public void Initialize(Item item)
    {
        this.item = item;
        UpdateUI();
    }

    // Обновление UI
    public void UpdateUI()
    {
        itemNameText.text = item.itemName;
        cityQuantityText.text = $"В городе: {item.quantityInCity}";
        playerQuantityText.text = $"У игрока: {item.quantityInPlayerInventory}";
    }

    // Покупка товара
    public void BuyItem()
    {
        int quantity = int.Parse(quantityInput.text);
        if (quantity > 0 && item.quantityInCity >= quantity)
        {
            item.quantityInCity -= quantity;
            item.quantityInPlayerInventory += quantity;
            Debug.Log($"Куплено {quantity} единиц товара {item.itemName}");
        }
        else
        {
            Debug.Log("Недостаточно товара в городе или введено неверное количество.");
        }
        UpdateUI();
    }

    // Продажа товара
    public void SellItem()
    {
        int quantity = int.Parse(quantityInput.text);
        if (quantity > 0 && item.quantityInPlayerInventory >= quantity)
        {
            item.quantityInCity += quantity;
            item.quantityInPlayerInventory -= quantity;
            Debug.Log($"Продано {quantity} единиц товара {item.itemName}");
        }
        else
        {
            Debug.Log("Недостаточно товара у игрока или введено неверное количество.");
        }
        UpdateUI();
    }
}