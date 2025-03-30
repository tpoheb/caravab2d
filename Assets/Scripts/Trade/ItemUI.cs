using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text itemNameText;
    public Text buyPriceText;
    public Text sellPriceText;
    public Text cityStockText;
    public Text playerStockText;
    public Button buyButton;
    public Button sellButton;

    public void Initialize(CityData.CityItem cityItem, int playerStock, TradeItemSystem tradeSystem)
    {
        itemNameText.text = cityItem.item.itemName;
        buyPriceText.text = $" {cityItem.buyPrice}";
        sellPriceText.text = $" {cityItem.sellPrice}";
        cityStockText.text = $"� ������: {cityItem.stock}";
        playerStockText.text = $"� ���: {playerStock}";

        buyButton.onClick.AddListener(() => tradeSystem.BuyItem(cityItem, 1));
        sellButton.onClick.AddListener(() => tradeSystem.SellItem(cityItem, 1));

        // ���������� ������ ���� ������ ��������� ������
        buyButton.interactable = (tradeSystem.playerInventory.money >= cityItem.buyPrice && cityItem.stock > 0);
        //sellButton.interactable = (playerStock > 0 && tradeSystem.currentCity.cityGold >= cityItem.sellPrice);
    }
}