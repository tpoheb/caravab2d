using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TradeItemSystem : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public List<CityData> cities;
    private CityData currentCity;

    [Header("UI References")]
    public Text playerMoneyText;
    public Text cityMoneyText;
    public Text cityNameText;

    public GameObject tradePanel;
    public Transform itemsContainer;
    public GameObject itemUIPrefab;

    public void OpenCityTrade(CityData city)
    {
        currentCity = city;
        tradePanel.SetActive(true);
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Очистка контейнера
        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);

        // Обновление заголовков
        cityNameText.text = currentCity.cityName;
        playerMoneyText.text = $"Золото: {playerInventory.money}";
        cityMoneyText.text = $"Казна: {currentCity.cityGold}";

        // Создание элементов UI
        foreach (var cityItem in currentCity.items)
        {
            var itemUI = Instantiate(itemUIPrefab, itemsContainer);
            var controller = itemUI.GetComponent<ItemUI>();

            int playerStock = playerInventory.GetItemStock(cityItem.item);
            controller.Initialize(cityItem, playerStock, this);
        }
    }

    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        int totalCost = cityItem.buyPrice * quantity;

        if (playerInventory.money >= totalCost &&
            cityItem.stock >= quantity)
        {
            playerInventory.money -= totalCost;
            currentCity.cityGold += totalCost;
            cityItem.stock -= quantity;
            playerInventory.AddItem(cityItem.item, quantity);
            UpdateUI();
        }
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        int totalValue = cityItem.sellPrice * quantity;
        int playerStock = playerInventory.GetItemStock(cityItem.item);

        if (playerStock >= quantity &&
            currentCity.cityGold >= totalValue)
        {
            playerInventory.money += totalValue;
            currentCity.cityGold -= totalValue;
            cityItem.stock += quantity;
            playerInventory.RemoveItem(cityItem.item, quantity);
            UpdateUI();
        }
    }
 

    public void CloseTrade()
    {
        tradePanel.SetActive(false);
    }



}