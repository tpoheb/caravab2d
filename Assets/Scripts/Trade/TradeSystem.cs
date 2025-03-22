using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TradeSystem : MonoBehaviour
{
    public TradeData tradeData;
    public GameObject tradePanel;
    public Text cityQuantityText;
    public Text playerQuantityText;
    public InputField quantityInput;
    public Text selectedItemText;

    private Item selectedItem;

    void Start()
    {
        tradePanel.SetActive(false);
    }

    public void OnBuyGoodsClicked()
    {
        OpenTradePanel();
    }
    public void OpenTradePanel()
    {
        tradePanel.SetActive(true);
        SelectItem(0); // Выбираем первый товар по умолчанию
    }

    public void CloseTradePanel()
    {
        tradePanel.SetActive(false);
    }

    public void SelectItem(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < tradeData.items.Length)
        {
            selectedItem = tradeData.items[itemIndex];
            UpdateUI();
        }
    }

    public void BuyItem()
    {
        int quantity = int.Parse(quantityInput.text);
        if (quantity > 0 && selectedItem.quantityInCity >= quantity)
        {
            selectedItem.quantityInCity -= quantity;
            selectedItem.quantityInPlayerInventory += quantity;
            Debug.Log($"Куплено {quantity} единиц товара {selectedItem.itemName}");
        }
        else
        {
            Debug.Log("Недостаточно товара в городе или введено неверное количество.");
        }
        UpdateUI();
    }

    public void SellItem()
    {
        int quantity = int.Parse(quantityInput.text);
        if (quantity > 0 && selectedItem.quantityInPlayerInventory >= quantity)
        {
            selectedItem.quantityInCity += quantity;
            selectedItem.quantityInPlayerInventory -= quantity;
            Debug.Log($"Продано {quantity} единиц товара {selectedItem.itemName}");
        }
        else
        {
            Debug.Log("Недостаточно товара у игрока или введено неверное количество.");
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        cityQuantityText.text = $"В городе: {selectedItem.quantityInCity}";
        playerQuantityText.text = $"У игрока: {selectedItem.quantityInPlayerInventory}";
        selectedItemText.text = selectedItem.itemName;
    }
}