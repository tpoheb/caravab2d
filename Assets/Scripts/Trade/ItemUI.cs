using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    //public Image icon;
    public TMP_Text itemName;
    public TMP_Text cityStock;
    public TMP_Text cityBuyPrice;
    public TMP_Text citySellPrice;
    public TMP_Text playerStock;
    public Button buyButton;
    public Button sellButton;

    public CityData.CityItem CityItem { get; private set; }

    public void Initialize(
        CityData.CityItem cityItem,
        int currentPlayerStock,
        System.Action buyAction,
        System.Action sellAction)
    {
        this.CityItem = cityItem;
        
        //icon.sprite = cityItem.item.icon;
        itemName.text = cityItem.item.itemName;
        cityStock.text = cityItem.stock.ToString();
        cityBuyPrice.text = cityItem.buyPrice.ToString();
        citySellPrice.text = cityItem.sellPrice.ToString();
        playerStock.text = currentPlayerStock.ToString();

        buyButton.onClick.AddListener(() => buyAction());
        sellButton.onClick.AddListener(() => sellAction());
    }

    public void UpdatePlayerStock(int newStock)
    {
        playerStock.text = newStock.ToString();
    }
}