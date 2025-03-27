using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Text itemNameText;
    public Text priceText;
    public Text cityStockText;
    public Text playerStockText;
    public Button buyButton;
    public Button sellButton;

    private Item currentItem;
    private TradeItemSystem tradeItemSystem;

    public void Initialize(Item item, TradeItemSystem tradeSystem)
    {
        currentItem = item;
        this.tradeItemSystem = tradeSystem;

        itemNameText.text = item.itemName;
        priceText.text = $"����: {item.price}";
        cityStockText.text = $"� ������: {item.cityStock}";
        playerStockText.text = $"� ���: {item.playerStock}";

        buyButton.onClick.AddListener(() => tradeSystem.BuyItem(item, 1));
        sellButton.onClick.AddListener(() => tradeSystem.SellItem(item, 1));
    }
}